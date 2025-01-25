using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Connection.Packets
{
    public static class Header
    {
        /// <summary>
        /// Windows and Unix differ in their memory models and make it difficult to
        /// support struct timeval in a single library, like this one, across
        /// multiple platforms.
        ///
        /// See http://en.wikipedia.org/wiki/64bit#Specific_data_models
        ///
        /// The issue is that struct timeval { long tv_sec; long tv_usec; }
        /// has different sizes on Linux 32 and 64bit but the same size on
        /// Windows 32 and 64 bit
        ///
        /// Thanks to Jon Pryor for his help in figuring out both the issue with Linux
        /// 32/64bit and the issue between Windows and Unix
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct timeval_unix
        {
            // NOTE: The use of IntPtr here is due to the issue with the timeval structure
            //       The timeval structure contains long values, which differ between 32 bit and
            //       64 bit platforms. One trick, thanks to Jon Pryor for the suggestion, is to
            //       use IntPtr. The size of IntPtr will change depending on the platform the
            //       code runs on, so it should handle the size properly on both 64 bit and 32 bit platforms.
            public IntPtr tv_sec;
            public IntPtr tv_usec;
        };

        /// <summary>
        /// Windows version of struct timeval, the longs are 32bit even on 64-bit versions of Windows
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct timeval_windows
        {
            public Int32 tv_sec;
            public Int32 tv_usec;
        };

        /// <summary>
        /// MacOSX version of struct timeval
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct timeval_macosx
        {
            public IntPtr tv_sec;
            public Int32 tv_usec;
        };
        private static readonly bool isMacOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly bool is32BitTs = IntPtr.Size == 4 || RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static readonly int CaptureLengthOffset = GetTimevalSize();

        private static int GetTimevalSize()
        {
            if (is32BitTs)
            {
                return Marshal.SizeOf<timeval_windows>();
            }
            if (isMacOSX)
            {
                return Marshal.SizeOf<timeval_macosx>();
            }
            return Marshal.SizeOf<timeval_unix>();
        }
    }
}
