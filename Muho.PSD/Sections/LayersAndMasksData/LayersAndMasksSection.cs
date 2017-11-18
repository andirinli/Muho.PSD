using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Sections
{
    public class LayersAndMasksSection : IPsdSection
    {
        #region Private Variables
        FileReader reader;
        private uint offsetBytes = 0;
        Stopwatch sw;
        #endregion
        #region Construct
        public LayersAndMasksSection(FileReader fileReader, uint offset, uint size, Psd parent)
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
        internal Psd Parent { get; private set; }
        public LayersSubSection LayersInfo { get; private set; }
        public MaskInfoSubSection MaskInfo { get; private set; }
        #endregion
        #region Methods
        private void construct()
        {
            if (Parent.DebugMode)
                sw = Stopwatch.StartNew();

            var layersInfoLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            if (layersInfoLength > 0)
            {
                LayersInfo = new LayersSubSection(reader, offsetBytes, layersInfoLength, this);
                offsetBytes += layersInfoLength;
            }

            if (Parent.DebugMode)
            {
                sw.Stop();
                Parent.DebugLog.Add(string.Format("Layer SubSection Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
                sw.Restart();
            }


            var maskLength = reader.ReadRangeWithOffset(4, ref offsetBytes).AsUInt32();
            if (maskLength > 0)
            {
                MaskInfo = new MaskInfoSubSection(reader, offsetBytes, maskLength);
                offsetBytes += maskLength;
            }

            if (Parent.DebugMode)
            {
                sw.Stop();
                Parent.DebugLog.Add(string.Format("Mask Info SubSection Okuma Süresi : {0} ms", sw.ElapsedMilliseconds));
            }

        }
        #endregion
    }
}
