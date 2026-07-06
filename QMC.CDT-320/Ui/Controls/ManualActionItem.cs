using System;
using System.Threading.Tasks;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>MANUAL ACTION 버튼 하나의 정의. 텍스트와 클릭 시 실행할 비동기 동작을 갖는다.</summary>
    public sealed class ManualActionItem
    {
        public string Text { get; private set; }
        public Func<Task> ClickAsync { get; private set; }
        public object Tag { get; set; }

        private ManualActionItem()
        {
        }

        /// <summary>버튼 텍스트와 클릭 동작으로 항목을 만든다. 동작 내부에서 확인 팝업/시퀀스 호출을 수행한다.</summary>
        public static ManualActionItem Create(string text, Func<Task> clickAsync)
        {
            return new ManualActionItem { Text = text ?? string.Empty, ClickAsync = clickAsync };
        }
    }
}