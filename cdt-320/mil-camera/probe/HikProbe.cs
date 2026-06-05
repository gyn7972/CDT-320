using System;
using System.Reflection;
using System.Runtime.InteropServices;

// Hik 진단(카메라 연결 상태): (1) 전송별 raw EnumDevices 카운트, (2) 발견 장치 상세,
// (3) 우리 코드 HikGigECamera.Enumerate() 카운트 — SDK 발견 vs 코드 드롭 구분.
class HikProbe
{
    static Assembly _mvs, _vis;
    static Type _camT, _listT, _devT;

    static void Main()
    {
        try
        {
            // 실험: GenTL producer 검색 경로 맨 앞에 현재 폴더(bin\Debug)를 둠 (.cti + 의존 DLL co-locate 가설)
            string cur = Environment.CurrentDirectory;
            var g64 = Environment.GetEnvironmentVariable("GENICAM_GENTL64_PATH") ?? "";
            if (!g64.StartsWith(cur, StringComparison.OrdinalIgnoreCase))
                Environment.SetEnvironmentVariable("GENICAM_GENTL64_PATH", cur + ";" + g64);
            Console.WriteLine("GENTL64[0]=" + (Environment.GetEnvironmentVariable("GENICAM_GENTL64_PATH") ?? "").Split(';')[0]);

            _vis = Assembly.LoadFrom("QMC.Vision.exe");
            _mvs = Assembly.LoadFrom("MvCameraControl.Net.dll");
            _camT  = _mvs.GetType("MvCamCtrl.NET.MyCamera");
            _listT = _camT.GetNestedType("MV_CC_DEVICE_INFO_LIST")
                  ?? _mvs.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO_LIST")
                  ?? _mvs.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO_LIST");
            _devT  = _mvs.GetType("MvCamCtrl.NET.MyCamera+MV_CC_DEVICE_INFO")
                  ?? _mvs.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO")
                  ?? _mvs.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO");
            Console.WriteLine("Is64Proc=" + Environment.Is64BitProcess + " | listT=" + (_listT!=null) + " | devT=" + (_devT!=null));

            RawEnum(0x1,   "GIGE");
            RawEnum(0x4,   "USB");
            RawEnum(0x8,   "CAMERALINK");
            RawEnum(0x40,  "GENTL_GIGE");
            RawEnum(0x80,  "GENTL_CL");
            RawEnum(0x100, "GENTL_CXP");
            RawEnum(0x200, "GENTL_XOF");
            RawEnum(0x3FF, "ALL(0x3FF)");

            // 우리 코드 경로
            try
            {
                var hik = _vis.GetType("QMC.Vision.Cameras.Hik.HikGigECamera");
                var mi  = hik.GetMethod("Enumerate", BindingFlags.Public|BindingFlags.Static);
                var list = (System.Collections.IEnumerable)mi.Invoke(null, null);
                int c=0; foreach(var x in list){ c++; var idp=x.GetType().GetProperty("Id"); Console.WriteLine("  OURS | " + idp.GetValue(x,null)); }
                Console.WriteLine("HikGigECamera.Enumerate() count=" + c);
            }
            catch(Exception ex){ var e=ex.InnerException??ex; Console.WriteLine("OURS_THROW | "+e.GetType().Name+": "+e.Message); }
        }
        catch (Exception ex) { var e=ex.InnerException??ex; Console.WriteLine("FATAL | "+e.GetType().Name+": "+e.Message); }
    }

    static void RawEnum(uint flag, string label)
    {
        try
        {
            var listObj = Activator.CreateInstance(_listT);
            var mi = _camT.GetMethod("MV_CC_EnumDevices_NET", BindingFlags.Public|BindingFlags.Static, null,
                                     new[]{ typeof(uint), _listT.MakeByRefType() }, null);
            var args = new object[]{ flag, listObj };
            int r = Convert.ToInt32(mi.Invoke(null, args));
            int n = (int)(uint)_listT.GetField("nDeviceNum").GetValue(args[1]);
            Console.WriteLine("ENUM(" + label + ") ret=0x" + r.ToString("X") + " num=" + n);
            if (n > 0)
            {
                var devs = _listT.GetField("pDeviceInfo").GetValue(args[1]);
                for (int i=0;i<n;i++)
                {
                    object dev = null;
                    if (devs is IntPtr[] ptrs && ptrs[i]!=IntPtr.Zero) dev = Marshal.PtrToStructure(ptrs[i], _devT);
                    else if (devs is Array arr) dev = arr.GetValue(i);
                    uint tlayer = 0; try { tlayer = (uint)dev.GetType().GetField("nTLayerType").GetValue(dev); } catch {}
                    Console.WriteLine("   dev[" + i + "] nTLayerType=0x" + tlayer.ToString("X"));
                }
            }
        }
        catch (Exception ex) { var e=ex.InnerException??ex; Console.WriteLine("ENUM("+label+")_THROW | "+e.GetType().Name+": "+e.Message); }
    }
}
