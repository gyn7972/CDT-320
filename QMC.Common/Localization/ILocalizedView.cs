namespace QMC.Common.Localization
{
    /// <summary>모델 값, 입력값, 선택 상태를 바꾸지 않고 표시 문구만 다시 적용하는 뷰입니다.</summary>
    public interface ILocalizedView
    {
        /// <summary>언어 변경 시 표시 문구와 셀 서식 등 화면 표현만 갱신합니다.</summary>
        void ApplyLanguage();
    }
}
