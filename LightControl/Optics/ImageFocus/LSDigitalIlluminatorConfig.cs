using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics.ImageFocus
{
    [Serializable]
    public enum ParamLSDigitalIlluminatorKey
    {
        PortName,
        BaudRate,
        DataBits,
        StopBits,
        Parity,
        Handshake,
        TimeOut
    }
    [Serializable]
    public class LSDigitalIlluminatorConfig : BaseConfig
    {
        #region Property
        public string PortName
        {
            set;
            get;
        }

        public int BaudRate
        {
            set;
            get;
        }

        public int DataBits
        {
            set;
            get;
        }

        public StopBits StopBits
        {
            set;
            get;
        }

        public Parity Parity
        {
            set;
            get;
        }

        public Handshake Handshake
        {
            set;
            get;
        }

        public int TimeOut { get; set; }

        public List<IlluminationChannel> ListIlluminationChannel { set; get; }

        public string Name { get; set; }
        #endregion
        public LSDigitalIlluminatorConfig()
        {
            PortName = "COM1";
            BaudRate = 9600;
            DataBits = 8;
            StopBits = StopBits.One;
            Parity = Parity.None;
            Handshake = Handshake.None;
            TimeOut = 1000;
            Init();
        }

        public void Init()
        {
            if(ListIlluminationChannel == null)
            {
                ListIlluminationChannel = new List<IlluminationChannel>();
            }
        }
        public override List<IlluminationChannel> GetIlluminationChannels()
        {
            return ListIlluminationChannel;
        }

        #region ListParam
        public override ListParam ToListParam()
        {

            ListParam list = new ListParam();
            ParamGroup ParamGroup = new ParamGroup();
            if (string.IsNullOrEmpty(Name) == true)
            {
                ParamGroup.Name = this.GetType().Name;
            }
            else
            {
                ParamGroup.Name = Name;
            }
            {
                Param param = new Param();
                param.SetParam(nameof(PortName), Param.DisplayTypeKey.Text, PortName, Param.ValueTypeKey.String, ParamGroup.Name);

                list.AddParam(param);
            }

            {
                Param param = new Param();
                param.SetParam(nameof(BaudRate), Param.DisplayTypeKey.Text, BaudRate, Param.ValueTypeKey.Int, ParamGroup.Name);

                list.AddParam(param);
            }

            {
                Param param = new Param();
                param.SetParam(nameof(DataBits), Param.DisplayTypeKey.Text, DataBits, Param.ValueTypeKey.Int, ParamGroup.Name);


                list.AddParam(param);
            }
            {
                Param param = new Param();
                param.SetParam(nameof(StopBits), Param.DisplayTypeKey.Combobox, StopBits, Param.ValueTypeKey.Int, ParamGroup.Name);

                param.SelectValues.Clear();
                foreach (Enum e in Enum.GetValues(typeof(StopBits)))
                {
                    param.SelectValues.Add(e.ToString());
                }

                list.AddParam(param);
            }
            {
                Param param = new Param();
                param.SetParam(nameof(Parity), Param.DisplayTypeKey.Combobox, Parity, Param.ValueTypeKey.Int, ParamGroup.Name);

                param.SelectValues.Clear();
                foreach (Enum e in Enum.GetValues(typeof(Parity)))
                {
                    param.SelectValues.Add(e.ToString());
                }


                list.AddParam(param);
            }
            {
                Param param = new Param();
                param.SetParam(nameof(Handshake), Param.DisplayTypeKey.Combobox, Handshake, Param.ValueTypeKey.Int, ParamGroup.Name);

                param.SelectValues.Clear();
                foreach (Enum e in Enum.GetValues(typeof(Handshake)))
                {
                    param.SelectValues.Add(e.ToString());
                }


                list.AddParam(param);
            }
            {
                Param param = new Param();
                param.SetParam(nameof(TimeOut), Param.DisplayTypeKey.Text, TimeOut, Param.ValueTypeKey.Int, ParamGroup.Name);

                list.AddParam(param);
            }
            return list;
        }
        public override void SetParam(ListParam listParam)
        {
            ParamGroup group = listParam.GetGroup();
            if (group != null)
            {
                Param param = null;
                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.PortName);
                if (param != null)
                {
                    string value = string.Empty;
                    if (param.GetStringValue(ref value))
                    {
                        PortName = value;
                    }
                }
                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.BaudRate);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        BaudRate = value;
                    }
                }
                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.DataBits);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        DataBits = value;
                    }
                }
                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.StopBits);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        StopBits = (StopBits)value;
                    }
                }

                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.Parity);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        Parity = (Parity)value;
                    }
                }

                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.Handshake);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        Handshake = (Handshake)value;
                    }
                }

                param = group.GetParam((int)ParamLSDigitalIlluminatorKey.TimeOut);
                if (param != null)
                {
                    int value = 0;
                    if (param.GetIntValue(ref value))
                    {
                        TimeOut = value;
                    }
                }
            }
        }

        public override List<object> GetPositions()
        {
            return null;
        }
        #endregion
    }
}
