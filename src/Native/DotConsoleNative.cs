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
