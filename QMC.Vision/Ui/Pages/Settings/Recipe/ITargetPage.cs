using System;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2d — 3열 타깃 페이지(VisionTargetPage=finder / InspectorTargetPage=inspector) 공통 계약.
    /// RecipePage 가 dirty 상태점·타깃 저장을 타입 무관하게 다루기 위함.
    /// </summary>
    public interface ITargetPage
    {
        bool IsDirty { get; }
        bool HasSavedData { get; }
        void SaveTarget();
        /// <summary>C3b-3 — 조명 지정(노드 Setup.LightPages) 변경을 레벨 그리드에 반영(재바인딩). RecipePage 가 타깃 표시 시 호출.</summary>
        void RefreshLightAssignment();
        event EventHandler DirtyChanged;
    }
}
