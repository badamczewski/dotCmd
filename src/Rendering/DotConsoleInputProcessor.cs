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
using dotCmd.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Rendering
{
    public class DotConsoleInputProcessor
    {
        /// <summary>
        /// Maximum buffer size that the NativeConsoleHost can handle.
        /// </summary>
        private const int maxBufferSize = 16384 / 4; //64K / 4 since the struct is 4 bytes in size

        //Create a single input buffer.
        private Lazy<SafeFileHandle> inputBuffer = new Lazy<SafeFileHandle>(DotConsoleNative.CreateInputBuffer);

        /// <summary>
        /// Reads the input from the input buffer.
        /// </summary>
        /// <param name="initialContent"></param>
        /// <param name="controlChar">ASCII control character.</param>
        /// <returns></returns>
        public string ReadInput(string initialContent, char controlChar)
        {
            var handle = GetInputBuffer();

            string text = DotConsoleNative.ReadConsole(handle, initialContent, maxBufferSize, controlChar);

            return text;
        }

        public void RegisterBreakHandler(BreakHandler handler)
        {
            ConsoleHostNativeMethods.SetConsoleCtrlHandler(handler, true);
        }

        /// <summary>
        /// Creates the input buffer.
        /// </summary>
        /// <returns></returns>
        private SafeFileHandle GetInputBuffer()
        {
            return inputBuffer.Value;
        }
    }
}
