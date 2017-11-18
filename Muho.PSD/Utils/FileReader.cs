using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Utils
{
    public class FileReader
    {
        #region Private Variables
        FileInfo fin;
        private MemoryMappedFile mmf;
        private MemoryMappedViewStream view;
        private bool x64 = Environment.Is64BitProcess;
        #endregion
        #region Construct
        public FileReader(string filePath)
        {
            fin = new FileInfo(filePath);
            mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, string.Format("{0}-{1}", fin.Name, DateTime.Now.ToFileTime()));
            Length = fin.Length;
        }
        #endregion
        #region Properties
        public long Length { get; private set; }
        #endregion
        #region Methods
        public string ReadPascalStringWithOffset(ref uint offsetBytes)
        {
            string result = "";
            byte nameLen = ReadRangeWithOffset(1, ref offsetBytes).First();
            if (nameLen > 0)
                result = ReadRangeWithOffset(nameLen, ref offsetBytes).AsString();

            if (nameLen % 2 == 0) offsetBytes++;

            return result;
        }
        public byte[] ReadRangeWithOffset(uint size, ref uint offsetBytes)
        {
            var result = ReadRange(offsetBytes, size);
            offsetBytes += size;
            return result;
        }
        public byte[] ReadRange(long offset, uint size, bool reverse = false)
        {
            return ReadRange(offset, (int)size, reverse);
        }
        public byte[] ReadRange(long offset, int size, bool reverse = false)
        {
            if ((fin == null) || (mmf == null))
                throw new NullReferenceException("Dispose must be after lazy load operation.");

            var check = (offset + size) <= fin.Length;
            if (!check)
                throw new IndexOutOfRangeException("Offset + size out of range");

            if (size > Consts.MaxArrayLength)
                throw new ArgumentOutOfRangeException(string.Format("Max Size : {0}", Consts.MaxArrayLength));

            using (view = mmf.CreateViewStream(offset, size))
            {
                var buf = new byte[size];
                var ms = new MemoryStream(buf);
                view.CopyTo(ms);

                if (reverse)
                {
                    var reversed = ms.ToArray();
                    Array.Reverse(reversed);
                    return reversed;
                }
                return ms.ToArray();
            }
        }
        #endregion
    }
}
