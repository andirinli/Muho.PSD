using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muho.PSD.Utils;

namespace Muho.PSD.Models
{
    public class PsdResolutionInfo : PsdImageResourceBase
    {
        public PsdResolutionInfo(PsdImageResourceBase resource)
        {
            resource.CopyPropertiesTo(this);

            Reader = resource.Reader;

            var bytes = Reader.ReadRange(Offset, Size);
            HRes = bytes.Skip(0).Take(2).ToArray().AsShort();
            HResUnit = (eResolutionUnit)bytes.Skip(2).Take(4).ToArray().AsInt32();
            WidthUnit = (eMeasureUnit)bytes.Skip(6).Take(2).ToArray().AsShort();

            VRes = bytes.Skip(8).Take(2).ToArray().AsShort();
            VResUnit = (eResolutionUnit)bytes.Skip(10).Take(4).ToArray().AsInt32();
            HeightUnit = (eMeasureUnit)bytes.Skip(14).Take(2).ToArray().AsShort();
        }
        public short HRes { get; private set; }
        public short VRes { get; private set; }

        public eResolutionUnit HResUnit { get; private set; }
        public eResolutionUnit VResUnit { get; private set; }

        public eMeasureUnit WidthUnit { get; private set; }
        public eMeasureUnit HeightUnit { get; private set; }
    }
}
