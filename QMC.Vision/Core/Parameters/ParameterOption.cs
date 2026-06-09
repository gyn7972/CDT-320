namespace QMC.Vision.Core.Parameters
{
    /// <summary>Enum/Selection 파라미터의 선택지 1개 (표시 텍스트 + 실값). WinForms 비의존(Core).</summary>
    public sealed class ParameterOption
    {
        public string Text { get; }
        public object Value { get; }

        public ParameterOption(string text, object value)
        {
            Text = text ?? string.Empty;
            Value = value;
        }
    }
}
