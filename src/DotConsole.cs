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

namespace dotCmd
{
    public class DotConsole
    {
        public void Start()
        {
            //Set main thread name.
            System.Threading.Thread.CurrentThread.Name = ".Console host main thread";
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            GetOutputBuffer();
        }

        private Lazy<SafeFileHandle> outputBuffer = new Lazy<SafeFileHandle>(DotConsoleNative.CreateOutputBuffer);

        private SafeFileHandle GetOutputBuffer()
        {
            return outputBuffer.Value;
        }

        public void WriteLine(string text)
        {
            Console.Out.WriteLine(text);
        }

        public void WriteOutput(Coordinates orgin, string[] content)
        {
            int lineId = 0;
            int charId = 0;

            //Find the widest line and set the OutputCell matrix to such width
            var max = content.Max(x => x.Length);

            OutputCell[,] buffer = new OutputCell[content.Length, max];

            foreach (var line in content)
            {
                charId = 0;
                foreach (var c in line)
                {
                    buffer[lineId, charId].Char = (ushort)c;
                    buffer[lineId, charId].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(Console.ForegroundColor, Console.BackgroundColor);
                    charId++;
                }
            }

            WriteOutput(orgin, buffer);
        }

        public void WriteOutput(Coordinates orgin, OutputCell[,] cellBuffer)
        {
            var handle = GetOutputBuffer();

            ConsoleHostNativeMethods.COORD bufferSize = new ConsoleHostNativeMethods.COORD();
            bufferSize.X = (short)cellBuffer.GetLength(1);
            bufferSize.Y = (short)cellBuffer.GetLength(0);

            ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[cellBuffer.Length];

            int idx = 0;
            for (int i = 0; i < bufferSize.Y; i++)
            {
                for (int k = 0; k < bufferSize.X; k++)
                {
                    buffer[idx].Attributes = cellBuffer[i, k].Attributes;
                    buffer[idx].UnicodeChar = cellBuffer[i, k].Char;
                    idx++;
                }
            }

            ConsoleHostNativeMethods.COORD bufferCoord = new ConsoleHostNativeMethods.COORD();
            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            ConsoleHostNativeMethods.SMALL_RECT writeRegion = new ConsoleHostNativeMethods.SMALL_RECT();
            writeRegion.Left = (short)orgin.X;
            writeRegion.Top = (short)orgin.Y;
            writeRegion.Right = (short)(orgin.X + bufferSize.X - 1);
            writeRegion.Bottom = (short)(orgin.Y + bufferSize.Y - 1);

            DotConsoleNative.WriteConsoleOutput(handle, buffer, bufferSize, bufferCoord, ref writeRegion);
        }


        public OutputCell[,] ReadOutput(Region region)
        {
            var handle = GetOutputBuffer();

            ConsoleHostNativeMethods.COORD bufferSize = new ConsoleHostNativeMethods.COORD();

            bufferSize.X = (short)(region.Width - region.Left + 1);
            bufferSize.Y = (short)(region.Height - region.Top + 1);

            ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[bufferSize.X * bufferSize.Y];

            ConsoleHostNativeMethods.COORD bufferCoord = new ConsoleHostNativeMethods.COORD();
            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            ConsoleHostNativeMethods.SMALL_RECT readRegion = new ConsoleHostNativeMethods.SMALL_RECT();
            readRegion.Left = (short)region.Left;
            readRegion.Top = (short)region.Top;
            readRegion.Right = (short)(region.Left + bufferSize.X - 1);
            readRegion.Bottom = (short)(region.Top + bufferSize.Y - 1);

            buffer = DotConsoleNative.ReadConsoleOutput(handle, buffer, bufferSize, bufferCoord, ref readRegion);

            OutputCell[,] cells = new OutputCell[bufferSize.Y, bufferSize.X];

            int idx = 0;
            for (int i = 0; i < bufferSize.Y; i++)
            {
                for (int k = 0; k < bufferSize.X; k++)
                {
                    cells[i, k].Attributes = buffer[idx].Attributes;
                    cells[i, k].Char = buffer[idx].UnicodeChar;
                    idx++;
                }
            }

            return cells;
        }
    }
}
