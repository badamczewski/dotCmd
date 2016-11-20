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

        private CellBuffer savedContentBuffer = null;
        private CellBuffer contentBuffer = null;
        private Coordinates savedCoordsWithOffset;
        private Coordinates savedCursorPosition;

        public IConsoleRenderer ConsoleRenderer { get; set; }
        public DotConsoleInputLoop InputLoop { get; set; }

        private ContentPosition position;
        private List<DotConsoleRegion> regions = new List<DotConsoleRegion>();
        private DotConsoleRegion parent = null;
 
        private bool scroll = false;

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        //Structs are hepas of fun but they make the worst properties so we just expose them as fields.
        public Coordinates BufferSize; 
        public Coordinates CurrentBufferSize; 
        public Coordinates Orgin;
       
        //This constructor is used by Dotconsole to register the main buffer within it.
        internal DotConsoleRegion(
            IConsoleRenderer consoleRenderer, 
            Coordinates bufferSize) 
        {
            this.ConsoleRenderer = consoleRenderer;
            this.InputLoop = new DotConsoleInputLoop(this);
  
            this.BufferSize = bufferSize;
            this.Orgin = new Coordinates(0, 0);
            this.position = ContentPosition.Bottom;

            this.contentBuffer = new CellBuffer(bufferSize.Y, bufferSize.X);

            this.scroll = true;

            this.BackgroundColor = new Color(0, 0, 80); 
            this.ForegroundColor = new Color(255, 255, 255);
        }

        public DotConsoleRegion(IConsoleRenderer console, 
            DotConsoleRegion parent,
            Coordinates bufferSize, 
            Coordinates orgin, 
            ContentPosition position, 
            bool scroll = false,
            Color backgroundColor = default(Color),
            Color foregroundColor = default(Color))
        {
            this.ConsoleRenderer = console;
            this.BufferSize = bufferSize;
            this.Orgin = orgin;
            this.position = position;

            this.contentBuffer = new CellBuffer(bufferSize.Y, bufferSize.X);
            this.InputLoop = new DotConsoleInputLoop(this);

            this.scroll = scroll;

            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;

            if (foregroundColor.Equals(default(Color)))
                this.ForegroundColor = new Color(255, 255, 255);

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

            this.Restore(parent);
        }

        private void PostRender()
        {
            //Only root node should set the cursor.
            //Since all buffers have known positions we can pre set cursor position
            //and then render contents.
            if (parent == null)
            {
                //Calculate curtor position.
                //We only call this function a single time since moving the cursor between regions
                //introduces lots of flicker.
                CalculateCursorBetweenRegions();
            }

            var wnd = ConsoleRenderer.GetOutputBufferWindow();

            //Show content regions.
            foreach (var region in regions)
                region.Render(wnd, this);

            this.Render(wnd, parent);
        }

        /// <summary>
        /// Calculates where to put the cursor.
        /// </summary>
        internal void CalculateCursorBetweenRegions()
        {
            var wnd = ConsoleRenderer.GetOutputBufferWindow();

            //Pick the max buffer size and scroll to that spot, this is mainly done to reducre the flickering.
            var maxY = -1;
            var maxX = -1;
            DotConsoleRegion maxYRegion = null;
            int current = maxY;

            //Always pick the parent if child regions don't need to scroll the window.
            var wndY = wnd.Top + wnd.Height;

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
            else if(maxY < wndY)
            {
                maxYRegion = this;
                maxY = current;
            }

            //Once we have the region with the biggest value of Y we use it's X coordinate.
            maxX = Math.Min(maxYRegion.CurrentBufferSize.X + maxYRegion.Orgin.X, maxYRegion.BufferSize.X);

            var position = new Coordinates() { X = Math.Min(maxX, maxYRegion.BufferSize.X - 1), Y = Math.Min(maxY, maxYRegion.BufferSize.Y - 1) };

            if (savedCursorPosition.X != position.X || savedCursorPosition.Y != position.Y)
            {
                ConsoleRenderer.SetCursorPosition(position);
                savedCursorPosition = position;
            }
        }

        /// <summary>
        /// Writes text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public WriteRef Write(string text)
        {
            PreRender();

            AlterLine(text, this.CurrentBufferSize.Y, this.CurrentBufferSize.X, text.Length, this.BackgroundColor, this.ForegroundColor);

            var @ref = new WriteRef(this.CurrentBufferSize.Y, this.CurrentBufferSize.X, text.Length);

            CurrentBufferSize.X += text.Length;

            PostRender();

            return @ref;

        }

        /// <summary>
        /// Writes text into the output buffer and depending on the [fill] param clears and fills the whole line first with selected colors.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public WriteRef Write(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {

            PreRender();

            int len = fill ? this.BufferSize.X : text.Length;

            var fc = this.ForegroundColor;
            this.ForegroundColor = foregroundColor;

            var bc = this.BackgroundColor;
            this.BackgroundColor = backgroundColor;

            AlterLine(text, this.CurrentBufferSize.Y, this.CurrentBufferSize.X, len, this.BackgroundColor, this.ForegroundColor);

            var @ref = new WriteRef(this.CurrentBufferSize.Y, this.CurrentBufferSize.X, text.Length);

            CurrentBufferSize.X += text.Length;

            this.ForegroundColor = fc;
            this.BackgroundColor = bc;

            PostRender();

            return @ref;
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text)
        {
            return WriteLine(text, this.BackgroundColor, this.ForegroundColor, false);
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {
            PreRender();

            this.CurrentBufferSize.X = 0;

            var fc = this.ForegroundColor;
            this.ForegroundColor = foregroundColor;

            var bc = this.BackgroundColor;
            this.BackgroundColor = backgroundColor;

            var lineId = WriteLineToBuffer(text, fill);

            this.ForegroundColor = fc;
            this.BackgroundColor = bc;

            PostRender();

            return new WriteRef(lineId, this.CurrentBufferSize.X, text.Length);
        }

        /// <summary>
        /// Alters the existing line with new text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId)
        {
            return AlterLine(text, relativeLineId, 0, text.Length, this.BackgroundColor, this.ForegroundColor);
        }

        /// <summary>
        /// Alters the existing line with new text and depending on the [cleraFirst] param clears the whole line first.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId, bool fill)
        {
            int len = fill ? this.BufferSize.X : text.Length;

            return AlterLine(text, relativeLineId, 0, len, this.BackgroundColor, this.ForegroundColor);
        }


        /// <summary>
        /// Alters the existing line at the specified column (X) position with new text and color palete.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <param name="relativeColumnId"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="foregroundColor"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId, int relativeColumnId, int columnLength, Color backgroundColor, Color foregroundColor)
        {
            PreRender();

            var fc = this.ForegroundColor;
            this.ForegroundColor = foregroundColor;

            var bc = this.BackgroundColor;
            this.BackgroundColor = backgroundColor;

            FillBuffer(text, relativeLineId, relativeColumnId, columnLength);

            this.ForegroundColor = fc;
            this.BackgroundColor = bc;

            PostRender();

            return new WriteRef(relativeLineId, relativeColumnId, columnLength);
        }


        /// <summary>
        /// Reads data from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef Read()
        {
            return InputLoop.ReadInput(
                new InputOptions()
                {
                    BackgroundColor = this.BackgroundColor,
                    ForegroundColor = this.ForegroundColor
                });
        }

        /// <summary>
        /// Reads a single key from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef ReadKey()
        {
            return InputLoop.ReadInput(
                new InputOptions()
                {
                    BackgroundColor = this.BackgroundColor,
                    ForegroundColor = this.ForegroundColor,
                    ReadLength = 1
                });
        }

        /// <summary>
        /// Clears the output buffer.
        /// </summary>
        public void Clear()
        {
            PreRender();

            this.CurrentBufferSize.X = 0;
            this.CurrentBufferSize.Y = 0;

            for (int y = 0; y < this.contentBuffer.GetLengthOfY(); y++)
            {
                for(int x = 0; x < this.contentBuffer.GetLengthOfX(); x++)
                {
                    this.contentBuffer.Cells[y, x].Attributes = 0;
                    this.contentBuffer.Cells[y,x].Char = 0;
                }
            }

            PostRender();
        }

        /// <summary>
        /// Sets the cursor position using the provided coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetCursorPosition(Coordinates orgin)
        {
            //Use the renderer.
            this.ConsoleRenderer.SetCursorPosition(orgin);
        }

        /// <summary>
        /// Gets the cursor position.
        /// </summary>
        /// <returns></returns>
        public Coordinates GetCursorPosition()
        {
            //Use the renderer.
            return this.ConsoleRenderer.GetCursorPosition();
        }

        /// <summary>
        /// Gets the buffer position.
        /// </summary>
        /// <returns></returns>
        public Coordinates GetBufferPosition()
        {
            return this.CurrentBufferSize;
        }

        /// <summary>
        /// Sets the input buffer position using the provided coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetBufferPosition(Coordinates orgin)
        {
            this.CurrentBufferSize = orgin;
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        private int WriteLineToBuffer(string text, bool fill)
        {
            var lineId = 0;
            var colId = this.CurrentBufferSize.X;
            int len = fill ? BufferSize.X : text.Length;
            var window = ConsoleRenderer.GetOutputBufferWindow();

            if (BufferSize.Y > CurrentBufferSize.Y)
            {
                lineId = CurrentBufferSize.Y;

                FillBuffer(text,  lineId, colId, len);
                
                CurrentBufferSize.Y++;
            }
            else
            {
                //Right shift the buffer.
                var @new = new OutputCell[BufferSize.Y, BufferSize.X];
                
                Array.Copy(contentBuffer.Cells, BufferSize.X, @new, 0, this.contentBuffer.Length - BufferSize.X);

                this.contentBuffer.Cells = @new;
                lineId = CurrentBufferSize.Y - 1;
                FillBuffer(text, lineId, colId, len);
            }

            return lineId;
        }

        /// <summary>
        /// Renders the buffer on the screen.
        /// </summary>
        public void Render(RenderingStrategy strategy)
        {
             var window = ConsoleRenderer.GetOutputBufferWindow();

            //Go up the chain to root and render there.
             if (strategy == RenderingStrategy.BubbleUp)
             {
                 Restore(parent);
                 Render(window, parent);
                 //If there's no parent then we are root and we need to render here.
                 if (parent != null)
                     parent.Render(strategy);
             }
             else if(strategy == RenderingStrategy.RenderHere)
             {
                 Restore(null);
                 Render(window, null);
             }
        }

        internal void Render(Region window, DotConsoleRegion owner)
        {
            int height = Orgin.Y + window.Top + (contentBuffer.GetLengthOfY());
            int width = Orgin.X + window.Left + (contentBuffer.GetLengthOfX());

            int top = Orgin.Y + window.Top;
            int left = Orgin.X + window.Left;

            //If we're rendering at the bottom the orgin moves the content up.
            if (position == ContentPosition.Bottom)
            {
                height = window.Height - Orgin.Y;

                int sizeOfY = contentBuffer.GetLengthOfY();

                if (sizeOfY < window.Height)
                    top = window.Height - Orgin.Y - (contentBuffer.GetLengthOfY());
            }

            if (scroll == true)
            {
                top = Orgin.Y;
            }
            //Scrollable content regions will not move with the window so theres no point to save state.
            else
            {
                savedContentBuffer = null;
                if (owner != null)
                {
                    savedContentBuffer = Restore(owner, new Region() { Left = left, Top = top, Height = height, Width = width });
                }
                else
                {
                    savedContentBuffer = ConsoleRenderer.ReadOutput(new Region() { Left = left, Top = top, Height = height, Width = width });
                }
            }

            savedCoordsWithOffset.X = left;
            savedCoordsWithOffset.Y = top;

            if (owner != null)
            {
                Merge(owner.contentBuffer, this.contentBuffer);
            }
            else
            {             
                ConsoleRenderer.WriteOutput(savedCoordsWithOffset, this.contentBuffer);
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
                    Merge(owner.contentBuffer, savedContentBuffer);
                }
                else
                {
                    ConsoleRenderer.WriteOutput(savedCoordsWithOffset, savedContentBuffer);
                }
            }
        }

        private CellBuffer Restore(DotConsoleRegion owner, Region region)
        {
            CellBuffer result = new CellBuffer(this.BufferSize.Y, this.BufferSize.X);

            int resultY = 0;
            int resultX = 0;

            int height = region.Top + (region.Height - region.Top);
            int width = region.Left + (region.Width - region.Left);

            for (int y = region.Top; y < height; y++)
            {
                resultX = 0;
                for (int x = region.Left; x < width; x++)
                {
                    result[resultY, resultX] = owner.contentBuffer[y, x];
                    resultX++;
                }

                resultY++;
            }

            return result;
        }

        private void Merge(CellBuffer target, CellBuffer source)
        {
            for (int y = 0; y < this.BufferSize.Y; y++)
            {
                for (int x = 0; x < this.BufferSize.X; x++)
                {
                    target[savedCoordsWithOffset.Y + y, savedCoordsWithOffset.X + x] = source[y, x];
                }
            }
        }

        private void FillBuffer(string text, int row, int col, int colLen)
        {
            //TODO Move this code out of here to ColorMap.
            ConsoleColor fc = ConsoleColor.White;
            ConsoleColor bc = ConsoleColor.Black;
            bool fcInMap = ConsoleRenderer.ColorMap.TryGetMappedColor(this.ForegroundColor, out fc);
            bool bcInMap = ConsoleRenderer.ColorMap.TryGetMappedColor(this.BackgroundColor, out bc);

            if (fcInMap == false) //add this color to map.
            {
                ConsoleRenderer.ColorMap.AddColor(this.ForegroundColor);
                ConsoleRenderer.ColorMap.TryGetMappedColor(this.ForegroundColor, out fc);
            }

            if(bcInMap == false)
            {
                ConsoleRenderer.ColorMap.AddColor(this.BackgroundColor);
                ConsoleRenderer.ColorMap.TryGetMappedColor(this.BackgroundColor, out bc);
            }

            int textIndex = 0;
            for (int idx = col; idx < col + colLen; idx++)
            {
                if (idx >= BufferSize.X)
                    break;

                //Clear this char
                if (text == null || text.Length == 0)
                {
                    this.contentBuffer.Cells[row, idx].Char = 0;
                    this.contentBuffer.Cells[row, idx].Attributes = 0;
                }
                else
                {
                    if (textIndex < text.Length)
                        this.contentBuffer.Cells[row, idx].Char = (ushort)text[textIndex];

                    this.contentBuffer.Cells[row, idx].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(fc, bc);
                }
                textIndex++;
            }
        
        }
    }
}
