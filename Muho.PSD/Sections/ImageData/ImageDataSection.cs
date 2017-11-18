using Muho.PSD.Accelerators;
using Muho.PSD.Models;
using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Muho.PSD.Sections
{
    public class ImageDataSection : IPsdSection
    {
        #region Private Variables
        private FileReader reader;
        private uint offsetBytes = 0;
        private byte[] imageData;
        private byte[][] imageDataChannels;

        #endregion
        #region Construct
        public ImageDataSection(FileReader fileReader, uint offset, uint size, Psd parent)
        {
            Parent = parent;

            reader = fileReader;
            offsetBytes = offset;
            //Debug.WriteLine(string.Format("Construct Offset : {0} , Size : {1}", Offset, Size));
            Compression = (eImageCompression)reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();

            Offset = offsetBytes;

            if (Compression == eImageCompression.Rle)
            {
                Offset += (uint)(parent.Height * parent.ChannelCount * 2);
            }

            ///Size kısmını sonunda çözdüm. Dosyanın artık en sonda kalan kısmını kullanacağım için.
            ///sadece sondaki veriyi almam yeterli geldi. Şimdi asıl sıkıntı RLE
            Size = (uint)(reader.Length - Offset);

            ///Photoshop denen ibne tüm renk kanallarının byte array değerlerini ayrı ayrı tutuyor.
            ///yani Red değerleri ayrı, Green değerleri ayrı, Blue Değerleri ayrı bir array içinde.
            ///tabi bu herzaman RGB olmak zorunda değil. CMYK felan da olabiliyor. bknz:eColorModes
            imageDataChannels = new byte[parent.ChannelCount][];

            ///Her bir kanala düşen bayt miktarı.
            var eachChannelSize = Size / parent.ChannelCount;
        }
        #endregion
        #region Properties
        internal Psd Parent { get; private set; }

        public eImageCompression Compression { get; private set; }
        public uint Offset { get; internal set; }
        public uint Size { get; private set; }

        public byte[] ImageBytes
        {
            get
            {
                if (imageData == null)
                {
                    var accel = Psd.accelerator;
                    ///Stride * Height;
                    int channelByteSize = (int)(Parent.Width * Parent.Height * (Parent.Depth / 8));

                    ///daha sonra parçalanacak
                    byte[] fullData = null;

                    switch (Compression)
                    {
                        case eImageCompression.Raw:
                            fullData = reader.ReadRange(Offset, Size);
                            break;
                        case eImageCompression.Rle:
                            ///Sıkıştırılmış veri alındı.
                            var compressedData = reader.ReadRange(Offset, Size);
                            ///Açıldıktan sonraki boyutu hesaplandı.
                            var resultSize = (channelByteSize * Parent.ChannelCount);
                            ///OPENCL ile unpack yapıldı. ortalama 10 kat daha hızlı olduğu görüldü.
                            fullData = accel.UnpackAllkBytes(compressedData, resultSize);
                            break;
                        case eImageCompression.Zip:
                        case eImageCompression.ZipPrediction:
                            throw new NotSupportedException();
                    }

                    ///Parçalara ayırma işlemi. Daha sonra gerekirse pointer ile yapılabilir. 
                    ///ama şimdilik bir yavaşlama göremedim.
                    for (int i = 0; i < imageDataChannels.Length; i++)
                    {
                        imageDataChannels[i] = new byte[channelByteSize];
                        var sourceIndex = i * channelByteSize;
                        Array.Copy(fullData, sourceIndex, imageDataChannels[i], 0, channelByteSize);
                    }

                    imageData = accel.RenderPsd(imageDataChannels, Parent.ColorMode);
                }

                return imageData;
            }
        }
        public BitmapSource Image
        {
            get
            {
                int xDpi = 96;
                int yDpi = 96;
                var rinfo = Parent.ImageResources.Resources.OfType<PsdResolutionInfo>().FirstOrDefault(f => f.Id == eResourceId.ResolutionInfo);
                if (rinfo != null)
                {
                    xDpi = rinfo.HRes;
                    yDpi = rinfo.VRes;
                }
                ///NOT: resim dataları accelerator içerisinden default olarak 
                ///BGRA32 olarak dönüşüp geliyor o yüzden her bir pixel 4 byte
                var imgStride = (int)(Parent.Width * 4);
                return BitmapSource.Create((int)Parent.Width, (int)Parent.Height, xDpi, yDpi, PixelFormats.Bgra32, null, ImageBytes, imgStride);

            }
        }
        #endregion
        #region Methods
        private byte[][] GetImageChannelsData()
        {
            var result = new byte[Parent.ChannelCount][];

            var accel = OpenCLAccelerator.CpuAccelerator;

            Stopwatch sw = Stopwatch.StartNew();

            ///Stride * Height;
            int channelByteSize = (int)(Parent.Width * Parent.Height * (Parent.Depth / 8));

            byte[] fullData = null;

            switch (Compression)
            {
                case eImageCompression.Raw:
                    fullData = reader.ReadRange(Offset, Size);
                    break;
                case eImageCompression.Rle:
                    sw.Restart();
                    var compressedData = reader.ReadRange(Offset, Size);
                    sw.Stop();
                    sw.Restart();
                    fullData = accel.UnpackAllkBytes(compressedData, (channelByteSize * Parent.ChannelCount));
                    sw.Stop();
                    break;
            }

            sw.Restart();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new byte[channelByteSize];
                var sourceIndex = i * channelByteSize;
                Array.Copy(fullData, sourceIndex, result[i], 0, channelByteSize);
            }

            sw.Stop();


            imageData = accel.RenderPsd(result, Parent.ColorMode);

            return result;
        }
        #endregion
    }
}
