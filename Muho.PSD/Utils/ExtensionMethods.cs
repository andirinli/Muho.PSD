using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Muho.PSD
{
    public static class ExtensionMethods
    {
        public static BitmapSource AsImage(this byte[] imageBytes)
        {

            ///NOT: Doğrudan bitmapImage kullanamıyorum metadata sıkıntısı çıkarıyor.
            ///zaten thumbnail büyük bişey değil o yüzden bu şekilde yaptım.
            BitmapImage bmpImage = new BitmapImage();
            MemoryStream mystream = new MemoryStream(imageBytes);
            bmpImage.BeginInit();
            bmpImage.StreamSource = mystream;
            bmpImage.EndInit();
            int stride = bmpImage.PixelWidth * (bmpImage.Format.BitsPerPixel / 8);
            int totalByte = stride * bmpImage.PixelHeight;
            var buffer = new byte[totalByte];
            bmpImage.CopyPixels(buffer, stride, 0);

            var bs = BitmapSource.Create(bmpImage.PixelWidth, bmpImage.PixelHeight, bmpImage.DpiX, bmpImage.DpiY, bmpImage.Format, null, buffer, stride);
            return bs;
        }
        public static byte[] ToSwapBytes(this byte[] bytes)
        {
            Array.Reverse(bytes);
            return bytes;
        }

        public static short AsShort(this IEnumerable<byte> bytes, bool reverse = true)
        {
            return bytes.ToArray().AsShort(reverse);
        }
        public static short AsShort(this byte[] bytes, bool reverse = true)
        {
            if (reverse)
                Array.Reverse(bytes);

            return BitConverter.ToInt16(bytes, 0);
        }

        public static ushort AsUShort(this IEnumerable<byte> bytes, bool reverse = true)
        {
            return bytes.ToArray().AsUShort(reverse);
        }
        public static ushort AsUShort(this byte[] bytes, bool reverse = true)
        {
            if (reverse)
                Array.Reverse(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        public static int AsInt32(this IEnumerable<byte> bytes, bool reverse = true)
        {
            return bytes.ToArray().AsInt32(reverse);
        }
        public static int AsInt32(this byte[] bytes, bool reverse = true)
        {
            if (reverse)
                Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        public static uint AsUInt32(this IEnumerable<byte> bytes, bool reverse = true)
        {
            return bytes.ToArray().AsUInt32(reverse);
        }
        public static uint AsUInt32(this byte[] bytes, bool reverse = true)
        {
            if (reverse)
                Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }


        public static string AsString(this IEnumerable<byte> bytes, bool firstByteLength = false)
        {
            return bytes.ToArray().AsString(firstByteLength);
        }
        public static string AsString(this byte[] bytes, bool firstByteLength = false)
        {
            if (!firstByteLength)
                return Encoding.Default.GetString(bytes);
            return Encoding.Default.GetString(bytes.Skip(1).ToArray());
        }



        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {

            var sourceProps = source.GetType().GetProperties().Where(x => x.CanRead).ToList();
            var destProps = dest.GetType().GetProperties()
                    .Where(x => x.CanWrite)
                    .ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.All(x => x.Name != sourceProp.Name)) continue;
                var p = destProps.First(x => x.Name == sourceProp.Name);
                p.SetValue(dest, sourceProp.GetValue(source, null), null);
            }

        }

    }
}
