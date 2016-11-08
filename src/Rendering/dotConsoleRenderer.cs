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
using dotCmd.DataStructures;
using dotCmd.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Rendering
{
    /// <summary>
    /// Console Renderer, uses low level functions to render
    /// content to buffers.
    /// </summary>
    public class DotConsoleRenderer : IConsoleRenderer
    {
        /// <summary>
        /// Maximum buffer size that the NativeConsoleHost can handle.
        /// </summary>
        private const int maxBufferSize = 16384 / 4; //64K / 4 since the struct is 4 bytes in size
        //Create a single output buffer.
        private Lazy<SafeFileHandle> outputBuffer = new Lazy<SafeFileHandle>(DotConsoleNative.CreateOutputBuffer);

        private ColorMap colorMap = null;

        public DotConsoleRenderer() {

            var buffer = GetOutputBuffer();
            colorMap = new ColorMap(buffer);
        }

        public ConsoleColor BackgroundColor { get; set; }
        public ConsoleColor ForegroundColor { get; set; }

        /// <summary>
        /// Get the color map that controls how colors are maped in the console host.
        /// </summary>
        public ColorMap ColorMap
        {
            get { return colorMap; }
        }

        /// <summary>
        /// Gets the output buffer window size.
        /// </summary>
        /// <returns></returns>
        public Coordinates GetOutputBufferWindowSize()
        {
            var buffer = GetOutputBuffer();
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO info = DotConsoleNative.GetConsoleScreenBufferInfo(buffer);

            return new Coordinates()
            {
                X = info.size.X,
                Y = info.size.Y
            };
        }

        /// <summary>
        /// Gets the output buffer windows as a rectangle.
        /// </summary>
        /// <returns></returns>
        public Region GetOutputBufferWindow()
        {
            var buffer = GetOutputBuffer();
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO info = DotConsoleNative.GetConsoleScreenBufferInfo(buffer);

            return new Region() { Left = info.window.Left, Top = info.window.Top, Height = info.window.Bottom, Width = info.window.Right };
        }

        /// <summary>
        /// Sets the cursor position.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetCursorPosition(Coordinates orgin)
        {
            var handle = GetOutputBuffer();

            DotConsoleNative.SetConsoleCursorPosition(handle, new ConsoleHostNativeMethods.COORD()
            {
                X = (short)(orgin.X),
                Y = (short)(orgin.Y)
            });
        }

        /// <summary>
        /// Writes lines of text into the output buffer at a specified coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        /// <param name="content"></param>
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
                    buffer[lineId, charId].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(ForegroundColor, BackgroundColor);
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

            //partition by Y coordinate.
            int partitionY = (int)Math.Ceiling((decimal)(maxBufferSize / sizeOfX));

            var sizeOfY = cellBuffer.GetLength(0);

            //if Y is smaller then the partition by Y then we need 
            //to set the partiton size to Y size.
            if (sizeOfY < partitionY)
            {
                partitionY = sizeOfY;
            }

            //Get partitoned buffer size.
            int charBufferSize = (int)(partitionY * cellBuffer.GetLength(1));

            int cursor = 0;
            int i = 0;
            do
            {
                i += partitionY;

                //If we exceeded the maximum size of the buffer we need to substract to the size of the remaining buffer.
                if (i > sizeOfY)
                {
                    int diff = i - sizeOfY;
                    i = i - diff;
                }

                //Fill the buffer part.
                ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[charBufferSize];
                int idx = 0;
                for (int y = cursor; y < i; y++)
                {
                    for (int x = 0; x < cellBuffer.GetLength(1); x++)
                    {
                        var cellToWrite = cellBuffer[y, x];

                        if (cellToWrite.Attributes == 0)
                        {
                            //Don't set the foreground color for empty cells this will force the ConsoleHost
                            //to do a very expensive calculation for each cell and slow things down.
                            cellToWrite.Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(0, BackgroundColor);
                        }

                        buffer[idx].Attributes = cellToWrite.Attributes;
                        buffer[idx].UnicodeChar = cellToWrite.Char;
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

                cursor = i;
            }
            while (i < sizeOfY);
        }

        /// <summary>
        /// Reads cell matrix form the output buffer using the provided rectangle.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public OutputCell[,] ReadOutput(Region region)
        {
            var handle = GetOutputBuffer();

            //Get len of X coordinate, the plan here is to partition by Y
            var sizeOfX = (region.Width - region.Left + 1) * 4;

            //partition by Y coordinate.
            var partitionY = (int)Math.Ceiling((decimal)(maxBufferSize / sizeOfX));

            int sizeOfY = (region.Height - region.Top + 1);

            //if Y is smaller then the partition by Y then we need 
            //to set the partiton size to Y size.
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
            int i = 0;
            do
            {
                i += partitionY;

                //the size of the Y coordinate is always the size of the buffer.
                bufferSize.X = (short)(region.Width - region.Left + 1);
                bufferSize.Y = (short)partitionY;

                //If we exceeded the maximum size of the buffer we need to substract to the size of the remaining buffer.
                if (i > sizeOfY)
                {
                    int diff = i - sizeOfY;
                    bufferSize.Y = (short)(partitionY - diff);
                }

                //Fill the buffer part.
                ConsoleHostNativeMethods.SMALL_RECT readRegion = new ConsoleHostNativeMethods.SMALL_RECT();

                readRegion.Left = (short)region.Left;
                readRegion.Top = (short)region.Top;
                readRegion.Right = (short)(region.Left + bufferSize.X - 1);
                readRegion.Bottom = (short)(region.Top + bufferSize.Y - 1);

                ConsoleHostNativeMethods.CHAR_INFO[] buffer = new ConsoleHostNativeMethods.CHAR_INFO[bufferSize.X * bufferSize.Y];
                buffer = DotConsoleNative.ReadConsoleOutput(handle, buffer, bufferSize, bufferCoord, ref readRegion);

                //Fill the output buffer cells.
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
            while (i < sizeOfY);

            return cells;
        }

        /// <summary>
        /// Creates the output buffer.
        /// </summary>
        /// <returns></returns>
        private SafeFileHandle GetOutputBuffer()
        {
            return outputBuffer.Value;
        }


    }
}
