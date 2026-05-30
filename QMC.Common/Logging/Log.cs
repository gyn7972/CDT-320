using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common
{
    public static class Log
    {
        public static void Write(string strClass, string strSource, string strMessage)
        {
            LogManager.Instance.Write(LogLevel.Normal, strClass, strSource, strMessage);
        }
        public static void Write(string strClass, string strOperator, string strSource, string strMessage)
        {
            LogManager.Instance.Write(LogLevel.Normal, strClass, strOperator, strSource, strMessage);
        }
        public static void Write(LogLevel level, string strClass, string strSource, string strMessage)
        {
            LogManager.Instance.Write(level, strClass, strSource, strMessage);
        }
        public static void Write(string strClass, string strMessage)
        {
            LogManager.Instance.Write(LogLevel.Normal, strClass, strMessage);
        }
        public static void Write(BaseEquipmentNode component, string strMessage)
        {
            LogManager.Instance.Write(LogLevel.Normal, component.Name, component.Name, strMessage);
        }
        public static void Write(LogLevel level, BaseEquipmentNode component, string strMessage)
        {
            LogManager.Instance.Write(level, component.Name, component.Name, strMessage);
        }
        public static void Write(LogLevel level, string strClass, BaseEquipmentNode component, string strMessage)
        {
            LogManager.Instance.Write(level, strClass, component.Name, strMessage);
        }
        public static void Write(Exception ex)
        {
            LogManager.Instance.Write(LogLevel.Highest, "ProgramExeption", ex.Source);
            LogManager.Instance.Write(LogLevel.Highest, "ProgramExeption", ex.Message);
            LogManager.Instance.Write(LogLevel.Highest, "ProgramExeption", ex.StackTrace);
            var st = new StackTrace(ex, true);  // true = 파일/줄 정보 포함
            var frame = st.GetFrame(0);         // 예외 발생 지점의 frame

            string fileName = frame?.GetFileName() ?? "UnknownFile";
            int lineNumber = frame?.GetFileLineNumber() ?? 0;
            string methodName = frame?.GetMethod()?.Name ?? "UnknownMethod";

            string log = $"[Exception] {ex.Message}\n"
                       + $"File: {fileName}\n"
                       + $"Line: {lineNumber}\n"
                       + $"Method: {methodName}\n"
                       + $"StackTrace:\n{ex.StackTrace}";
            LogManager.Instance.Write(LogLevel.Highest, "ProgramExeption", log);
        }
        public static void WriteWorkLog(string strMessage)
        {
            LogManager.Instance.WriteWorkLog(strMessage);
        }

        // [추가] 외부에서 호출 가능한 로그 정리 메서드
        /// <summary>
        /// 지정된 일수(days)가 지난 로그 파일을 삭제합니다.
        /// </summary>
        /// <param name="keepDays">로그 보관 일수 (예: 30)</param>
        public static void DeleteOldLogs(int keepDays)
        {
            if (keepDays < 1) return;
            LogManager.Instance.DeleteOldLogs(keepDays);
        }

        // [추가] 임의의 폴더 정리 기능 노출
        public static void DeleteOldFiles(string folderPath, int keepDays, string pattern = "*.*")
        {
            if (keepDays < 1) return;
            LogManager.Instance.DeleteOldFiles(folderPath, keepDays, pattern);
        }

        // [추가] 오래된 폴더 삭제 기능 노출
        /// <summary>
        /// 지정된 경로 하위의 폴더들 중 오래된 폴더를 삭제합니다.
        /// </summary>
        /// <param name="rootPath">검사할 상위 폴더 경로 (예: D:\Log)</param>
        /// <param name="keepDays">보관 일수</param>
        public static void DeleteOldFolders(string rootPath, int keepDays)
        {
            if (keepDays < 1) return;
            LogManager.Instance.DeleteOldFolders(rootPath, keepDays);
        }
    }
}
