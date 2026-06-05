using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;

namespace QMC.Parts
{
    public interface IContainServiceExecutor : IElement
    {
        string ServiceName { get; set; }
    }
}
