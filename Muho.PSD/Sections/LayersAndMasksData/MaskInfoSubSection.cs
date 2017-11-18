using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Sections
{
    public class MaskInfoSubSection
    {
        #region Private Variables
        FileReader reader;
        private uint offsetBytes = 0;
        #endregion
        #region Construct
        public MaskInfoSubSection(FileReader fileReader, uint offset, uint size)
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
        #endregion
        #region Methods
        private void construct()
        {

        }
        #endregion
    }
}
