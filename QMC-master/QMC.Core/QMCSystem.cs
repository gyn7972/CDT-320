using SentiCore.Diagnostics;
using SentiCore.Languages;
using SentiCore.Products.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC
{
    #region QMCSystem
    public sealed class QMCSystem : DeviceSystem
    {
        #region Field
        private static QMCSystem m_Instance;
        private static Language m_Language;
        private static SystemLogService m_SystemLogService;
        #endregion

        #region Constructor
        /// <summary>
        /// <see cref="QMCSystem"/> 클래스의 인스턴스를 초기화한다.
        /// </summary>
        private QMCSystem() : base(nameof(QMCSystem), string.Empty) { }

        static QMCSystem()
        {
            SystemLogService.CreateLogService("QMC", out SystemLogService service);

            Language.CountryChanged += Language_CountryChanged;

            QMCSystem.m_Language = new Language(typeof(QMCSystem).Assembly, Language.Country);
            QMCSystem.m_SystemLogService = service;
        }
        #endregion

        #region Property
        public static QMCSystem Instance
        {
            get
            {
                if(QMCSystem.m_Instance == null)
                    QMCSystem.m_Instance = new QMCSystem();

                return QMCSystem.m_Instance;
            }
        }
        #endregion

        #region Method
        public static string Translate(string message)
        {
            return QMCSystem.m_Language.Translate(message);
        }

        public static void Regsite(string message)
        {
            QMCSystem.m_Language.Registe(message);
        }
        #endregion

        #region Event Handlers
        private static void Language_CountryChanged(object sender, System.EventArgs e)
        {
            QMCSystem.m_Language = new Language(typeof(QMCSystem).Assembly, Language.Country);
        }
        #endregion

        #region DeviceSystem Members
        protected override int OnCreate()
        {
            int ret = 0;

            return ret;
        }

        protected override int OnTerminate()
        {
            int ret = 0;

            return ret;
        }
        #endregion
    }
    #endregion
}
