using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Models
{
    public class PsdLayerAdjusmentInfo
    {
        #region Construct
        public PsdLayerAdjusmentInfo(string key, byte[] data)
        {
            Key = key;
            Data = data;
        }
        #endregion
        #region Properties
        public string Key { get; private set; }
        public byte[] Data { get; private set; }
        #endregion
    }
}
