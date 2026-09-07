using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Calibration
{
    public sealed class VisionCameraManualSaveRequest
    {
        public CalibrationData CalibrationData { get; set; }
        public PickerFrontSetup PickerFrontSetup { get; set; }
        public PickerRearSetup PickerRearSetup { get; set; }
        public InputStageRecipe InputStageRecipe { get; set; }
        public OutputStageRecipe OutputStageRecipe { get; set; }
        public string RecipeName { get; set; }
    }

    /// <summary>
    /// 수동 카메라 캘리브레이션의 독립 사본만 저장합니다. 장비/축/런타임 객체를 변경하지 않습니다.
    /// 호출자는 저장 준비부터 성공 후 런타임 반영까지 기존 장비 배타 범위를 유지해야 합니다.
    /// </summary>
    public static class VisionCameraManualSaveService
    {
        public static JsonDataSaveCoordinator.TransactionSaveResult Save(VisionCameraManualSaveRequest request)
        {
            try
            {
                if (request == null || request.CalibrationData == null || request.CalibrationData.Camera == null)
                    throw new ArgumentException("저장할 카메라 캘리브레이션 사본이 없습니다.");
                if ((request.InputStageRecipe != null || request.OutputStageRecipe != null) &&
                    string.IsNullOrWhiteSpace(request.RecipeName))
                    throw new ArgumentException("Reticle 위치를 저장할 활성 Recipe 이름이 없습니다.");

                // 편집 후 후보도 재시작 로드와 동일해야 합니다. 예를 들어 Front 보정 배열이
                // 전부 0이면 OnDeserialized가 기본 보정을 넣으므로 그대로 저장하지 않습니다.
                CalibrationData calibration = CloneForEdit(request.CalibrationData);
                PickerFrontSetup front = request.PickerFrontSetup != null ? CloneForEdit(request.PickerFrontSetup) : null;
                PickerRearSetup rear = request.PickerRearSetup != null ? CloneForEdit(request.PickerRearSetup) : null;
                InputStageRecipe input = request.InputStageRecipe != null ? CloneForEdit(request.InputStageRecipe) : null;
                OutputStageRecipe output = request.OutputStageRecipe != null ? CloneForEdit(request.OutputStageRecipe) : null;

                var saves = new List<JsonDataSaveCoordinator.PreparedSave>
                {
                    JsonDataSaveCoordinator.Capture(calibration, CalibrationDataStore.FilePath)
                };
                if (front != null)
                    saves.Add(JsonDataSaveCoordinator.Capture(front,
                        EquipmentDataStore.PathOf("PickerFrontUnit", "Setup")));
                if (rear != null)
                    saves.Add(JsonDataSaveCoordinator.Capture(rear,
                        EquipmentDataStore.PathOf("PickerRearUnit", "Setup")));
                if (input != null)
                    saves.Add(JsonDataSaveCoordinator.Capture(input,
                        RecipeDataStore.PathOf(request.RecipeName, "InputStageUnit")));
                if (output != null)
                    saves.Add(JsonDataSaveCoordinator.Capture(output,
                        RecipeDataStore.PathOf(request.RecipeName, "OutputStageUnit")));

                return JsonDataSaveCoordinator.SaveTransaction(saves);
            }
            catch (Exception ex)
            {
                return new JsonDataSaveCoordinator.TransactionSaveResult(false, false,
                    "카메라 캘리브레이션 저장 사본을 준비하지 못했습니다. " + ex.Message);
            }
        }

        /// <summary>
        /// 런타임과 참조를 공유하지 않는 편집 사본을 만듭니다. 역직렬화 기본값 보정이 기존 저장값을
        /// 바꾸면 편집을 거절하여 이번 편집과 관계없는 필드가 함께 바뀌지 않게 합니다.
        /// </summary>
        public static T CloneForEdit<T>(T source) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            byte[] original = Serialize(source);
            T clone;
            using (var stream = new MemoryStream(original, false))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), JsonPrettySerializer.CreateSettings(true));
                clone = serializer.ReadObject(stream) as T;
            }
            if (clone == null || !original.SequenceEqual(Serialize(clone)))
                throw new InvalidOperationException("편집 사본을 만드는 과정에서 저장값이 달라졌습니다. 설정을 확인한 뒤 다시 실행하십시오. type=" + typeof(T).Name);
            return clone;
        }

        private static byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                JsonPrettySerializer.WriteObject(stream, typeof(T), value, JsonPrettySerializer.CreateSettings(true));
                return stream.ToArray();
            }
        }
    }
}
