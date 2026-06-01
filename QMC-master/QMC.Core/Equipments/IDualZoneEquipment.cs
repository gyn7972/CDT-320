using MechaSys.SoftBricks.LoadPorts;
using System;

namespace QMC.Equipments
{
    /// <summary>
    /// 투입부와 반출부로 나눠져있는 설비를 정의한다.
    /// </summary>
    public interface IDualZoneEquipment
    {
        /// <summary>
        /// 투입부 LoadPort(들)을 가져온다.
        /// </summary>
        LoadPortReadOnlyCollection InputLoadPorts { get; }

        /// <summary>
        /// 반출부 LoadPort(들)을 가져온다.
        /// </summary>
        LoadPortReadOnlyCollection OutputLoadPorts { get; }
    }
}