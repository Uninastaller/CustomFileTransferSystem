using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel
{
    public static class ResourceInformer
    {
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
    }
}
