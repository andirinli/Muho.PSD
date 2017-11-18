using Muho.PSD.Models;
using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Muho.PSD.Sections
{
    public class LayersSubSection : IPsdSection
    {
        #region Private Variables
        FileReader reader;
        private uint offsetBytes = 0;
        Stopwatch sw;
        #endregion
        #region Construct
        public LayersSubSection(FileReader fileReader, uint offset, uint size, LayersAndMasksSection parent)
        {
            reader = fileReader;
            Offset = offset;
            offsetBytes = offset;
            Size = size;
            Parent = parent;
            construct();
        }
        #endregion
        #region Properties
        public uint Offset { get; private set; }
        public uint Size { get; private set; }
        internal LayersAndMasksSection Parent { get; private set; }

        public short LayerCount { get; private set; }
        public bool AbsoluteAlpha { get; private set; }
        public List<PsdLayer> Layers { get; private set; }

        #endregion
        #region Methods
        private void construct()
        {
            LayerCount = reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
            if (LayerCount == 0)
                return;

            if (Parent.Parent.DebugMode)
                sw = Stopwatch.StartNew();


            if (LayerCount < 0)
            {
                LayerCount = Math.Abs(LayerCount);
                AbsoluteAlpha = true;
            }
            Layers = new List<PsdLayer>();
            #region Layers First Read
            for (int i = 0; i < LayerCount; i++)
            {
                var layer = new PsdLayer(reader, this);


                #region Rectangle
                var Y = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
                var X = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
                var H = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32() - Y;
                var W = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32() - X;
                layer.Rectangle = new Rect(X, Y, W, H);
                #endregion
                #region Channels
                var numberOfChannels = reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
                ///Layer içindeki kanallar oluşturuldu. 
                ///Sıralama çok önemli sakın yer değiştirme
                for (int c = 0; c < numberOfChannels; c++)
                {
                    var channel = new PsdLayerChannel(layer)
                    {
                        Id = (eChannelId)reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort(),
                        DataLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32()
                    };

                    layer.Channels.Add(channel);
                }
                #endregion
                #region Others
                var sign = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
                var blendKey = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
                var opacity = reader.ReadRangeWithOffset(1, ref offsetBytes).First();
                var clipping = reader.ReadRangeWithOffset(1, ref offsetBytes).First() > 0;
                var flagByte = reader.ReadRangeWithOffset(1, ref offsetBytes).First();

                ///offset Ayarlaması için gerekli
                reader.ReadRangeWithOffset(1, ref offsetBytes);

                #endregion
                #region Extra Data
                var extraDataSize = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
                var extraDataOffset = offsetBytes;
                offsetBytes += extraDataSize;
                #endregion

                layer.Signature = sign;
                layer.BlendModeKey = blendKey;
                layer.Opacity = opacity;
                layer.Clipping = clipping;
                layer.Flags = flagByte;
                layer.DataOffset = extraDataOffset;
                layer.DataSize = extraDataSize;

                ///NOT bu kısım normal şartlar altında kendi içinde offset ile çalışıyor.
                ///fakat sıralama önemli olduğu için birçok veriyi okumak gerekiyor.
                ///daha sonra duruma göre içeride lazyload yapılabilir.
                layer.Load();

                Layers.Add(layer);
            }
            #endregion

            if (Parent.Parent.DebugMode)
            {
                sw.Stop();
                Parent.Parent.DebugLog.Add(string.Format("Tüm Layer Listesi Channel Olmadan Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }


            #region Layers Set Pixels Data
            foreach (var layer in Layers)
            {
                foreach (var channel in layer.Channels.Where(w => w.Id != eChannelId.LayerOrVectorMask))
                {
                    ///NOT: Bu 2 byte veri zaten kanal datası içerisinde mevcut 
                    ///o yüzden offset değiştirmek hatalı sonuç verecektir.
                    //channel.ImageCompression = (eImageCompression)reader.ReadRange(2, offsetBytes).AsShort();
                    channel.Data = reader.ReadRangeWithOffset(channel.DataLength, ref offsetBytes);
                }
                if (layer.MaskData != null)
                {
                    ///Maskdata kısmı kaldı.
                    ///sakin kafa ile bakmak lazım.

                }
            }

            if (Parent.Parent.DebugMode)
            {
                sw.Stop();
                Parent.Parent.DebugLog.Add(string.Format("Tüm Layer Listesi Channel Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
            }

            if (offsetBytes % 2 == 1)
                offsetBytes++;
            #endregion

        }
        #endregion
    }
}
