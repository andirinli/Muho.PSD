using Muho.PSD.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Models
{
    public class PsdImageResourceBase
    {
        #region Private Variables
        #endregion
        #region Construct
        public PsdImageResourceBase()
        {
            
        }
        #endregion
        #region Properties
        internal virtual FileReader Reader { get; set; }
        public virtual string Signature { get; internal set; }
        public virtual eResourceId Id { get; internal set; }
        public virtual string Name { get; internal set; }
        public virtual uint Offset { get; internal set; }
        public virtual uint Size { get; internal set; }
        #endregion
        #region Methods

        #endregion
    }
}
