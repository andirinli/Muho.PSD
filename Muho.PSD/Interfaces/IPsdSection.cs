using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD
{
    public interface IPsdSection
    {
        uint Offset { get;}
        uint Size { get;}
    }
}
