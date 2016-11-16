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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Native
{
    /// <summary>
    /// Wraps Native Console calls to provide additional facilities like logging
    /// and error handling.
    /// </summary>
    internal static class DotConsoleNative
    {
        internal static SafeFileHandle CreateOutputBuffer()
        {
            //We dont use GetStdHandle since it might return a redirected handle, so use the bare bones function.

            var handle = ConsoleHostNativeMethods.CreateFile(
                           "CONOUT$",
                           (UInt32)(ConsoleHostNativeMethods.DesiredAccess.GenericRead | ConsoleHostNativeMethods.DesiredAccess.GenericWrite),
                           (UInt32)ConsoleHostNativeMethods.ShareMode.ShareWrite,
                           (IntPtr)0,
                           (UInt32)ConsoleHostNativeMethods.CreationDisposition.OpenExisting,
                           0,
                           (IntPtr)0);

            if (handle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot get the output buffer", err);
            }

            return handle;
        }

        internal static SafeFileHandle CreateInputBuffer()
        {
            //We dont use GetStdHandle since it might return a redirected handle, so use the bare bones function.

            var handle = ConsoleHostNativeMethods.CreateFile(
                           "CONIN$",
                           (UInt32)(ConsoleHostNativeMethods.DesiredAccess.GenericRead | ConsoleHostNativeMethods.DesiredAccess.GenericWrite),
                           (UInt32)ConsoleHostNativeMethods.ShareMode.ShareWrite,
                           (IntPtr)0,
                           (UInt32)ConsoleHostNativeMethods.CreationDisposition.OpenExisting,
                           0,
                           (IntPtr)0);

            if (handle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot get the input buffer", err);
            }

            return handle;
        }

        internal static string ReadConsole(SafeFileHandle handle, string initialContent, int charsToRead, int? controlCharacter)
        {
            dotCmd.Native.ConsoleHostNativeMethods.CONSOLE_READCONSOLE_CONTROL readControl = new dotCmd.Native.ConsoleHostNativeMethods.CONSOLE_READCONSOLE_CONTROL();

            readControl.length = (uint)Marshal.SizeOf(readControl);

            if (initialContent != null)
                readControl.initialChars = (uint)initialContent.Length;

            readControl.controlKeyState = 0;

            //Magic VOODO starts here.
            //I've found almost no documentation how to set this mask to a given key
            //from what I know it only supports control characters \n \b \t etc.
            if (controlCharacter.HasValue)
            {
                readControl.ctrlWakeupMask = (uint)(1 << controlCharacter.Value);
            }

            StringBuilder buffer = new StringBuilder(initialContent, charsToRead);
            uint charsRead = 0;

            bool result = ConsoleHostNativeMethods.ReadConsole(handle.DangerousGetHandle(), buffer, (uint)charsToRead, out charsRead, ref readControl);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot read from the input buffer", err);
            }

            return buffer.ToString(0, (int)charsRead);
        }

        internal static dotCmd.Native.ConsoleHostNativeMethods.CHAR_INFO[] WriteConsoleOutput(
            SafeFileHandle handle,
            dotCmd.Native.ConsoleHostNativeMethods.CHAR_INFO[] buffer,
            dotCmd.Native.ConsoleHostNativeMethods.COORD bufferSize,
            dotCmd.Native.ConsoleHostNativeMethods.COORD bufferCoord,
            ref dotCmd.Native.ConsoleHostNativeMethods.SMALL_RECT writeRegion)
        {
            bool result = ConsoleHostNativeMethods.WriteConsoleOutput(handle.DangerousGetHandle(), buffer, bufferSize, bufferCoord, ref writeRegion);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot write to the output buffer", err);
            }

            return buffer;
        }

        internal static dotCmd.Native.ConsoleHostNativeMethods.CHAR_INFO[] ReadConsoleOutput(
            SafeFileHandle handle,
            dotCmd.Native.ConsoleHostNativeMethods.CHAR_INFO[] buffer,
            dotCmd.Native.ConsoleHostNativeMethods.COORD bufferSize,
            dotCmd.Native.ConsoleHostNativeMethods.COORD bufferCoord,
            ref dotCmd.Native.ConsoleHostNativeMethods.SMALL_RECT readRegion)
        {
            bool result = ConsoleHostNativeMethods.ReadConsoleOutput(handle.DangerousGetHandle(), buffer, bufferSize, bufferCoord, ref readRegion);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot read from the output buffer", err);
            }

            return buffer;
        }

        internal static dotCmd.Native.ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO GetConsoleScreenBufferInfo(
            SafeFileHandle handle)
        {
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO info = new ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO();
            bool result = ConsoleHostNativeMethods.GetConsoleScreenBufferInfo(handle.DangerousGetHandle(), out info);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot get output buffer info", err);
            }            

            return info;
        }

        internal static dotCmd.Native.ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX GetConsoleScreenBufferInfoExtended(
            SafeFileHandle handle)
        {
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX info = new ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX();
            var size = Marshal.SizeOf(info);

            info.cbSize = size;
            bool result = ConsoleHostNativeMethods.GetConsoleScreenBufferInfoEx(handle.DangerousGetHandle(), ref info);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot get output buffer info", err);
            }

            return info;
        }

        internal static dotCmd.Native.ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX SetConsoleScreenBufferInfoExtended(
            SafeFileHandle handle,
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX info)
        {
            bool result = ConsoleHostNativeMethods.SetConsoleScreenBufferInfoEx(handle.DangerousGetHandle(), ref info); 

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot set output buffer info", err);
            }

            return info;
        }

        internal static void SetConsoleCursorPosition(SafeFileHandle handle, dotCmd.Native.ConsoleHostNativeMethods.COORD cursorPosition)
        {
            bool result = ConsoleHostNativeMethods.SetConsoleCursorPosition(handle.DangerousGetHandle(), cursorPosition);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                throw CreateException("Cannot set the curor position", err);
            }            
        }

        public static uint ToNativeConsoleColor(ConsoleColor foreground, ConsoleColor background)
        {
            //Console coloros are controles using four bits so a combination of backgroud and foregroud color
            //fits into a single byte.

            uint result = (uint)(((int)background << 4) | (int)foreground);

            return result;
        }

        private static Exception CreateException(string friendlyMessage, int err)
        {
            var win32Ex = new Win32Exception(err);
            return new Exception(friendlyMessage, win32Ex);
        }
    }
}
