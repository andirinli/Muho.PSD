using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Accelerators
{
    public partial class OpenCLAccelerator
    {
        #region Methods
        public byte[] RenderPsd(byte[][] Channels, eColorModes colorMode, int resultLen = 0)
        {
            if (Channels.Length == 0)
                return null;

            ///Sonuç boyutu kanalların Toplam Boyutuna
            ///BGRA olduğu için 4 ile çarpılıyor
            if (resultLen == 0)
                resultLen = Channels[0].Length * 4;

            ///NOT : channels verisindeki her bir array aynı boyutta
            int workSize = Channels[0].Length;

            IntPtr kernel = IntPtr.Zero;

            ///NOT Psd sonuçları herzaman BGRA32 olarak dönecek
            #region Kernel Seçimi
            switch (colorMode)
            {
                case eColorModes.Bitmap:
                    break;
                case eColorModes.Grayscale:
                    break;
                case eColorModes.Indexed:
                    break;
                case eColorModes.RGB:
                    if (Channels.Length == 3)
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_RGB")).Handle.Value;///Eğer kanal verisi alpha olmadan geldiyse
                    else
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_ARGB")).Handle.Value;///Eğer kanal verisi alpha ile birlikte geldiyse
                    break;
                case eColorModes.CMYK:
                    if (Channels.Length == 4)
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_CMYK")).Handle.Value;
                    else
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_ACMYK")).Handle.Value;
                    break;
                case eColorModes.Multichannel:
                    kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_CMY")).Handle.Value;
                    break;
                case eColorModes.Duotone:
                    break;
                case eColorModes.Lab:
                    if (Channels.Length == 3)
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_LAB")).Handle.Value;
                    else
                        kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("PSD_RenderColors_ALAB")).Handle.Value;
                    break;
                default:
                    break;
            }
            #endregion

            return Execute(kernel, Channels, workSize, resultLen);
        }
        #endregion
    }
}
