using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Sections
{
    public class ColorModeDataSection : IPsdSection
    {
        #region Private Variables
        FileReader reader;
        #endregion
        #region Construct
        public ColorModeDataSection(FileReader fileReader, uint offset, uint size)
        {
            reader = fileReader;
            Offset = offset;
            Size = size;
        }
        #endregion
        #region Properties
        public uint Offset { get; private set; }
        public uint Size { get; private set; }
        #endregion
        #region Methods

        #endregion
    }
}
