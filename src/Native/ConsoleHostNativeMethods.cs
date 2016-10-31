#region Licence
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
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
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
        internal struct CHAR_INFO
        {
            internal ushort UnicodeChar;
            internal UInt16 Attributes;
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
