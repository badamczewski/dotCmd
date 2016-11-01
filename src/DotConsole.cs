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
        private const int maxBufferSize = 64000 / 4; //the struct is 4 bytes in size

        public List<ContentRegion> regions = new List<ContentRegion>();

        public void Start()
        {
            //Set main thread name.
            System.Threading.Thread.CurrentThread.Name = ".Console host main thread";
            Console.OutputEncoding = System.Text.Encoding.Unicode;
        }

        private Lazy<SafeFileHandle> outputBuffer = new Lazy<SafeFileHandle>(DotConsoleNative.CreateOutputBuffer);

        private SafeFileHandle GetOutputBuffer()
        {
            return outputBuffer.Value;
        }

        internal Coordinates GetOutputBufferWindowSize()
        {
            var buffer = GetOutputBuffer();
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO info = DotConsoleNative.GetConsoleScreenBufferInfo(buffer);

            return new Coordinates()
            {
                X = info.size.X,
                Y = info.size.Y
            };
        }

        internal Region GetOutputBufferWindow()
        {
            var buffer = GetOutputBuffer();
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO info = DotConsoleNative.GetConsoleScreenBufferInfo(buffer);
            
            return new Region() { Left = info.window.Left, Top = info.window.Top, Height = info.window.Bottom, Width = info.window.Right };
        }

        public void AddContentRegion(ContentRegion region)
        {
            this.regions.Add(region);
        }

        public void WriteLine(string text)
        {
            foreach (var region in regions)
                region.Hide();

            Console.Out.WriteLine(text);

            foreach (var region in regions)
                region.Show();
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

            //Get len of X coordinate, the plan here is to partition by Y
            var sizeOfX = cellBuffer.GetLength(1) * 4;

            int partitionY = (int)Math.Ceiling((decimal)(maxBufferSize / sizeOfX));

            var sizeOfY = cellBuffer.GetLength(0);

            if(sizeOfY < partitionY)
            {
                partitionY = sizeOfY;
            }

            int charBufferSize = (int)(partitionY * cellBuffer.GetLength(1));

            int cursor = 0;

            for (int i = partitionY; i <= sizeOfY; i += partitionY)
            {
                ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[charBufferSize];
                int idx = 0;
                for (int y = cursor; y < i; y++)
                {
                    for (int x = 0; x < cellBuffer.GetLength(1); x++)
                    {
                        buffer[idx].Attributes = cellBuffer[y, x].Attributes;
                        buffer[idx].UnicodeChar = cellBuffer[y, x].Char;
                        idx++;
                    }
                }

                ConsoleHostNativeMethods.COORD bufferSize = new ConsoleHostNativeMethods.COORD();
                bufferSize.X = (short)cellBuffer.GetLength(1);
                bufferSize.Y = (short)partitionY;

                ConsoleHostNativeMethods.COORD bufferCoord = new ConsoleHostNativeMethods.COORD();
                bufferCoord.X = 0;
                bufferCoord.Y = 0;

                ConsoleHostNativeMethods.SMALL_RECT writeRegion = new ConsoleHostNativeMethods.SMALL_RECT();
                writeRegion.Left = (short)orgin.X;
                writeRegion.Top = (short)(orgin.Y + cursor);
                writeRegion.Right = (short)(orgin.X + bufferSize.X - 1);
                writeRegion.Bottom = (short)(orgin.Y + cursor + bufferSize.Y - 1);

                DotConsoleNative.WriteConsoleOutput(handle, buffer, bufferSize, bufferCoord, ref writeRegion);

                cursor += i;
            }
        }

        public OutputCell[,] ReadOutput(Region region)
        {
            var handle = GetOutputBuffer();

            //Get len of X coordinate, the plan here is to partition by Y
            var sizeOfX = (region.Width - region.Left + 1) * 4;

            var partitionY = Math.Ceiling((decimal)(maxBufferSize / sizeOfX));

            int sizeOfY = (region.Height - region.Top + 1);

            if (sizeOfY < partitionY)
            {
                partitionY = sizeOfY;
            }

            ConsoleHostNativeMethods.COORD bufferCoord = new ConsoleHostNativeMethods.COORD();
            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            ConsoleHostNativeMethods.COORD bufferSize = new ConsoleHostNativeMethods.COORD();
            bufferSize.X = (short)(region.Width - region.Left + 1);
            
            OutputCell[,] cells = new OutputCell[sizeOfY, bufferSize.X];

            int cursor = 0;
            for (int i = (int)partitionY; i <= sizeOfY; i += (int)partitionY)
            {
                bufferSize.X = (short)(region.Width - region.Left + 1);
                bufferSize.Y = (short)partitionY;

                var sub = i - sizeOfY;
                if (sub >= 0)
                {
                    bufferSize.Y = (short)(partitionY - sub);
                }

                ConsoleHostNativeMethods.SMALL_RECT readRegion = new ConsoleHostNativeMethods.SMALL_RECT();

                readRegion.Left = (short)region.Left;
                readRegion.Top = (short)bufferCoord.Y;
                readRegion.Right = (short)(region.Left + bufferSize.X - 1);
                readRegion.Bottom = (short)(bufferCoord.Y + bufferSize.Y - 1);

                ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[bufferSize.X * bufferSize.Y];
                buffer = DotConsoleNative.ReadConsoleOutput(handle, buffer, bufferSize, bufferCoord, ref readRegion);

                int idx = 0;
                for (int k = cursor; k < bufferSize.Y; k++)
                {
                    for (int n = 0; n < bufferSize.X; n++)
                    {
                        cells[k, n].Attributes = buffer[idx].Attributes;
                        cells[k, n].Char = buffer[idx].UnicodeChar;
                        idx++;
                    }
                    cursor = k;
                }
            }

            return cells;
        }
    }
}
