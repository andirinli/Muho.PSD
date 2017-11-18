using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Models
{
    public class PsdAlphaChannel : PsdImageResourceBase
    {
        #region Private Variables
        private uint offsetBytes = 0;
        #endregion
        public PsdAlphaChannel(PsdImageResourceBase resource)
        {
            resource.CopyPropertiesTo(this);
            Reader = resource.Reader;
            offsetBytes = resource.Offset;

            ChannelNames = new List<string>();
            uint offset = offsetBytes;
            while ((offsetBytes - offset) < Size)
            {
                var len = Reader.ReadRangeWithOffset(1, ref offsetBytes).First();
                if (len > 0)
                {
                    var str = Reader.ReadRangeWithOffset(len, ref offsetBytes).AsString();
                    ChannelNames.Add(str);
                }
            }

        }

        #region Properties
        public List<string> ChannelNames { get; private set; }
        #endregion

    }
}
