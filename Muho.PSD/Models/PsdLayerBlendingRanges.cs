using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Models
{
    public class PsdLayerBlendingRanges
    {
        #region Construct
        public PsdLayerBlendingRanges(byte[] data)
        {
            Data = data;
        }
        #endregion
        #region Properties
        public byte[] Data { get; private set; }
        #endregion
    }
}
