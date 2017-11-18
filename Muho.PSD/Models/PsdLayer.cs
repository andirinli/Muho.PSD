using Muho.PSD.Sections;
using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Muho.PSD.Models
{
    public class PsdLayer
    {
        #region Private Variables
        private static readonly int ProtectTransBit = BitVector32.CreateMask();
        private static readonly int VisibleBit = BitVector32.CreateMask(ProtectTransBit);
        private BitVector32 flags;

        FileReader reader;
        private uint offsetBytes = 0;
        private byte[] imageData;
        #endregion
        #region Construct
        public PsdLayer(FileReader fileReader, LayersSubSection parent)
        {
            reader = fileReader;
            Parent = parent;
            Channels = new List<PsdLayerChannel>();
        }
        #endregion
        #region Properties
        internal LayersSubSection Parent { get; private set; }
        public List<PsdLayerChannel> Channels { get; private set; }

        public Rect Rectangle { get; internal set; }
        public string Signature { get; internal set; }
        public string BlendModeKey { get; internal set; }
        public byte Opacity { get; internal set; }
        public bool Clipping { get; internal set; }
        internal byte Flags
        {
            set
            {
                flags = new BitVector32(value);
            }
        }
        public bool Visible
        {
            get { return !flags[VisibleBit]; }
            private set { flags[VisibleBit] = !value; }
        }
        public bool ProtectTransparency
        {
            get { return flags[ProtectTransBit]; }
            set { flags[ProtectTransBit] = value; }
        }

        public uint DataSize { get; internal set; }
        public uint DataOffset { get; internal set; }

        public PsdLayerMask MaskData { get; private set; }
        public PsdLayerBlendingRanges BlendingRanges { get; private set; }
        public string Name { get; private set; }
        public List<PsdLayerAdjusmentInfo> Adjustments { get; private set; }

        public byte[] ImageBytes
        {
            get
            {
                if (imageData == null)
                {
                    var accel = Psd.accelerator;
                    var psd = Parent.Parent.Parent;

                    ///Stride * Height;
                    int channelByteSize = (int)(Rectangle.Width * Rectangle.Height * (psd.Depth / 8));

                    var channels = Channels.Where(w => (w.ChannelData != null) && (w.ChannelData.Length > 0))
                                           .Select(s => s.ChannelData)
                                           .ToArray();
                    if (channels.Length > 0)
                        imageData = accel.RenderPsd(channels, psd.ColorMode);
                }
                return imageData;
            }
        }
        public BitmapSource Image
        {
            get
            {
                if (ImageBytes == null)
                    return null;

                int xDpi = 96;
                int yDpi = 96;
                var psd = Parent.Parent.Parent;
                var rinfo = psd.ImageResources.Resources.OfType<PsdResolutionInfo>().FirstOrDefault(f => f.Id == eResourceId.ResolutionInfo);
                if (rinfo != null)
                {
                    xDpi = rinfo.HRes;
                    yDpi = rinfo.VRes;
                }
                ///NOT: resim dataları accelerator içerisinden default olarak 
                ///BGRA32 olarak dönüşüp geliyor o yüzden her bir pixel 4 byte
                var imgStride = (int)(Rectangle.Width * 4);
                return BitmapSource.Create((int)Rectangle.Width, (int)Rectangle.Height, xDpi, yDpi, PixelFormats.Bgra32, null, ImageBytes, imgStride);
            }
        }

        #endregion
        #region Methods
        internal PsdLayer Load()
        {
            offsetBytes = DataOffset;
            #region Mask Data
            ///I need example file. i test my files but its always zero
            var maskLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
            if (maskLength > 0)
            {
                MaskData = new PsdLayerMask(reader, offsetBytes, (uint)maskLength);

                ///NOT: bu kısım offseti düzgün ayarlamak için.
                ///bazen maskdata içerisinde okunan miktar ayırılan miktardan küçük olabiliyor.
                ///örneğin 20 byte maskLength ayrılıyor ama 18 byte okunuyor
                offsetBytes += (uint)maskLength;
            }
            #endregion
            #region BlendingRanges
            var blendRangeLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
            if (blendRangeLength > 0)
            {
                var blendData = reader.ReadRangeWithOffset((uint)blendRangeLength, ref offsetBytes);
                BlendingRanges = new PsdLayerBlendingRanges(blendData);
            }
            #endregion
            #region Name and offset sync
            var namePos = offsetBytes;
            Name = reader.ReadPascalStringWithOffset(ref offsetBytes);
            ///Layer padding after Name
            offsetBytes += ((offsetBytes - namePos) % 4);
            #endregion
            #region AdjusmentInfo
            Adjustments = new List<PsdLayerAdjusmentInfo>();
            var adjustmenLayerEndPos = DataOffset + DataSize;
            while (offsetBytes < adjustmenLayerEndPos)
            {
                var sign = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
                if (sign != "8BIM")
                    throw new Exception("Could not read an image resource");

                var adjustKey = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
                var adjustDataLen = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
                if (adjustDataLen > 0)
                {
                    var adjustData = reader.ReadRangeWithOffset(adjustDataLen, ref offsetBytes);
                    Adjustments.Add(new PsdLayerAdjusmentInfo(adjustKey, adjustData));
                }
            }
            #endregion
            return this;
        }
        #endregion
    }
}
