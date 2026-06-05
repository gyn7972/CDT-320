using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Security;

using QMC.Transfers.Feeders;

namespace QMC
{
    #region TraceLogger
    public sealed class TraceLogger : IDisposable
    {
        #region Define
        [Serializable]
        public enum Category
        {
            ALARM,
            EVENT,
            PROCESS,
            CONFIGURATION,
            RECIPE,
        }
        #endregion

        #region Field
        private static TraceLogger m_Instance;
        private ConcurrentQueue<TraceMessage> m_Messages;
        private SafeThread m_Thread;
        #endregion

        #region Constructor
        static TraceLogger()
        {
            TraceLogger.Instance.m_Messages = new ConcurrentQueue<TraceMessage>();
            TraceLogger.Instance.Thread = new SafeThread();
            TraceLogger.Instance.Thread.Method = new AsyncDelegate(TraceLogger.Instance.Monitoring);

            AlarmManager.AlarmPosted += AlarmManager_AlarmPosted;
            FormManager.FormShowWindow += FormManager_FormShowWindow;

            TraceLogger.Instance.GetPositionRepository();
        }      
        #endregion

        #region Property
        public static TraceLogger Instance
        {
            get
            {
                if(TraceLogger.m_Instance == null)
                    TraceLogger.m_Instance = new TraceLogger();

                return TraceLogger.m_Instance;
            }
        }

        public SafeThread Thread
        {
            get { return this.m_Thread; }
            private set { this.m_Thread = value; }
        }
        #endregion

        #region Method
        private void GetPositionRepository()
        {
            PositionRepository[] repositories = ElementList.GetByType<PositionRepository>(true);
            foreach(PositionRepository repository in repositories)
            {
                repository.AppliedConfiguration += Repository_AppliedConfiguration;
            }
        }

        private void Repository_AppliedConfiguration(object sender, AppliedConfigurationEventArgs e)
        {
            try
            {
                var previous = e.Previous.Positions.ToList();
                var current = e.Current.Positions.ToList();
                for(int i = 0; i < previous.Count; i++)
                {
                    DataComparer.DeepCompareLog(DataComparer.DataTypes.Configuration, $"[{sender.ToString()} / {previous[i].Key}]", previous[i], current[i]);
                }
            }
            catch(Exception ex)
            {

            }
        }

        private string GetUser()
        {
            string userName = "";
            User user = null;
            user = UserManager.GetLogOnUser();

            userName = user == null ? "QMC" : user.ToString();

            return userName;
        }
        private static void AlarmLogging(string agent, string title)
        {
            List<string> messages = new List<string>();
            messages.Add(agent);
            messages.Add(title);
            TraceMessage trace = new TraceMessage(TraceLogger.Category.ALARM.ToString(), TraceLogger.Instance.GetUser(), messages);

            TraceLogger.Instance.m_Messages.Enqueue(trace);
        }

        private static void EventLogging(string formName)
        {
            List<string> messages = new List<string>();
            messages.Add($"[{formName}] have entered");
            TraceMessage trace = new TraceMessage(TraceLogger.Category.EVENT.ToString(), TraceLogger.Instance.GetUser(), messages);

            TraceLogger.Instance.m_Messages.Enqueue(trace);
        }

        public static void ProcessLogging(params string[] logs)
        {
            TraceMessage trace = new TraceMessage(TraceLogger.Category.PROCESS.ToString(), TraceLogger.Instance.GetUser(), logs.ToList());

            TraceLogger.Instance.m_Messages.Enqueue(trace);
        }

        public static void ConfigurationLogging(params string[] logs)
        {
            TraceMessage trace = new TraceMessage(TraceLogger.Category.CONFIGURATION.ToString(), TraceLogger.Instance.GetUser(), logs.ToList());

            TraceLogger.Instance.m_Messages.Enqueue(trace);
        }

        public static void RecipeLogging(params string[] logs)
        {
            TraceMessage trace = new TraceMessage(TraceLogger.Category.RECIPE.ToString(), TraceLogger.Instance.GetUser(), logs.ToList());

            TraceLogger.Instance.m_Messages.Enqueue(trace);
        }

        private void Monitoring()
        {
            TraceMessage message = null;
            StringBuilder builder = null;
            while(true)
            {
                if(TraceLogger.Instance.m_Messages.Count <= 0)
                    SafeThread.Sleep();
                else
                {
                    TraceLogger.Instance.m_Messages.TryDequeue(out message);

                    builder = new StringBuilder();

                    builder.Append($"[{message.Category}]");
                    builder.Append(", ");
                    builder.Append(message.User);

                    for(int i = 0; i < message.Messages.Count; i++)
                    {
                        builder.Append(", ");
                        builder.Append(message.Messages[i]);
                    }

                    Log.Write("Trace log", $"{builder.ToString()}");
                }
            }
        }
        #endregion

        #region Event Handlers
        private static void AlarmManager_AlarmPosted(Alarm sender, AlarmEventArgs e)
        {
            if(sender == null) return;

            if(sender.Grade == AlarmGrade.Inform.ToString()) return;

            TraceLogger.AlarmLogging(sender.Source, sender.Title);
        }
        private static void FormManager_FormShowWindow(ChildForm sender, FormShowWindowEventArgs e)
        {
            if(sender == null) return;

            TraceLogger.EventLogging(sender.Name);
        }
        #endregion

        #region IDisposal Members
        public void Dispose()
        {

        }
        #endregion
    }
    #endregion

    #region TraceMessage
    public class TraceMessage
    {
        #region Field
        private string m_Category;
        private string m_User;
        private List<string> m_Messages;
        #endregion

        #region Constructor
        public TraceMessage(string category, string user, List<string> message)
        {
            this.Category = category;
            this.User = user;
            this.Messages = message;
        }
        #endregion

        #region Property
        public string Category
        {
            get { return this.m_Category; }
            set { this.m_Category = value; }
        }
        public string User
        {
            get { return this.m_User; }
            set { this.m_User = value; }
        }

        public List<string> Messages
        {
            get { return this.m_Messages; }
            set { this.m_Messages = value; }
        }
        #endregion
    }
    #endregion

}
