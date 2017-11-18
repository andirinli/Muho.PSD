using Muho.PSD.Accelerators;
using Muho.PSD.Sections;
using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD
{
    /// <summary>
    /// http://www.adobe.com/devnet-apps/photoshop/fileformatashtml/PhotoshopFileFormats.htm
    /// </summary>
    public class Psd
    {
        #region Internal Variables
        internal static OpenCLAccelerator accelerator = OpenCLAccelerator.CpuAccelerator;
        #endregion
        #region Private Variables
        private FileReader reader;
        private bool x64 = Environment.Is64BitProcess;
        uint offsetBytes = 0;
        Stopwatch sw;
        #endregion
        #region Construct
        public Psd(string filePath, bool debugMode = false)
        {
            DebugMode = debugMode;

            if (debugMode)
                sw = Stopwatch.StartNew();


            reader = new FileReader(filePath);

            Signature = reader.ReadRangeWithOffset(4, ref offsetBytes).AsString();
            if (Signature != "8BPS") throw new IOException("Bad or invalid file");

            Version = reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
            if (Version != 1) throw new IOException("Invalid version number supplied");

            //6 bytes reserverd in PSD format
            offsetBytes += 6;

            ChannelCount = reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
            Height = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            Width = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            Depth = reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();
            ColorMode = (eColorModes)reader.ReadRangeWithOffset(2, ref offsetBytes).AsShort();


            if (debugMode)
            {
                sw.Stop();
                DebugLog.Add(string.Format("Header Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }


            #region ColorModeData
            uint ColorModeDataLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            if (ColorModeDataLength > 0)
            {
                ColorModeData = new ColorModeDataSection(reader, offsetBytes, ColorModeDataLength);
                offsetBytes += ColorModeDataLength;
            }
            #endregion

            if (debugMode)
            {
                sw.Stop();
                DebugLog.Add(string.Format("Color Mode Data Section Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }

            #region ImageResources
            uint imgResLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            if (imgResLength > 0)
            {
                ImageResources = new ImageResourcesSection(reader, offsetBytes, imgResLength);
                offsetBytes += imgResLength;
            }
            #endregion

            if (debugMode)
            {
                sw.Stop();
                DebugLog.Add(string.Format("Image Resources Section Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }


            ///Posizyon kontrolü için.
            uint positionCheck = 0;

            #region LayersAndMasks
            uint layerAndMaskLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();

            positionCheck = offsetBytes;
            if (layerAndMaskLength > 0)
            {
                LayersAndMasks = new LayersAndMasksSection(reader, offsetBytes, layerAndMaskLength, this);
                offsetBytes += layerAndMaskLength;
            }
            #endregion
            positionCheck += layerAndMaskLength;


            if (debugMode)
            {
                sw.Stop();
                DebugLog.Add(string.Format("Layers And Masks Section Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }


            #region ImageData
            ImageData = new ImageDataSection(reader, offsetBytes, (uint)(reader.Length - offsetBytes), this);
            #endregion


            if (debugMode)
            {
                sw.Stop();
                DebugLog.Add(string.Format("Image Data Section Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                
            }

        }
        #endregion
        #region Properties
        public string Signature { get; private set; }
        public short Version { get; private set; }
        public short ChannelCount { get; private set; }
        public uint Height { get; private set; }
        public uint Width { get; private set; }
        public int Depth { get; private set; }
        public eColorModes ColorMode { get; private set; }
        #endregion
        #region Sections
        public ColorModeDataSection ColorModeData { get; private set; }
        public ImageResourcesSection ImageResources { get; private set; }
        public LayersAndMasksSection LayersAndMasks { get; private set; }
        public ImageDataSection ImageData { get; private set; }
        #endregion
        #region Debug
        public bool DebugMode { get; private set; }
        public List<string> DebugLog { get; private set; } = new List<string>();
        #endregion
    }
}
