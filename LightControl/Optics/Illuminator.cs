using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics
{
    [Serializable]
    public class Illuminator : Part
    {
        public enum IlluminatorType
        {
            Normal,
            Strobe,
        }

        public IlluminatorConfig IlluminatorConfig
        {
            set
            {
                Config = value;
            }
            get
            {
                return (IlluminatorConfig)Config;
            }
        }
        public IlluminatorType Type { get; set; }
        public Illuminator() : this("Illuminator")
        {
            Type = IlluminatorType.Normal;
        }
        public Illuminator(string strName) : base(strName)
        {
            Type = IlluminatorType.Normal;
        }

        public override int Initialize()
        {
            return base.Initialize();
        }
        
        public virtual int CheckPowerOn(int channel)
        {
            return -1;
        }

        public virtual int SetVolume(int volume, int channel)
        {
            return -1;
        }

        public virtual int TurnOnOff(bool bOnOff, int channel)
        {
            return -1;
        }
        public virtual int SetStrobeOnTime(int channel, double settime)
        {
            return -1;
        }

        public virtual int SetStrobeOnTime(int page, int[] times )
        {
            return -1;
        }

        public virtual int SetValue(int channel, int power, int onTime)
        {
            return -1;
        }

        public virtual int SetValue(BrightSettingParameter parameter)
        {
            return -1;
        }
        public virtual int SetStrobeOnTime(IlluminationDataSet illuminationDataSet)
        {
            int ret = 0;
            if(illuminationDataSet != null)
            {
                foreach (IlluminationChannel channel in illuminationDataSet.Values)
                {
                    if (Type == Illuminator.IlluminatorType.Normal)
                    {
                        if((ret = SetVolume(channel.Value, channel.Channel)) != 0)
                        {
                            return ret;
                        }
                    }
                    else if (Type == Illuminator.IlluminatorType.Strobe)
                    {
                        if((ret = SetStrobeOnTime(channel.Channel, channel.StrobeValue)) != 0)
                        {
                            return ret;
                        }
                    }
                    Thread.Sleep(50);
                }
            }

            return ret;
        }

        public virtual int StrobeOn(int channel)
        {
            return -1;
        }
        public virtual int SetStrobeEdge(int channel, bool mode)
        {
            return -1;
        }

        public virtual void Load(FileStream fs)
        {
            
        }

        public virtual void Save(FileStream fs)
        {
            
        }
    }

    [Serializable]
    public enum ParamIlluminatorKey
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
    public enum IlluminatorControllerType
    {
        None,
        Leesos,
        LFine,
        LFinePSD,
        LSStrobe,
    }

    [Serializable]
    public class IlluminatorConfig : BaseConfig
    {
        public string Name { set; get; }
        public IlluminatorControllerType ControllerType { set; get; }

        public List<object> IlluminationKeys { set; get; }
        public IlluminatorConfig()
        {
            Name = string.Empty;
            ControllerType = IlluminatorControllerType.None;
            IlluminationKeys = new List<object>();
        }
        public virtual void Init()
        {
            if(IlluminationKeys == null)
            {
                IlluminationKeys = new List<object>();
            }
        }

        public override List<object> GetPositions()
        {
            return null;
        }

        public override void SetParam(ListParam listParam)
        {
            
        }

        public override ListParam ToListParam()
        {
            return null;
        }
    }

    [Serializable]
    public class IlluminatorChannelInfo
    {
        public string Name { set; get; }
        public int Channel { set; get; }
    }
}
