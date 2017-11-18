using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Muho.PSD.Models
{
    public class PsdLayerMask
    {
        #region Private variables
        private static readonly int PositionIsRelativeBit = BitVector32.CreateMask();
        private static readonly int DisabledBit = BitVector32.CreateMask(PositionIsRelativeBit);
        private static readonly int _invertOnBlendBit = BitVector32.CreateMask(DisabledBit);
        private BitVector32 flags;

        FileReader reader;
        private uint offsetBytes = 0;
        #endregion
        #region Construct
        public PsdLayerMask(FileReader fileReader, uint offset, uint size)
        {
            reader = fileReader;
            Offset = offset;
            offsetBytes = offset;
            Size = size;

            setRectangle();
            DefaultColor = reader.ReadRangeWithOffset(1, ref offsetBytes).First();
            byte _flags = reader.ReadRangeWithOffset(1, ref offsetBytes).First();
            flags = new BitVector32(_flags);

        }
        #endregion
        #region Properties
        public uint Offset { get; private set; }
        public uint Size { get; private set; }

        public Rect Rectangle { get; private set; }
        public byte DefaultColor { get; private set; }

        public bool PositionIsRelative
        {
            get
            {
                return flags[PositionIsRelativeBit];
            }
            private set
            {
                flags[PositionIsRelativeBit] = value;
            }
        }
        public bool Disabled
        {
            get { return flags[DisabledBit]; }
            private set { flags[DisabledBit] = value; }
        }
        public bool InvertOnBlendBit
        {
            get { return flags[_invertOnBlendBit]; }
            private set { flags[_invertOnBlendBit] = value; }
        }
        public byte[] ImageData { get; set; }
        #endregion
        #region Methods
        private void setRectangle()
        {
            var Y = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
            var X = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32();
            var H = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32() - Y;
            var W = reader.ReadRangeWithOffset(4, ref offsetBytes).AsInt32() - X;

            Rectangle = new Rect(X, Y, W, H);
        }
        #endregion
    }
}
