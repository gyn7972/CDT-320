using System;
using System.Collections.Generic;
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
                       + $"Source: {ex.Source}\n"
                       + $"data: {ex.Data.ToString()}\n"
                       + $"Method: {methodName}\n"
                       + $"StackTrace:\n{ex.StackTrace}"
                       ;
            ;
            LogManager.Instance.Write(LogLevel.Highest, "ProgramExeption", log);
        }
        public static void WriteWorkLog(string strMessage)
        {
            LogManager.Instance.WriteWorkLog(strMessage);
        }
    }
}
