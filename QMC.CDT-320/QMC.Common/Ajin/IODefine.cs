using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common
{
    //
    // 요약:
    //     디지털 접점의 값을 지정합니다.
    public enum DioValue
    {
        //
        // 요약:
        //     알수 없는 상태를 나타냅니다.
        Unknown = -1,
        //
        // 요약:
        //     꺼져있는 상태를 나타냅니다.
        Off = 0,
        //
        // 요약:
        //     켜저 있는 상태를 나타냅니다.
        On = 1
    }

    //
    // 요약:
    //     디지털 접점의 변화를 지정합니다.
    public enum DigitalEdge
    {
        //
        // 요약:
        //     오프에서 온으로 변경을 나타냅니다.
        Up = 0,
        //
        // 요약:
        //     온에서 오프로 변경을 나타냅니다.
        Down = 1
    }

    public enum IoType
    {
        Input, 
        Output
    }

    public enum AioItemStyles
    {
        /// <summary>
        /// 숫자를 나타냅니다.
        /// </summary>
        Number,
        /// <summary>
        /// BCD 코드를 나타냅니다.
        /// </summary>
        BcdNumber
    }
}
