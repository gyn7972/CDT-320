using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common
{
    public class LogInfo
    {
        private DateTime m_OccurredTime;
        public string Source { set; get; }
        public string Message { set; get; }
        public LogLevel Level { set; get; }
        public string Classification { set; get; }
        public string CreationDate 
        {
            get
            {
                return m_OccurredTime.ToString("yyyy-MM-dd");
            }
            
        }

        public LogInfo() : this(LogLevel.Normal, string.Empty, string.Empty)
        {
        }
        public LogInfo(string message) : this(LogLevel.Normal, string.Empty, message)
        {
        }
        public LogInfo(string source, string message) : this(LogLevel.Normal, source, message)
        {
        }
        public LogInfo(LogLevel level, string source, string message)
        {
            Level = level;
            Source = source;
            Message = message;
            m_OccurredTime = DateTime.Now;
        }
        public LogInfo(LogLevel level, string strSource, string strMessage, string strClass)
        {
            Level = level;
            Source = strSource;
            Message = strMessage;
            m_OccurredTime = DateTime.Now;
            Classification = strClass;
        }
        public override string ToString()
        {
            return string.Format("{0}:{1}> {2}", m_OccurredTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), Source, Message);
        }

    }
}
