using Muho.PSD.Models;
using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Sections
{
    public class ImageResourcesSection : IPsdSection
    {
        #region Private Variables
        FileReader reader;
        private uint offsetBytes = 0;
        #endregion
        #region Construct
        public ImageResourcesSection(FileReader fileReader, uint offset, uint size)
        {
            reader = fileReader;
            Offset = offset;
            offsetBytes = offset;
            Size = size;

            construct();

        }
        #endregion
        #region Properties
        public uint Offset { get; private set; }
        public uint Size { get; private set; }
        public List<PsdImageResourceBase> Resources { get; set; }
        #endregion
        #region Methods
        private void construct()
        {
            Resources = new List<PsdImageResourceBase>();
            uint offset = offsetBytes;

            PsdImageResourceBase res;

            while ((offsetBytes - offset) < Size)
            {
                res = new PsdImageResourceBase();
                res.Reader = reader;
                res.Signature = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
                res.Id = (eResourceId)reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
                res.Name = reader.ReadPascalStringWithOffset(ref offsetBytes);
                res.Size = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
                res.Offset = offsetBytes;
                #region for sync
                offsetBytes += res.Size;
                if (offsetBytes % 2 == 1) offsetBytes++;
                #endregion

                switch (res.Id)
                {
                    case eResourceId.ResolutionInfo:
                        var rif = new PsdResolutionInfo(res);
                        Resources.Add(rif);
                        break;
                    case eResourceId.Thumbnail1:
                    case eResourceId.Thumbnail2:
                        var th = new PsdThumbnail(res);
                        Resources.Add(th);
                        break;
                    case eResourceId.AlphaChannelNames:
                        var alpha = new PsdAlphaChannel(res);
                        Resources.Add(alpha);
                        break;
                    default:
                        Resources.Add(res);
                        break;
                }

            }

        }
        #endregion
    }
}
