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
using System.Threading;
using System.Threading.Tasks;

namespace dotCmd
{
    /// <summary>
    /// A Content region that will render contents indivdually from the main console buffer.
    /// </summary>
    public class ContentRegion
    {
        public enum ContentPosition
        {
            Top,
            Bottom
        }

        private OutputCell[,] savedContentBuffer = null;
        private OutputCell[,] contentBuffer = null;
        private Coordinates savedCoordsWithOffset;

        private DotConsole console;
        private ContentPosition position;
        private ContentRegion theOneToRuleThemAll;

        private bool scroll = false;

        public ConsoleColor BackgroundColor { get; set; }
        public ConsoleColor ForegroundColor { get; set; }

        //Structs are hepas of fun but they make the worst properties so we just expose them as fields.
        public Coordinates BufferSize; 
        public Coordinates CurrentBufferSize; 
        public Coordinates Orgin;
       
        //This constructor is used by Dotconsole to register the main buffer within it.
        internal ContentRegion(DotConsole console, Coordinates bufferSize) 
        {
            this.console = console;
            this.BufferSize = bufferSize;
            this.Orgin = new Coordinates(0, 0);
            this.position = ContentPosition.Bottom;

            this.contentBuffer = new OutputCell[bufferSize.Y, bufferSize.X];

            this.scroll = true;

            this.BackgroundColor = ConsoleColor.Black;
            this.ForegroundColor = ConsoleColor.White;
        }

        public ContentRegion(DotConsole console, 
            Coordinates bufferSize, Coordinates orgin, ContentPosition position, bool scroll = false, 
            ConsoleColor backgroundColor = ConsoleColor.Black,
            ConsoleColor foregroundColor = ConsoleColor.White)
        {
            this.console = console;
            this.BufferSize = bufferSize;
            this.Orgin = orgin;
            this.position = position;

            this.contentBuffer = new OutputCell[bufferSize.Y, bufferSize.X];

            this.scroll = scroll;


            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;

            this.console.RegisterRegion(this);
        }

        public void RegisterOwner(ContentRegion theOneToRuleThemAll)
        {
            this.theOneToRuleThemAll = theOneToRuleThemAll;
        }

        private void FillBuffer(string text, int row)
        {
            int idx = 0;
            foreach (var c in text)
            {
                if (idx >= BufferSize.X)
                    break;

                this.contentBuffer[row, idx].Char = (ushort)c;
                this.contentBuffer[row, idx].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(this.ForegroundColor, this.BackgroundColor);

                idx++;
            }
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            var window = console.GetOutputBufferWindow();

            if (BufferSize.Y > CurrentBufferSize.Y)
            {
                FillBuffer(text,  CurrentBufferSize.Y);
                CurrentBufferSize.Y++;
                CurrentBufferSize.X = text.Length;
            }
            else
            {
                //Right shift the buffer.
                var @new = new OutputCell[BufferSize.Y, BufferSize.X];
                
                Array.Copy(contentBuffer, BufferSize.X, @new, 0, this.contentBuffer.Length - BufferSize.X);

                this.contentBuffer = @new;
                FillBuffer(text,  CurrentBufferSize.Y - 1);
                CurrentBufferSize.X = text.Length;

            }
        }

        /// <summary>
        /// Renders the buffer on the screen.
        /// </summary>
        public void Render()
        {
            var window = console.GetOutputBufferWindow();
            Render(window);
        }

        public void Render(Region window)
        {
            int height = Orgin.Y + window.Top + (contentBuffer.GetLength(0) - 1);
            int width = Orgin.X + window.Left + (contentBuffer.GetLength(1) - 1);

            int top = Orgin.Y + window.Top;
            int left = Orgin.X + window.Left;

            //If we're rendering at the bottom the orgin moves the content up.
            if (position == ContentPosition.Bottom)
            {
                height = window.Height - Orgin.Y;

                int sizeOfY = contentBuffer.GetLength(0);

                if (sizeOfY < window.Height)
                    top = window.Height - Orgin.Y - (contentBuffer.GetLength(0) - 1);
            }

            if (scroll == true)
            {
                top = Orgin.Y;
            }
            //Scrollable content regions will not move with the window so theres no point to save state.
            else
            {
                savedContentBuffer = null;
                savedContentBuffer = console.ReadOutput(new Region() { Left = left, Top = top, Height = height, Width = width });
            }

            savedCoordsWithOffset.X = left;
            savedCoordsWithOffset.Y = top;

            if (theOneToRuleThemAll != null)
            {
                RenderToContentRegion(this.contentBuffer);
            }
            else
            {
                console.WriteOutput(savedCoordsWithOffset, this.contentBuffer);
            }
        }

        private void RenderToContentRegion(OutputCell[,] source)
        {
            for (int y = 0; y < this.BufferSize.Y; y++)
            {
                for (int x = 0; x < this.BufferSize.X; x++)
                {
                    theOneToRuleThemAll.contentBuffer[savedCoordsWithOffset.Y + y, savedCoordsWithOffset.X + x] = source[y, x];
                }
            }
        }

        /// <summary>
        ///Hides the contents of this buffer and shows orginal contents under the region.
        /// </summary>
        public void Restore()
        {
            if (savedContentBuffer != null)
            {
                if (theOneToRuleThemAll != null)
                {
                    RenderToContentRegion(savedContentBuffer);
                }
                else
                {
                    console.WriteOutput(savedCoordsWithOffset, savedContentBuffer);
                }
            }
        }
    }
}
