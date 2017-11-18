using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;

namespace Muho.PSD.Accelerators
{
    public partial class OpenCLAccelerator : IDisposable
    {
        #region Private Variables
        private ComputeDevice device;
        private ComputeContextPropertyList computePropList;
        private ComputeContext context;
        private ComputeProgram program;
        private List<ComputeKernel> kernels;
        private ComputeCommandQueue queue;
        private ComputeEventList events;
        #endregion
        #region Construct
        public OpenCLAccelerator(IntPtr deviceHandle)
        {
            device = ComputePlatform.Platforms.SelectMany(sm => sm.Devices).Where(s => s.Handle.Value == deviceHandle).FirstOrDefault();
            if (device == null)
                throw new Exception("Device Not Found");

            computePropList = new ComputeContextPropertyList(device.Platform);

            var devices = new List<ComputeDevice>() { device };
            context = new ComputeContext(devices, computePropList, null, IntPtr.Zero);

            program = new ComputeProgram(context, SourceCode);
            program.Build(devices, string.Empty, null, IntPtr.Zero);

            kernels = program.CreateAllKernels().ToList();

            queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);

            events = new ComputeEventList();

        }
        #endregion
        #region Properties
        public string AcceleratorDevice
        {
            get
            {
                return string.Format("{0} ({1})", device.Name, device.Type);
            }
        }

        internal ComputeDevice CurrentDevice { get; private set; }
        internal List<ComputeKernel> Kernels
        {
            get
            {
                return kernels;
            }
        }
        internal ComputeContext Context
        {
            get
            {
                return context;
            }
        }
        #endregion
        #region Generic Methods
        public IntPtr GetKernel(string methodName)
        {
            var q = Kernels.FirstOrDefault(f => f.FunctionName.Equals(methodName));
            if (q == null)
                return IntPtr.Zero;

            return q.Handle.Value;
        }
        public byte[] Execute(IntPtr kernelHandle,
                                byte[][] arguments,
                                long[] workSizeArray,
                                int resultSize)
        {

            var kernel = Kernels.FirstOrDefault(f => f.Handle.Value == kernelHandle);
            if (kernel == null)
                throw new Exception("Kernel Not Found");


            var memList = new List<ComputeMemory>();
            int argCounter = 0;
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    var memArg = new ComputeBuffer<byte>(context,
                                                 ComputeMemoryFlags.ReadWrite
                                                 |
                                                 ComputeMemoryFlags.UseHostPointer,
                                                 arg);
                    memList.Add(memArg);
                    kernel.SetMemoryArgument(argCounter, memArg);
                    argCounter++;
                }
            }

            var result = new byte[resultSize];
            var resultBuffer = new ComputeBuffer<byte>(context,
                                                 ComputeMemoryFlags.ReadWrite
                                                 |
                                                 ComputeMemoryFlags.UseHostPointer,
                                                 result);
            kernel.SetMemoryArgument(argCounter, resultBuffer);

            queue.Execute(kernel, new long[] { 0 }, workSizeArray, null, null);

            queue.ReadFromBuffer(resultBuffer, ref result, true, events);



            queue.Finish();
            queue.Flush();

            return result;
        }
        public byte[] Execute(IntPtr kernelHandle,
                                byte[][] arguments,
                                long workSize,
                                int resultSize)
        {
            return Execute(kernelHandle, arguments, new long[] { workSize }, resultSize);
        }

        public byte[] UnpackAllkBytes(byte[] compressedBytes, int resultSize)
        {
            var result = new byte[resultSize];
            var kernel = Kernels.FirstOrDefault(f => f.FunctionName.Equals("UnpackAllData"));

            var sourceArg = new ComputeBuffer<byte>(context,
                                                 ComputeMemoryFlags.ReadWrite
                                                 |
                                                 ComputeMemoryFlags.UseHostPointer,
                                                 compressedBytes);
            kernel.SetMemoryArgument(0, sourceArg);


            var destArg = new ComputeBuffer<byte>(context,
                                                 ComputeMemoryFlags.ReadWrite
                                                 |
                                                 ComputeMemoryFlags.UseHostPointer,
                                                 result);
            kernel.SetMemoryArgument(1, destArg);


            kernel.SetValueArgument(2, compressedBytes.Length);

            queue.Execute(kernel, new long[] { 0 }, new long[] { 1 }, null, null);

            queue.ReadFromBuffer(destArg, ref result, true, events);



            queue.Finish();
            queue.Flush();

            return result;
        }

        #endregion
        #region Implements
        public void Dispose()
        {
            events = null;

            queue.Dispose();
            queue = null;

            foreach (var kernel in kernels)
                kernel.Dispose();

            kernels = null;

            program.Dispose();
            program = null;

            context.Dispose();
            context = null;
            computePropList = null;

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
