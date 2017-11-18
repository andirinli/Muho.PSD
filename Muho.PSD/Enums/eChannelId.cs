using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD
{
    public enum eChannelId : short
    {
        TransparencyMask                = -1,
        LayerOrVectorMask               = -2,
        LayerOrVectorMaskHasVectorMask  = -3,
        Red                             = 0,
        Green                           = 1,
        Blue                            = 2
    }
}
