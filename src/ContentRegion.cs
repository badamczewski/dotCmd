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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd
{
    public class ContentRegion
    {
        private OutputCell[,] savedContentBuffer = null;
        private OutputCell[,] contentBuffer = null;
        private Coordinates savedCoordsWithOffset;
        private DotConsole console;

        private Coordinates bufferSize;
        private Coordinates currentBufferSize;
        private Coordinates orgin;
        private object locker = new object();

        public ContentRegion(DotConsole console, Coordinates bufferSize, Coordinates orgin)
        {
            this.console = console;
            this.bufferSize = bufferSize;
            this.orgin = orgin;

            this.contentBuffer = new OutputCell[bufferSize.Y, bufferSize.X];
        }

        private void FillBuffer(string text, int row)
        {
            int idx = 0;
            foreach (var c in text)
            {
                if (idx >= bufferSize.X)
                    break;

                this.contentBuffer[row, idx].Char = (ushort)c;
                this.contentBuffer[row, idx].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(Console.ForegroundColor, ConsoleColor.Blue);

                idx++;
            }
        }

        public void WriteLine(string text)
        {
            var window = console.GetOutputBufferWindow();

            if (bufferSize.Y > currentBufferSize.Y)
            {
                FillBuffer(text,  currentBufferSize.Y);
                currentBufferSize.Y++;
            }
            else
            {
                //Right shift the buffer.
                var @new = new OutputCell[bufferSize.Y, bufferSize.X];
                Array.Copy(contentBuffer, bufferSize.X, @new, 0, this.contentBuffer.Length - bufferSize.X);

                this.contentBuffer = @new;
                FillBuffer(text,  currentBufferSize.Y - 1);
            }

            Show();
        }

        public void Show()
        {
            var window = console.GetOutputBufferWindow();
            Show(window);
        }

        public void Show(Region window)
        {
            int height = orgin.Y + window.Top + (contentBuffer.GetLength(0) - 1);
            int width = orgin.X + window.Left + (contentBuffer.GetLength(1) - 1);

            int top = orgin.Y + window.Top;
            int left = orgin.X + window.Left;

            savedContentBuffer = console.ReadOutput(new Region() { Left = left, Top = top, Height = height, Width = width });
            savedCoordsWithOffset = new Coordinates() { X = left, Y = top };

            console.WriteOutput(savedCoordsWithOffset, this.contentBuffer);
        }

        public void Hide()
        {
            if (savedContentBuffer != null)
                console.WriteOutput(savedCoordsWithOffset, savedContentBuffer);
        }
    }
}
