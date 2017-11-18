using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Models
{
    public class PsdLayerChannel
    {
        #region Private Variables
        private byte[] _data;
        #endregion
        #region Construct
        public PsdLayerChannel(PsdLayer parent)
        {
            Parent = parent;
        }
        #endregion
        #region Properties
        internal PsdLayer Parent { get; private set; }

        public eChannelId Id { get; internal set; }
        public uint DataLength { get; internal set; }

        public eImageCompression ImageCompression { get; internal set; }
        internal byte[] Data
        {
            set
            {
                var accel = Psd.accelerator;
                var psd = Parent.Parent.Parent.Parent;
                var bytesPerRow = Parent.Rectangle.Width * (psd.Depth / 8);

                ImageCompression = (eImageCompression)value.Take(2).AsShort();
                switch (ImageCompression)
                {
                    case eImageCompression.Raw:
                        ///NOT +2 byte ImageCompression verisi için kullanıldı hesaptan çıkarmak lazım.
                        _data = value.Skip(2).ToArray();
                        break;
                    case eImageCompression.Rle:
                        ///NOT : eğer kanal rle kullanılarak sıkıştırılmış ise
                        ///Yüksekliğin 2 katı kadar es geçmek gerekiyor
                        ///+2 byte ImageCompression verisi için kullanıldı hesaptan çıkarmak lazım.
                        var skipCount = (int)(Parent.Rectangle.Height * 2) + 2;

                        var compressedData = value.Skip(skipCount).ToArray();
                        int resultSize = (int)(Parent.Rectangle.Height * bytesPerRow);
                        _data = accel.UnpackAllkBytes(compressedData, resultSize);
                        break;
                    case eImageCompression.Zip:
                    case eImageCompression.ZipPrediction:
                        throw new NotSupportedException();
                }
            }
        }
        public byte[] ChannelData
        {
            get
            {
                return _data;
            }
        }
        #endregion
    }
}
