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
        event EventHandler DirtyChanged;
    }
}
