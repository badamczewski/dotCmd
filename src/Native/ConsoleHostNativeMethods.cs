﻿#region Licence
/*
Copyright (c) 2011-2014 Contributors as noted in the AUTHORS file
This file is part of dotCmd.

dotCmd is free software; you can redistribute it and/or modify it under
the terms of the GNU Lesser General Public License as published by
the Free Software Foundation; either version 3 of the License, or
(at your option) any later version.

dotCmd is distributed WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
You should have received a copy of the GNU Lesser General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
#endregion
using dotCmd.DataStructures;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Native
{
    internal static class ConsoleHostNativeMethods
    {
        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetConsoleCtrlHandler(
            BreakHandler handlerRoutine, 
            bool add
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool FillConsoleOutputCharacter
        (
            IntPtr consoleOutput,
            Char character,
            uint length,
            COORD writeCoord,
            out uint numberOfCharsWritten
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool FillConsoleOutputAttribute(
            IntPtr consoleOutput,
            ushort attribute,
            uint length,
            COORD writeCoord,
            out uint numberOfAttrsWritten
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetConsoleScreenBufferInfo(
            IntPtr consoleOutput,
            out CONSOLE_SCREEN_BUFFER_INFO consoleScreenBufferInfo);

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetConsoleScreenBufferInfoEx(
            IntPtr consoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX ConsoleScreenBufferInfo);

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetConsoleScreenBufferInfoEx(
            IntPtr consoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX consoleScreenBufferInfo
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ReadConsoleOutput
        (
            IntPtr consoleOutput,
            [Out] CHAR_INFO[] buffer,
            COORD bufferSize,
            COORD bufferCoord,
            ref SMALL_RECT readRegion
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool WriteConsoleOutput
        (
            IntPtr consoleOutput,
            CHAR_INFO[] buffer,
            COORD bufferSize,
            COORD bufferCoord,
            ref SMALL_RECT writeRegion
        );


        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ReadConsole
        (
            IntPtr consoleInput,
            StringBuilder buffer,
            uint numberOfCharsToRead,
            out uint numberOfCharsRead,
            ref CONSOLE_READCONSOLE_CONTROL controlData
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ReadConsoleInput
        (
            IntPtr consoleInput,
            [Out] INPUT_RECORD[] buffer,
            uint length,
            out uint numberOfEventsRead
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetConsoleMode
        (
            IntPtr consoleHandle, 
            out UInt32 mode
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetConsoleMode
        (
            IntPtr consoleHandle,
            UInt32 mode
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetConsoleCursorPosition
        (
            IntPtr consoleOutput,
            COORD cursorPosition
        );

        [DllImport(DllImportNames.Kernel, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetConsoleCursorInfo
        (
            IntPtr consoleOutput, 
            out CONSOLE_CURSOR_INFO consoleCursorInfo);

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT_RECORD
        {
            public UInt16 EventType;
            public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_CURSOR_INFO
        {
            public uint size;
            public bool visible;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEY_EVENT_RECORD
        {
            public bool keyDown;
            public UInt16 repeatCount;
            public UInt16 virtualKeyCode;
            public UInt16 virtualScanCode;
            public char unicodeChar;
            public UInt16 controlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO
        {

            public COORD size;
            public COORD cursorPosition;
            public UInt16 attributes;
            public SMALL_RECT window;
            public COORD maximumWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public int cbSize;
            public COORD size;
            public COORD cursorPosition;
            public ushort attributes;
            public SMALL_RECT window;
            public COORD maximumWindowSize;
            public ushort popupAttributes;
            public bool fullscreenSupported;

            public COLORREF black;            
            public COLORREF darkBlue;
            public COLORREF darkGreen;
            public COLORREF darkCyan;
            public COLORREF darkRed;
            public COLORREF darkMagenta;
            public COLORREF darkYellow;
            public COLORREF gray;
            public COLORREF darkGray;
            public COLORREF blue;
            public COLORREF green;
            public COLORREF cyan;
            public COLORREF red;
            public COLORREF magenta;
            public COLORREF yellow;
            public COLORREF white;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_READCONSOLE_CONTROL
        {
            public uint length;
            public uint initialChars;
            public uint ctrlWakeupMask;
            public uint controlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COLORREF
        {
            public uint Color;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        {
            public ushort UnicodeChar;
            public UInt16 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COORD
        {
            public short X;
            public short Y;
        }

        [Flags]
        internal enum DesiredAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000
        }

        [Flags]
        internal enum ShareMode : uint
        {
            ShareRead = 0x00000001,
            ShareWrite = 0x00000002
        }

        internal enum CreationDisposition : uint
        {
            CreateNew = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }
    }
}
