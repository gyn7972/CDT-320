using System;
using System.Collections;
using System.Reflection;

// QMC.Vision.exe 의 CameraFactory.EnumerateAll() 를 호출해 카메라 검색 결과를 출력.
// bin\Debug 에서 실행해야 의존 어셈블리(QMC.Common/Matrox/MvCameraControl)가 해소됨.
class VerifyEnum
{
    static void Main()
    {
        try
        {
            var asm = Assembly.LoadFrom("QMC.Vision.exe");
            var t   = asm.GetType("QMC.Vision.Core.CameraFactory");
            var mi  = t.GetMethod("EnumerateAll", BindingFlags.Public | BindingFlags.Static);
            var list = (IEnumerable)mi.Invoke(null, null);
            int milCount = 0, total = 0;
            foreach (var info in list)
            {
                total++;
                var tp = info.GetType();
                string id = "" + tp.GetProperty("Id").GetValue(info, null);
                string vd = "" + tp.GetProperty("Vendor").GetValue(info, null);
                string tr = "" + tp.GetProperty("Transport").GetValue(info, null);
                string md = "" + tp.GetProperty("Model").GetValue(info, null);
                if (id.StartsWith("Mil/")) milCount++;
                Console.WriteLine("CAM | " + id + " | " + vd + " | " + tr + " | " + md);
            }
            Console.WriteLine("SUMMARY | total=" + total + " | milCount=" + milCount);
        }
        catch (Exception ex)
        {
            var e = ex.InnerException ?? ex;
            Console.WriteLine("EXCEPTION | " + e.GetType().Name + ": " + e.Message);
        }
    }
}
