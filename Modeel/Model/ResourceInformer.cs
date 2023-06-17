using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public static class ResourceInformer
    {
        private const int _kilobyte = 1024;
        private const int _megabyte = _kilobyte * 1024;

        public static int CalculateBufferSize(long fileSize)
        {
            // Determine the available system memory
            long availableMemory = GC.GetTotalMemory(false);

            // Choose a buffer size based on the file size and available memory
            if (fileSize <= availableMemory)
            {
                // If the file size is smaller than available memory, use a buffer size equal to the file size
                return (int)fileSize;
            }
            else
            {
                // Otherwise, choose a buffer size that is a fraction of available memory
                double bufferFraction = 0.1;
                int bufferSize = (int)(availableMemory * bufferFraction);

                // Ensure the buffer size is at least 4KB and at most 1MB
                return Math.Max(4096, Math.Min(bufferSize, 1048576));
            }
        }

        public static string FormatDataTransferRate(long bytesSent)
        {

            string unit;
            double transferRate;

            if (bytesSent < _kilobyte)
            {
                transferRate = bytesSent;
                unit = "B/s";
            }
            else if (bytesSent < _megabyte)
            {
                transferRate = (double)bytesSent / _kilobyte;
                unit = "KB/s";
            }
            else
            {
                transferRate = (double)bytesSent / _megabyte;
                unit = "MB/s";
            }

            return $"{transferRate:F2} {unit}";
        }
    }
}
