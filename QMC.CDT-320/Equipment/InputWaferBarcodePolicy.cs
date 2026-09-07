using System;

namespace QMC.CDT320
{
    /// <summary>정규화된 입력 웨이퍼 바코드와 현재 생산 LOT ID의 접두어를 검증한다.</summary>
    public static class InputWaferBarcodePolicy
    {
        public const int DefaultPrefixLength = 5;
        public const int MaximumPrefixLength = 128;

        public static bool IsValidPrefixLength(int prefixLength)
        {
            return prefixLength >= 1 && prefixLength <= MaximumPrefixLength;
        }

        public static bool TryValidate(
            bool enabled,
            int prefixLength,
            bool barcodeEnabled,
            string lotId,
            string barcode,
            out string reason)
        {
            reason = string.Empty;
            if (!enabled)
                return true;

            if (!barcodeEnabled)
            {
                reason = "LOT 접두어 검사를 사용하려면 Input Wafer 바코드 사용을 켜야 합니다.";
                return false;
            }
            if (!IsValidPrefixLength(prefixLength))
            {
                reason = "LOT 접두어 비교 글자수가 유효하지 않습니다. actual=" + prefixLength + ", 허용 범위=1~128";
                return false;
            }

            string normalizedLot = string.IsNullOrWhiteSpace(lotId) ? string.Empty : lotId.Trim();
            string normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? string.Empty : barcode.Trim();
            if (normalizedLot.Length == 0)
            {
                reason = "LOT 접두어 검사를 위한 현재 생산 LOT ID가 없습니다. LOT을 시작한 뒤 다시 확인하십시오.";
                return false;
            }
            if (normalizedLot.Length < prefixLength || normalizedBarcode.Length < prefixLength)
            {
                reason = "LOT ID 또는 Input Wafer 바코드가 비교 글자수보다 짧습니다. 글자수=" + prefixLength +
                    ", LOT 길이=" + normalizedLot.Length + ", 바코드 길이=" + normalizedBarcode.Length;
                return false;
            }
            if (string.Compare(normalizedLot, 0, normalizedBarcode, 0, prefixLength, StringComparison.OrdinalIgnoreCase) != 0)
            {
                reason = "Input Wafer 바코드와 현재 생산 LOT ID의 접두어가 일치하지 않습니다. 글자수=" + prefixLength +
                    ", LOT 접두어=" + normalizedLot.Substring(0, prefixLength) +
                    ", 바코드 접두어=" + normalizedBarcode.Substring(0, prefixLength);
                return false;
            }
            return true;
        }
    }
}
