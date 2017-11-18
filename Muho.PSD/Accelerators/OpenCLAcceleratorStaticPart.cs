using Cloo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muho.PSD.Accelerators
{
    public partial class OpenCLAccelerator
    {
        #region Private Variables
        private static List<ComputePlatform> platforms;


        private static object _cpuAcceleratorSync = new object();
        private static OpenCLAccelerator _cpuAccelerator;

        private static object _gpuAcceleratorSync = new object();
        private static OpenCLAccelerator _gpuAccelerator;

        #endregion
        #region Instances
        public static OpenCLAccelerator CpuAccelerator
        {
            get
            {
                if (_cpuAccelerator == null)
                {
                    lock (_cpuAcceleratorSync)
                    {
                        if (_cpuAccelerator == null)
                            _cpuAccelerator = new OpenCLAccelerator(CpuDevice);
                    }
                }
                return _cpuAccelerator;
            }
        }
        public static OpenCLAccelerator GpuAccelerator
        {
            get
            {
                if (_gpuAccelerator == null)
                {
                    lock (_gpuAcceleratorSync)
                    {
                        if (_gpuAccelerator == null)
                            _gpuAccelerator = new OpenCLAccelerator(GpuDevice);
                    }
                }
                return _gpuAccelerator;
            }
        }
        #endregion
        #region Static Construct
        static OpenCLAccelerator()
        {
            platforms = ComputePlatform.Platforms.ToList();
        }
        #endregion
        #region Devices & Platforms
        internal static List<ComputePlatform> GPU_Platforms
        {
            get
            {
                return platforms.SelectMany(sm => sm.Devices)
                                .Where(w => w.Type == ComputeDeviceTypes.Gpu)
                                .Select(s => s.Platform)
                                .ToList();
            }
        }
        internal static List<ComputePlatform> CPU_Platforms
        {
            get
            {
                return platforms.SelectMany(sm => sm.Devices)
                                .Where(w => w.Type == ComputeDeviceTypes.Cpu)
                                .Select(s => s.Platform)
                                .ToList();
            }
        }

        internal static List<ComputeDevice> GPU_Devices
        {
            get
            {
                return GPU_Platforms.SelectMany(sm => sm.Devices)
                                    .Where(w => w.Type == ComputeDeviceTypes.Gpu)
                                    .ToList();
            }
        }
        internal static List<ComputeDevice> CPU_Devices
        {
            get
            {
                return CPU_Platforms.SelectMany(sm => sm.Devices)
                                    .Where(w => w.Type == ComputeDeviceTypes.Cpu)
                                    .ToList();
            }
        }

        internal static IntPtr CpuDevice
        {
            get
            {
                return CPU_Devices.FirstOrDefault(f => f.Version < new Version(2, 1)).Handle.Value;
            }
        }

        internal static IntPtr GpuDevice
        {
            get
            {
                var q = GPU_Devices.FirstOrDefault(f => !f.Vendor.Contains("Intel"));
                if (q != null)
                    return q.Handle.Value;
                return GPU_Devices.FirstOrDefault().Handle.Value;
            }
        }
        #endregion
        #region SourceCode
        private static string SourceCode
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(SourceCodeForPsd);
                sb.AppendLine();
                sb.Append(SourceCodeForBenchmark);
                sb.AppendLine();
                sb.AppendLine(SourceCodePackBits);
                sb.AppendLine();

                return sb.ToString();
            }
        }
        private static string SourceCodePackBits
        {
            get
            {
                return @"
                            kernel void UnpackAllData(global uchar* source, global uchar* dest, int sourceLen)
                            {
                                int destPos = 0;
                                int count = 0;
    
                                while(count < sourceLen)
                                {
                                    uchar val = source[count];
        
                                    if(val == 128)
                                    {
                                        count++;
                                    }
                                    else if(val < 128)
                                    {
                                        int stepSize = val + 1;
                                        count++;
            
                                        for(int i = 0; i < stepSize; i++)
                                        {
                                            dest[destPos + i] = source[count + i];
                                        }
            
                                        count += stepSize;
                                        destPos += stepSize;
                                    }
                                    else
                                    {
                                        int copyCount = 257 - val;
                                        count++;
                                        uchar copyVal = source[count];
            
                                        for(int i = 0; i < copyCount; i++)
                                        {
                                            dest[destPos + i] = copyVal;
                                        }
            
                                        count ++;
                                        destPos += copyCount;
                                    }
                                }
                            }
                        ";
            }
        }
        #region PSD Methods
        private static string SourceCodeForPsd
        {
            get
            {
                var result = new StringBuilder();
                result.AppendLine(SourceCodePsdRGB);
                result.AppendLine();
                result.AppendLine(SourceCodePsdCMYK);
                result.AppendLine();
                result.AppendLine(SourceCodePsdCMY);
                result.AppendLine();
                result.AppendLine(SourceCodePsdLAB);
                result.AppendLine();

                return result.ToString();
            }
        }
        private static string SourceCodePsdRGB
        {
            get
            {
                return @"

                                kernel void PSD_RenderColors_RGB(global uchar* srR, global uchar* srG, global uchar* srB, global uchar* dest)
                                {
                                
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    uchar R = srR[id];  
                                    uchar G = srG[id];
                                    uchar B = srB[id];
                                    uchar A = (uchar)255;
                                    
                                    dest[pos] = B;
                                    dest[pos + 1] = G;
                                    dest[pos + 2] = R;
                                    dest[pos + 3] = A;
                                
                                }
                                
                                kernel void PSD_RenderColors_ARGB(global uchar* srA, global uchar* srR, global uchar* srG, global uchar* srB, global uchar* dest)
                                {
                                
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                  
                                    uchar B = srB[id];
                                    uchar G = srG[id];
                                    uchar R = srR[id];
                                    uchar A = srA[id];
                                    
                                    dest[pos] = B;
                                    dest[pos + 1] = G;
                                    dest[pos + 2] = R;
                                    dest[pos + 3] = A;
                                }
                        ";
            }
        }
        private static string SourceCodePsdCMYK
        {
            get
            {
                return @"
                                kernel void PSD_RenderColors_CMYK(global uchar* srC, global uchar* srM, global uchar* srY, global uchar* srK, global uchar* dest)
                                {
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    
                                    uchar c = srC[id];
                                    uchar m = srM[id];
                                    uchar y = srY[id];
                                    uchar k = srK[id];
                                    
                                    float C = ((float)1.0 - ((float)c / (float)256));
                                    float M = ((float)1.0 - ((float)m / (float)256));
                                    float Y = ((float)1.0 - ((float)y / (float)256));
                                    float K = ((float)1.0 - ((float)k / (float)256));
                                    
                                    
                                    int R = (int)((1.0 - (C * (1 - K) + K)) * 255);
                                    int G = (int)((1.0 - (M * (1 - K) + K)) * 255);
                                    int B = (int)((1.0 - (Y * (1 - K) + K)) * 255);
                                    
                                    if (R < 0) R = 0;
                                    else if (R > 255) R = 255;
                                    
                                    if (G < 0) G = 0;
                                    else if (G > 255) G = 255;
                                    
                                    if (B < 0) B = 0;
                                    else if (B > 255) B = 255;
                                    
                                    
                                    dest[pos] = (uchar)B;
                                    dest[pos + 1] = (uchar)G;
                                    dest[pos + 2] = (uchar)R;
                                    dest[pos + 3] = (uchar)255;
                                    
                                }
                                
                                kernel void PSD_RenderColors_ACMYK(global uchar* srA,global uchar* srC, global uchar* srM, global uchar* srY, global uchar* srK, global uchar* dest)
                                {
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    
                                    uchar a = srA[id];
                                    uchar c = srC[id];
                                    uchar m = srM[id];
                                    uchar y = srY[id];
                                    uchar k = srK[id];
                                    
                                    float C = ((float)1.0 - ((float)c / (float)256));
                                    float M = ((float)1.0 - ((float)m / (float)256));
                                    float Y = ((float)1.0 - ((float)y / (float)256));
                                    float K = ((float)1.0 - ((float)k / (float)256));
                                    
                                    
                                    int R = (int)((1.0 - (C * (1 - K) + K)) * 255);
                                    int G = (int)((1.0 - (M * (1 - K) + K)) * 255);
                                    int B = (int)((1.0 - (Y * (1 - K) + K)) * 255);
                                    
                                    if (R < 0) R = 0;
                                    else if (R > 255) R = 255;
                                    
                                    if (G < 0) G = 0;
                                    else if (G > 255) G = 255;
                                    
                                    if (B < 0) B = 0;
                                    else if (B > 255) B = 255;
                                    
                                    
                                    dest[pos] = (uchar)B;
                                    dest[pos + 1] = (uchar)G;
                                    dest[pos + 2] = (uchar)R;
                                    dest[pos + 3] = a;
                                    
                                }  
                        ";
            }
        }
        private static string SourceCodePsdCMY
        {
            get
            {
                return @"
                                kernel void PSD_RenderColors_CMY(global uchar* srC, global uchar* srM, global uchar* srY, global uchar* dest)
                                {
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    
                                    uchar c = srC[id];
                                    uchar m = srM[id];
                                    uchar y = srY[id];
                                
                                    
                                    float C = (float)(c / 255.0);
                                    float M = (float)(m / 255.0);
                                    float Y = (float)(y / 255.0);
                                    
                                    int R = (int)((1.0 - C) * 255);
                                    int G = (int)((1.0 - M) * 255);
                                    int B = (int)((1.0 - Y) * 255);
                                    
                                    if (R < 0) R = 0;
                                    else if (R > 255) R = 255;
                                    
                                    if (G < 0) G = 0;
                                    else if (G > 255) G = 255;
                                    
                                    if (B < 0) B = 0;
                                    else if (B > 255) B = 255;
                                    
                                    
                                    dest[pos] = (uchar)255 - (uchar)B;
                                    dest[pos + 1] = (uchar)255 -(uchar)G;
                                    dest[pos + 2] = (uchar)255 -(uchar)R;
                                    dest[pos + 3] = (uchar)255;
                                    
                                }
                        ";
            }
        }
        private static string SourceCodePsdLAB
        {
            get
            {
                return @"
                                kernel void PSD_RenderColors_LAB(global uchar* srL, global uchar* srA, global uchar* srB, global uchar* dest)
                                {
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    
                                    uchar l = srL[id];
                                    uchar a = srA[id];
                                    uchar b = srB[id];
                                    
                                    float l_coef = (float)2.56;
                                    float a_coef = (float)1.0;
                                    float b_coef = (float)1.0;
                                    
                                    
                                    int Lab_L = (int)(l / l_coef);
                                    int Lab_A = (int)((a / a_coef) - (float)128.0);
                                    int Lab_B = (int)((b / b_coef) - (float)128.0);
                                    
                                    
                                    float ref_X = (float)95.047;
                                    float ref_Y = (float)100.000;
                                    float ref_Z = (float)108.883;
                                    
                                    float var_Y = ((float)Lab_L + (float)16.0) / (float)116.0;
                                    float var_X = (float)Lab_A / (float)500.0 + var_Y;
                                    float var_Z = var_Y - (float)Lab_B / (float)200.0;
                                
                                    
                                    if(powr(var_Y,3) > (float)0.008856)
                                        var_Y = powr(var_Y , 3);
                                    else
                                        var_Y = (var_Y - (float)16 / (float)116) / (float)7.787;
                                    
                                    
                                        
                                    if (powr(var_X, 3) > (float)0.008856)
                                        var_X = powr(var_X, 3);
                                    else
                                        var_X = (var_X - (float)16 / (float)116) / (float)7.787;
                                
                                
                                    
                                    if (powr(var_Z, 3) > (float)0.008856)
                                    var_Z = powr(var_Z, 3);
                                    else
                                        var_Z = (var_Z - (float)16 / (float)116) / (float)7.787;   
                                
                                    
                                    float var_R = var_X * 3.2406 + var_Y * (-1.5372) + var_Z * (-0.4986);
                                    float var_G = var_X * (-0.9689) + var_Y * 1.8758 + var_Z * 0.0415;
                                    float var_B = var_X * 0.0557 + var_Y * (-0.2040) + var_Z * 1.0570;
                                    
                                    
                                    if (var_R > 0.0031308)
                                        var_R = 1.055 * (rootn(var_R, 2.4)) - 0.055;
                                    else
                                        var_R = 12.92 * var_R;
                                    
                                    if (var_G > 0.0031308)
                                        var_G = 1.055 * (rootn(var_G, 2.4)) - 0.055;
                                    else
                                        var_G = 12.92 * var_G;
                                
                                    if (var_B > 0.0031308)
                                        var_B = 1.055 * (rootn(var_B, 2.4)) - 0.055;
                                    else
                                        var_B = 12.92 * var_B;
                                        
                                
                                    int R =  (int)(var_R * 256.0);       
                                    int G = (int)(var_G * 256.0);
                                    int B = (int)(var_B * 256.0);
                                    
                                    if (R < 0) R = 0;
                                    else if (R > 255) R = 255;
                                    
                                    if (G < 0) G = 0;
                                    else if (G > 255) G = 255;
                                    
                                    if (B < 0) B = 0;
                                    else if (B > 255) B = 255;
                                    
                                    dest[pos] = B;
                                    dest[pos + 1] = G;
                                    dest[pos + 2] = R;
                                    dest[pos + 3] = 255;
                                }
                                
                                kernel void PSD_RenderColors_ALAB(global uchar* srAlpha, global uchar* srL, global uchar* srA, global uchar* srB, global uchar* dest)
                                {
                                    int id = get_global_id(0);
                                    int pos = id * 4;
                                    
                                    uchar alpha = srAlpha[id];
                                    uchar l = srL[id];
                                    uchar a = srA[id];
                                    uchar b = srB[id];
                                    
                                    float l_coef = (float)2.56;
                                    float a_coef = (float)1.0;
                                    float b_coef = (float)1.0;
                                    
                                    
                                    int Lab_L = (int)(l / l_coef);
                                    int Lab_A = (int)((a / a_coef) - (float)128.0);
                                    int Lab_B = (int)((b / b_coef) - (float)128.0);
                                    
                                    
                                    float ref_X = (float)95.047;
                                    float ref_Y = (float)100.000;
                                    float ref_Z = (float)108.883;
                                    
                                    float var_Y = ((float)Lab_L + (float)16.0) / (float)116.0;
                                    float var_X = (float)Lab_A / (float)500.0 + var_Y;
                                    float var_Z = var_Y - (float)Lab_B / (float)200.0;
                                
                                    
                                    if(powr(var_Y,3) > (float)0.008856)
                                        var_Y = powr(var_Y , 3);
                                    else
                                        var_Y = (var_Y - (float)16 / (float)116) / (float)7.787;
                                    
                                    
                                        
                                    if (powr(var_X, 3) > (float)0.008856)
                                        var_X = powr(var_X, 3);
                                    else
                                        var_X = (var_X - (float)16 / (float)116) / (float)7.787;
                                
                                
                                    
                                    if (powr(var_Z, 3) > (float)0.008856)
                                    var_Z = powr(var_Z, 3);
                                    else
                                        var_Z = (var_Z - (float)16 / (float)116) / (float)7.787;   
                                
                                    
                                    float var_R = var_X * 3.2406 + var_Y * (-1.5372) + var_Z * (-0.4986);
                                    float var_G = var_X * (-0.9689) + var_Y * 1.8758 + var_Z * 0.0415;
                                    float var_B = var_X * 0.0557 + var_Y * (-0.2040) + var_Z * 1.0570;
                                    
                                    
                                    if (var_R > 0.0031308)
                                        var_R = 1.055 * (rootn(var_R, 2.4)) - 0.055;
                                    else
                                        var_R = 12.92 * var_R;
                                    
                                    if (var_G > 0.0031308)
                                        var_G = 1.055 * (rootn(var_G, 2.4)) - 0.055;
                                    else
                                        var_G = 12.92 * var_G;
                                
                                    if (var_B > 0.0031308)
                                        var_B = 1.055 * (rootn(var_B, 2.4)) - 0.055;
                                    else
                                        var_B = 12.92 * var_B;
                                        
                                
                                    int R =  (int)(var_R * 256.0);       
                                    int G = (int)(var_G * 256.0);
                                    int B = (int)(var_B * 256.0);
                                    
                                    if (R < 0) R = 0;
                                    else if (R > 255) R = 255;
                                    
                                    if (G < 0) G = 0;
                                    else if (G > 255) G = 255;
                                    
                                    if (B < 0) B = 0;
                                    else if (B > 255) B = 255;
                                    
                                    dest[pos] = B;
                                    dest[pos + 1] = G;
                                    dest[pos + 2] = R;
                                    dest[pos + 3] = alpha;
                                }   
                        ";
            }
        }
        
        #endregion

        private static string SourceCodeForBenchmark
        {
            get
            {
                return @"
                            kernel void BenchmarkBytes(global uchar* data)
                            {
                                int id = get_global_id(0);
                                
                                uchar val = 0;
                                
                                if(id % 2 == 0)
                                    val = data[id] * 128 / 255;
                                else
                                    val = data[id] * 127 / 255;
                                    
                                    data[id] = val;
                            }
                        ";
            }
        }

        #endregion
    }
}
