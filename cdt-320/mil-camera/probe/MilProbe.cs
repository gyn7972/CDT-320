using System;
using Matrox.MatroxImagingLibrary;

// MIL 보드/디지타이저/카메라 감지 프로브 (본 구현 전 타당성 확인용).
class MilProbe
{
    static bool IsNull(MIL_ID id) { return ((long)id) == 0; }

    static void Main()
    {
        MIL_ID app = MIL.M_NULL, sys = MIL.M_NULL, host = MIL.M_NULL;
        try
        {
            MIL.MappAlloc(MIL.M_DEFAULT, ref app);
            if (IsNull(app)) { Console.WriteLine("RESULT|FAIL|MappAlloc NULL (MIL/license unusable)"); return; }
            MIL.MappControl(MIL.M_ERROR, MIL.M_PRINT_DISABLE);
            Console.WriteLine("APP|MappAlloc OK");

            MIL.MsysAlloc(app, MIL.M_SYSTEM_DEFAULT, MIL.M_DEFAULT, MIL.M_DEFAULT, ref sys);
            if (IsNull(sys)) { Console.WriteLine("RESULT|NO_BOARD|default system NULL"); }
            else
            {
                MIL_INT digs = 0;  MIL.MsysInquire(sys, MIL.M_DIGITIZER_NUM, ref digs);
                MIL_INT stype = 0; MIL.MsysInquire(sys, MIL.M_SYSTEM_TYPE,   ref stype);
                Console.WriteLine("SYS|OK|systemType=" + ((long)stype) + "|digitizers=" + ((long)digs));

                long n = (long)digs; if (n > 4) n = 4;
                for (int i = 0; i < n; i++)
                {
                    MIL_ID dig = MIL.M_NULL;
                    MIL_INT dn = i;
                    try { MIL.MdigAlloc(sys, dn, "M_DEFAULT", MIL.M_DEFAULT, ref dig); }
                    catch (Exception ex) { Console.WriteLine("DIG" + i + "|EX|" + ex.Message); continue; }
                    if (IsNull(dig)) { Console.WriteLine("DIG" + i + "|empty (no camera / needs format)"); continue; }
                    MIL_INT sx = 0, sy = 0, present = 0;
                    try { MIL.MdigInquire(dig, MIL.M_SIZE_X, ref sx); } catch { }
                    try { MIL.MdigInquire(dig, MIL.M_SIZE_Y, ref sy); } catch { }
                    try { MIL.MdigInquire(dig, MIL.M_CAMERA_PRESENT, ref present); } catch { }
                    Console.WriteLine("DIG" + i + "|OK|" + ((long)sx) + "x" + ((long)sy) + "|cameraPresent=" + ((long)present));
                    MIL.MdigFree(dig);
                }
            }

            MIL.MsysAlloc(app, MIL.M_SYSTEM_HOST, MIL.M_DEFAULT, MIL.M_DEFAULT, ref host);
            Console.WriteLine("HOST|alloc=" + (!IsNull(host)));
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION|" + ex.GetType().Name + ": " + ex.Message); }
        finally
        {
            try { if (!IsNull(host)) MIL.MsysFree(host); } catch { }
            try { if (!IsNull(sys))  MIL.MsysFree(sys);  } catch { }
            try { if (!IsNull(app))  MIL.MappFree(app);  } catch { }
        }
    }
}
