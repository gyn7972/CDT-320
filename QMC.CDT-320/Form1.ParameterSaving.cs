using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.Common;
using QMC.Common.Data.Store;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320
{
    public partial class Form1
    {
        private sealed class PendingParameterCollection
        {
            internal BaseEquipmentNode Node;
            internal bool Recipe;
            internal string RecipeName;
        }

        private bool _parameterSaveFailureDialogOpen;
        private readonly int _parameterSaveUiThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        private bool _parameterSaveExitInProgress;
        private bool _parameterSaveFileActionInProgress;
        private string _parameterSaveLastFailure = string.Empty;
        private Task _parameterSaveObservation = Task.FromResult(0);
        private readonly Dictionary<string, object> _parameterSaveUncaptured =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PendingParameterCollection> _parameterSaveCollections =
            new Dictionary<string, PendingParameterCollection>(StringComparer.OrdinalIgnoreCase);

        private bool HasUnfinishedParameterSaves
        {
            get { return _parameterSaveCollections.Count != 0 || _parameterSaveUncaptured.Count != 0 || JsonDataSaveCoordinator.HasUnfinishedSaves; }
        }

        // 값의 기존 런타임 적용 시점은 유지하고, 해당 유닛의 파일 저장만 UI 밖으로 분리합니다.
        internal void QueueRecipeEditorSave(BaseEquipmentNode node, bool recipe)
        {
            if (IsDisposed || Disposing) throw new InvalidOperationException("종료 중에는 설정을 저장할 수 없습니다.");
            if (node == null) throw new InvalidOperationException("저장할 유닛이 없습니다.");
            string recipeName = NormalizeRecipeName(ActiveRecipeName);
            if (recipe && string.IsNullOrEmpty(recipeName)) throw new InvalidOperationException("활성 Recipe가 없습니다.");

            var watch = Stopwatch.StartNew();
            string collectionKey = node.StorageKey + "|" + recipe + "|" + (recipe ? recipeName : string.Empty);
            var collection = new PendingParameterCollection { Node = node, Recipe = recipe, RecipeName = recipeName };
            _parameterSaveCollections[collectionKey] = collection;
            var sources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            try { CollectParameterSources(collection, sources); }
            catch (Exception ex)
            {
                Text = "CDT-320 - 설정 저장 대상 준비 실패";
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-COLLECT-FAIL",
                    "설정 저장 대상을 준비하지 못했습니다. unit=" + node.Name + ", recipe=" + recipeName + ", detail=" + ex);
                throw;
            }
            // 모든 대상 캡처를 끝낸 후 등록하여 캡처 실패 때 일부 파일만 큐에 남지 않게 합니다.
            List<JsonDataSaveCoordinator.PreparedSave> snapshots = CaptureParameterSources(sources);
            Task<DataStoreResult>[] pending = snapshots.Select(JsonDataSaveCoordinator.Enqueue).ToArray();
            _parameterSaveCollections.Remove(collectionKey);
            Text = "CDT-320 - 설정 저장 중";
            if (watch.ElapsedMilliseconds >= 100)
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-SNAPSHOT",
                    "설정 저장 사본 준비 시간이 길었습니다. unit=" + node.Name + ", count=" + snapshots.Count +
                    ", elapsedMs=" + watch.ElapsedMilliseconds);
            _parameterSaveObservation = ObserveParameterSavesAsync(pending, node.Name, recipeName);
        }

        private void CollectParameterSources(PendingParameterCollection collection, Dictionary<string, object> sources)
        {
            BaseEquipmentNode node = collection.Node;
            string recipeName = collection.RecipeName;
            bool recipe = collection.Recipe;
            if (recipe && !string.Equals(recipeName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("저장하지 못한 Recipe와 현재 Recipe가 다릅니다. 이전 Recipe를 확인한 후 명시적으로 저장하십시오. pending=" +
                    recipeName + ", active=" + ActiveRecipeName);
            CollectEditedNodeSaves(node, recipe, recipeName, sources);
            // 양 Picker의 T 티칭은 유닛이 아닌 Machine.Recipe.PickerT가 단일 저장 원본입니다.
            if (recipe && (node is PickerFrontUnit || node is PickerRearUnit) && Machine != null)
                sources[RecipeDataStore.PathOf(recipeName, Machine.StorageKey)] = Machine.Recipe;
        }

        private static void CollectEditedNodeSaves(BaseEquipmentNode node, bool recipe, string recipeName,
            Dictionary<string, object> sources)
        {
            if (node == null) return;
            if (recipe)
                sources[RecipeDataStore.PathOf(recipeName, node.StorageKey)] = node.Recipe;
            else if (!(node is BaseAxis))
            {
                // BaseAxis.SaveSettings는 MotionAxisStore만 권위로 사용하므로 축 설정 파일은 생성하지 않습니다.
                // 실 DI/DO는 기존 SaveSettings와 동일하게 주소/극성 Setup 자동 저장을 생략합니다.
                VisionUnit vision = node as VisionUnit;
                if (vision != null && vision.Config != null) vision.Config.EnsureCalibrationObjects();
                if (!(node is AjinDigitalInput) && !(node is AjinDigitalOutput))
                    sources[EquipmentDataStore.PathOf(node.StorageKey, "Setup")] = node.Setup;
                sources[EquipmentDataStore.PathOf(node.StorageKey, "Config")] = node.Config;
                // CalibrationData는 VisionConfig의 DataMember가 아니며 별도 파일이 로드 시 우선합니다.
                if (vision != null && vision.Config != null && vision.Config.CalibrationData != null)
                    sources[CalibrationDataStore.FilePath] = vision.Config.CalibrationData;
            }
            var property = node.GetType().GetProperty("Components");
            var children = property != null ? property.GetValue(node, null) as IEnumerable : null;
            if (children == null) return;
            foreach (BaseEquipmentNode child in children)
                CollectEditedNodeSaves(child, recipe, recipeName, sources);
        }

        private List<JsonDataSaveCoordinator.PreparedSave> CaptureParameterSources(Dictionary<string, object> sources)
        {
            try
            {
                var snapshots = sources.Select(s => JsonDataSaveCoordinator.Capture(
                    UnitDataStore.GetPersistentConfigSnapshot(s.Value), s.Key)).ToList();
                foreach (string path in sources.Keys) _parameterSaveUncaptured.Remove(path);
                return snapshots;
            }
            catch (Exception ex)
            {
                // Recipe 객체가 이후 교체돼도 실패 당시의 데이터 객체와 확정된 경로를 보존합니다.
                foreach (var source in sources) _parameterSaveUncaptured[source.Key] = source.Value;
                Text = "CDT-320 - 설정 저장 사본 준비 실패";
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-CAPTURE-FAIL",
                    "설정 저장 사본을 준비하지 못했습니다. paths=" + string.Join(", ", sources.Keys) + ", detail=" + ex);
                throw;
            }
        }

        private DataStoreResult RetryUncapturedParameterSaves()
        {
            try
            {
                foreach (var collection in _parameterSaveCollections.ToArray())
                {
                    var collected = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    CollectParameterSources(collection.Value, collected);
                    foreach (var snapshot in CaptureParameterSources(collected)) JsonDataSaveCoordinator.Enqueue(snapshot);
                    _parameterSaveCollections.Remove(collection.Key);
                }
                if (_parameterSaveUncaptured.Count != 0)
                {
                    var sources = new Dictionary<string, object>(_parameterSaveUncaptured, StringComparer.OrdinalIgnoreCase);
                    foreach (var snapshot in CaptureParameterSources(sources)) JsonDataSaveCoordinator.Enqueue(snapshot);
                }
                return DataStoreResult.Ok(string.Empty);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-CAPTURE-FAIL", "설정 사본 재준비에 실패했습니다. " + ex);
                return DataStoreResult.Fail(string.Empty, "설정 사본을 준비하지 못했습니다. " + ex.Message, ex);
            }
        }

        private void OnSynchronousParameterSaveSucceeded(string recipeName)
        {
            // 일부 보정 화면은 기존 SaveMachine*를 worker에서 호출합니다. 그 저장이 끝나는 동안
            // UI에 새로 생긴 실패 요청을 뒤늦은 worker 완료로 지우지 않습니다.
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != _parameterSaveUiThreadId)
                return;
            string directory = string.IsNullOrEmpty(recipeName) ? EquipmentDataStore.Root : RecipeDataStore.DirOf(recipeName);
            string prefix = directory.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            foreach (string path in _parameterSaveUncaptured.Keys.Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                _parameterSaveUncaptured.Remove(path);
            foreach (var collection in _parameterSaveCollections.ToArray())
                if (string.IsNullOrEmpty(recipeName) ? !collection.Value.Recipe :
                    collection.Value.Recipe && string.Equals(collection.Value.RecipeName, recipeName, StringComparison.OrdinalIgnoreCase))
                    _parameterSaveCollections.Remove(collection.Key);
        }

        private async Task ObserveParameterSavesAsync(Task<DataStoreResult>[] pending, string unitName, string recipeName)
        {
            try
            {
                DataStoreResult[] results = await Task.WhenAll(pending);
                if (IsDisposed || Disposing) return;
                DataStoreResult failed = results.Where(r => !r.Success)
                    .Select(r => JsonDataSaveCoordinator.GetCurrentFailure(r.Path)).FirstOrDefault(r => r != null);
                if (failed == null)
                {
                    if (!HasUnfinishedParameterSaves)
                    {
                        Text = "CDT-320 - 설정 저장 완료";
                        _parameterSaveLastFailure = string.Empty;
                    }
                    return;
                }
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-SAVE-FAIL",
                    "설정 파일 저장에 실패했습니다. unit=" + unitName + ", recipe=" + recipeName +
                    ", path=" + failed.Path + ", detail=" + failed.Message);
                Text = "CDT-320 - 설정 저장 실패";
                string signature = failed.Path + "|" + failed.Message;
                if (_parameterSaveExitInProgress || _parameterSaveFileActionInProgress || _parameterSaveFailureDialogOpen || _parameterSaveLastFailure == signature)
                    return;
                _parameterSaveLastFailure = signature;
                _parameterSaveFailureDialogOpen = true;
                try
                {
                    if (MessageDialog.Show(this,
                        "설정 파일 저장에 실패했습니다. 화면의 변경값은 유지되어 있습니다.\r\n" + failed.Path +
                        "\r\n" + failed.Message + "\r\n\r\n다시 저장하시겠습니까?", "설정 저장 실패",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                    {
                        DataStoreResult retry = await JsonDataSaveCoordinator.RetryFailedAsync();
                        if (IsDisposed || Disposing) return;
                        Text = retry.Success ? (HasUnfinishedParameterSaves ? "CDT-320 - 설정 저장 중" : "CDT-320 - 설정 저장 완료") : "CDT-320 - 설정 저장 실패";
                        if (retry.Success) _parameterSaveLastFailure = string.Empty;
                        else MessageDialog.Show(this, "재저장하지 못했습니다. 변경값은 유지되어 있으며 명시적 저장 또는 종료 시 다시 시도할 수 있습니다.\r\n" +
                            retry.Message, "설정 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                finally { _parameterSaveFailureDialogOpen = false; }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, UserSession.Name, "UI-PARAM-SAVE-FAIL", "설정 저장 결과 처리에 실패했습니다. " + ex);
            }
        }

        private async Task<bool> FlushParameterSavesBeforeExitAsync()
        {
            return await FlushParameterSavesBeforeActionAsync("종료");
        }

        internal async Task<bool> FlushParameterSavesBeforeRecipeFileActionAsync()
        {
            _parameterSaveFileActionInProgress = true;
            try { return await FlushParameterSavesBeforeActionAsync("Recipe 파일 작업"); }
            finally { _parameterSaveFileActionInProgress = false; }
        }

        private async Task<bool> FlushParameterSavesBeforeActionAsync(string action)
        {
            var wait = Stopwatch.StartNew();
            DataStoreResult result = RetryUncapturedParameterSaves();
            if (result.Success) result = await JsonDataSaveCoordinator.FlushAsync();
            while (!result.Success || HasUnfinishedParameterSaves)
            {
                if (IsDisposed || Disposing) return false;
                if (result.Success)
                {
                    // await 중 다른 화면에서 발생한 새 편집/사본 실패도 확인합니다.
                    if (wait.ElapsedMilliseconds >= 15000)
                        result = DataStoreResult.Fail(string.Empty, "저장 대기 중 새 설정 변경이 발생하여 완료를 확인하지 못했습니다.");
                    else
                    {
                        result = RetryUncapturedParameterSaves();
                        if (result.Success) result = await JsonDataSaveCoordinator.FlushAsync(15000 - (int)wait.ElapsedMilliseconds);
                        continue;
                    }
                }
                Text = "CDT-320 - 설정 저장 미완료";
                if (MessageDialog.Show(this, "설정 저장을 완료하지 못해 " + action + "을 진행하지 않았습니다.\r\n" + result.Path +
                    "\r\n" + result.Message + "\r\n\r\n다시 저장하시겠습니까?", "설정 저장 확인",
                    MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry)
                    return false;
                wait.Restart();
                result = RetryUncapturedParameterSaves();
                if (result.Success) result = await JsonDataSaveCoordinator.RetryFailedAsync();
            }
            return true;
        }
    }
}
