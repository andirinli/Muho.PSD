using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Muho.PSD.Models
{
    public class PsdThumbnail : PsdImageResourceBase
    {
        #region Private Variables
        private uint offsetBytes = 0;
        private uint size = 0;
        private byte[] imageData = null;
        #endregion
        public PsdThumbnail(PsdImageResourceBase resource)
        {
            resource.CopyPropertiesTo(this);
            Reader = resource.Reader;
            offsetBytes = resource.Offset;

            Format = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
            Width = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            Height = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            WidthBytes = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            TotalSize = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            CompressedSize = Reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            Bpp = Reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
            PlaneCount = Reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();

            size = Size - (offsetBytes - Offset);
        }
        #region Properties
        public int Format { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint WidthBytes { get; private set; }
        public uint TotalSize { get; private set; }
        public uint CompressedSize { get; set; }
        public short Bpp { get; private set; }
        public short PlaneCount { get; private set; }

        public BitmapSource Image
        {
            get
            {
                if ((Format != 1) || (size <= 0))
                    return new WriteableBitmap((int)Width, (int)Height, 72, 72, PixelFormats.Rgb24, null);

                if (imageData == null)
                    imageData = Reader.ReadRange(offsetBytes, size);

                return imageData.AsImage();
            }
        }

        #endregion
    }
}
