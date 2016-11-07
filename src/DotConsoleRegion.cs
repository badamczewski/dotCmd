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
using dotCmd.Rendering;
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
    /// Content regions are organized in a form of a tree, where each region can have multiple child regions,
    /// and each content region can render it's contents to a parent or call renderer to render it directly to
    /// the output buffer.
    /// </summary>
    public class DotConsoleRegion : IConsole
    {
        /// <summary>
        /// Content position relative to the visible console screen.
        /// </summary>
        public enum ContentPosition
        {
            /// <summary>
            /// Content will be rendered at the top, any positive orgin Y value will move the content down.
            /// </summary>
            Top,
            /// <summary>
            /// Content will be rendered at the bottom, any positive orgin Y value will move the content up.
            /// </summary>
            Bottom
        }

        /// <summary>
        /// Who should be reponsible for content rendering in the ContentRegion tree.
        /// </summary>
        public enum RenderingStrategy
        {
            /// <summary>
            /// [Default] Move up the ContentRegion chain and render at the root.
            /// (This method is fast but it's impossible to do independent multithreading rendering with it)
            /// 
            /// </summary>
            BubbleUp,
            /// <summary>
            /// Render directly at this level. 
            /// (This method is slow and can cause flickering but can be used in a multithreaded scenario)
            /// </summary>
            RenderHere
        }

        private OutputCell[,] savedContentBuffer = null;
        private OutputCell[,] contentBuffer = null;
        private Coordinates savedCoordsWithOffset;

        private IConsoleRenderer consoleRenderer;
        private ContentPosition position;

        private List<DotConsoleRegion> regions = new List<DotConsoleRegion>();
        private DotConsoleRegion parent = null;
 
        private bool scroll = false;

        public ConsoleColor BackgroundColor { get; set; }
        public ConsoleColor ForegroundColor { get; set; }

        //Structs are hepas of fun but they make the worst properties so we just expose them as fields.
        public Coordinates BufferSize; 
        public Coordinates CurrentBufferSize; 
        public Coordinates Orgin;
       
        //This constructor is used by Dotconsole to register the main buffer within it.
        internal DotConsoleRegion(IConsoleRenderer consoleRenderer, Coordinates bufferSize) 
        {
            this.consoleRenderer = consoleRenderer;
  
            this.BufferSize = bufferSize;
            this.Orgin = new Coordinates(0, 0);
            this.position = ContentPosition.Bottom;

            this.contentBuffer = new OutputCell[bufferSize.Y, bufferSize.X];

            this.scroll = true;

            this.BackgroundColor = ConsoleColor.Black;
            this.ForegroundColor = ConsoleColor.White;
        }

        public DotConsoleRegion(IConsoleRenderer console, 
            DotConsoleRegion parent,
            Coordinates bufferSize, Coordinates orgin, ContentPosition position, 
            bool scroll = false, 
            ConsoleColor backgroundColor = ConsoleColor.Black,
            ConsoleColor foregroundColor = ConsoleColor.White)
        {
            this.consoleRenderer = console;
            this.BufferSize = bufferSize;
            this.Orgin = orgin;
            this.position = position;

            this.contentBuffer = new OutputCell[bufferSize.Y, bufferSize.X];

            this.scroll = scroll;

            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;

            this.parent = parent;
            this.parent.RegisterRegion(this);
        }

        /// <summary>
        /// Registers a content region and reconfigures it so that they
        /// are controled by this dotConsole instance.
        /// </summary>
        /// <param name="region"></param>
        public void RegisterRegion(DotConsoleRegion region)
        {
            this.regions.Add(region);
        }

        private void PreRender()
        {
            //TODO we need to switch to double buffering using SetConsoleActiveScreenBuffer.
            //Hide contents and show oryginal contents under the region.
            foreach (var region in regions)
                region.Restore(this);
        }

        private void PostRender()
        {
            var wnd = consoleRenderer.GetOutputBufferWindow();

            //Show content regions.
            foreach (var region in regions)
                region.Render(wnd, this);

            this.Render(wnd, parent);

            //Only root node should set the cursor.
            if (parent == null)
                //Calculate curtor position.
                //We only call this function a single time since moving the cursor between regions
                //introduces lots of flicker.
                CalculateCursorBetweenRegions();
        }

        /// <summary>
        /// Calculates where to put the cursor, it picks the maximum buffer corrdintates for any ContentRegion.
        /// </summary>
        internal void CalculateCursorBetweenRegions()
        {
            //Pick the max buffer size and scroll to that spot, this is mainly done to reducre the flickering.
            var maxY = -1;
            var maxX = -1;
            DotConsoleRegion maxYRegion = null;
            int current = maxY;

            foreach (var region in regions)
            {
                current = region.CurrentBufferSize.Y + region.Orgin.Y;
                if (current > maxY)
                {
                    maxY = current;
                    maxYRegion = region;
                }
            }

            current = this.CurrentBufferSize.Y + this.Orgin.Y;
            if (current > maxY)
            {
                maxYRegion = this;
                maxY = current;
            }

            //Once we have the region with the biggest value of Y we use it's X coordinate.
            maxX = Math.Min(maxYRegion.CurrentBufferSize.X + maxYRegion.Orgin.X, maxYRegion.BufferSize.X);

            consoleRenderer.SetCursorPosition(new Coordinates() { X = Math.Max(maxX - 1, 0), Y = Math.Max(maxY - 1, 0) });
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public int WriteLine(string text)
        {
            PreRender();

            var lineId = WriteLineToBuffer(text);

            PostRender();

            return lineId;
        }

        /// <summary>
        /// Updates a line of text using the relative line index of the output buffer.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        public int UpdateLine(string text, int relativeLineId)
        {
            PreRender();

            FillBuffer(text, relativeLineId);

            PostRender();

            return relativeLineId;
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        private int WriteLineToBuffer(string text)
        {
            var lineId = 0;
            var window = consoleRenderer.GetOutputBufferWindow();

            if (BufferSize.Y > CurrentBufferSize.Y)
            {
                lineId = CurrentBufferSize.Y;
                FillBuffer(text,  lineId);
                
                CurrentBufferSize.Y++;
                CurrentBufferSize.X = text.Length;
            }
            else
            {
                //Right shift the buffer.
                var @new = new OutputCell[BufferSize.Y, BufferSize.X];
                
                Array.Copy(contentBuffer, BufferSize.X, @new, 0, this.contentBuffer.Length - BufferSize.X);

                this.contentBuffer = @new;
                lineId = CurrentBufferSize.Y - 1;
                FillBuffer(text,  lineId);
                CurrentBufferSize.X = text.Length;

            }

            return lineId;
        }

        /// <summary>
        /// Renders the buffer on the screen.
        /// </summary>
        public void Render(RenderingStrategy strategy)
        {
             var window = consoleRenderer.GetOutputBufferWindow();

            //Go up the chain to root and render there.
             if (strategy == RenderingStrategy.BubbleUp)
             {
                 Render(window, parent);
                 //If there's no parent then we are root and we need to render here.
                 if (parent != null)
                     parent.Render(strategy);
             }
             else if(strategy == RenderingStrategy.RenderHere)
             {
                 Render(window, null);
             }
        }

        internal void Render(Region window, DotConsoleRegion owner)
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
                savedContentBuffer = consoleRenderer.ReadOutput(new Region() { Left = left, Top = top, Height = height, Width = width });
            }

            savedCoordsWithOffset.X = left;
            savedCoordsWithOffset.Y = top;

            if (owner != null)
            {
                RenderToContentRegion(owner, this.contentBuffer);
            }
            else
            {
                
                consoleRenderer.WriteOutput(savedCoordsWithOffset, this.contentBuffer);
            }
        }

        /// <summary>
        ///Hides the contents of this buffer and shows orginal contents under the region.
        /// </summary>
        public void Restore()
        {
            Restore(null);
        }

        /// <summary>
        ///Hides the contents of this buffer and shows orginal contents under the region.
        /// </summary>
        internal void Restore(DotConsoleRegion owner)
        {
            if (savedContentBuffer != null)
            {
                if (owner != null)
                {
                    RenderToContentRegion(owner, savedContentBuffer);
                }
                else
                {
                    consoleRenderer.WriteOutput(savedCoordsWithOffset, savedContentBuffer);
                }
            }
        }

        private void RenderToContentRegion(DotConsoleRegion owner, OutputCell[,] source)
        {
            for (int y = 0; y < this.BufferSize.Y; y++)
            {
                for (int x = 0; x < this.BufferSize.X; x++)
                {
                    owner.contentBuffer[savedCoordsWithOffset.Y + y, savedCoordsWithOffset.X + x] = source[y, x];
                }
            }
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
    }
}
