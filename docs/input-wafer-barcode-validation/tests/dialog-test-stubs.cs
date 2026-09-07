using System;
using System.Windows.Forms;

// 실제 복구창만 검사한다. 장비/전역 설정/운영 로그를 초기화하지 않는다.
namespace QMC.CDT320.Barcode
{
    public enum BarcodeReaderChannel { InputWafer, OutputBin }
    public sealed class BarcodeRecoveryRequest
    {
        public BarcodeReaderChannel Channel { get; set; }
        public string MaterialId { get; set; }
        public string MaterialInstanceId { get; set; }
        public string FailureMessage { get; set; }
        public bool ValidationRecovery { get; set; }
        public string CurrentBarcode { get; set; }
        public string LotId { get; set; }
        public int PrefixLength { get; set; }
        public int RetryCount { get; set; } = 3;
        public double RetryStepMm { get; set; } = 1.0;
    }
}

namespace QMC.CDT320.VisionComm
{
    public static class BarcodeSerialAdapter
    {
        public static string NormalizePayload(string value) { return (value ?? "").Trim(); }
    }
}

namespace QMC.Common
{
    public static class MessageDialog
    {
        public static void Show(IWin32Window owner, string text, string title)
        {
            throw new InvalidOperationException("Unexpected message dialog: " + text);
        }
    }
}
