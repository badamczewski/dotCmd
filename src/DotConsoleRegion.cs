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

        private List<DotConsoleRegion> regions = new List<DotConsoleRegion>();
        private DotConsoleRegion parent = null;

        private CellBuffer savedContentBuffer = null;
        private CellBuffer contentBuffer = null;
        private Coordinates savedCoordsWithOffset;
        private Coordinates savedCursorPosition;
        private Coordinates currentBufferSize;
       
        /// <summary>
        /// Get the creation options of this Region.
        /// </summary>
        public RegionCreationOptions Options 
        { 
            get; private set; 
        }

        /// <summary>
        /// Gets the console renderer.
        /// </summary>
        public IConsoleRenderer Renderer
        {
            get { return Options.Renderer; }
        }

        /// <summary>
        /// Gets/Sets the console input loop.
        /// </summary>
        public IConsoleInputLoop InputLoop
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets/Sets the region visibility.
        /// </summary>
        public bool IsVisible
        {
            get { return Options.IsVisible; }
            set { Options.IsVisible = value; }
        }

        /// <summary>
        /// Gets/Sets the Console Background Color.
        /// </summary>
        public Color BackgroundColor
        {
            get { return Options.BackgroundColor; }
            set { Options.BackgroundColor = value; }
        }

        /// <summary>
        /// Gets/Sets the Console Foreground Color.
        /// </summary>
        public Color ForegroundColor
        {
            get { return Options.ForegroundColor; }
            set { Options.ForegroundColor = value; }
        }

       
        public DotConsoleRegion(RegionCreationOptions config)
        {
            Options = config;
  
            this.contentBuffer = new CellBuffer(Options.BufferSize.Y, Options.BufferSize.X);
            this.InputLoop = new DotConsoleInputLoop(this);

            if (Options.ForegroundColor.Equals(default(Color)))
                Options.ForegroundColor = new Color(255, 255, 255);

            if (Options.Parent != null)
            {
                this.parent = config.Parent;
                this.parent.RegisterRegion(this);
            }
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

        public void PreRender()
        {
            //TODO we need to switch to double buffering using SetConsoleActiveScreenBuffer.
            //Hide contents and show oryginal contents under the region.
            foreach (var region in regions)
            {
                if (region.IsVisible)
                    region.Restore(this);
            }

            if (IsVisible)
                this.Restore(parent);
        }

        public void PostRender()
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

            var wnd = Renderer.GetOutputBufferWindow();

            //Show content regions.
            foreach (var region in regions)
            {
                if (region.IsVisible)
                    region.Render(wnd, this);
            }

            if (IsVisible)
                this.Render(wnd, parent);
        }

        /// <summary>
        /// Calculates where to put the cursor.
        /// </summary>
        internal void CalculateCursorBetweenRegions()
        {
            var wnd = Renderer.GetOutputBufferWindow();

            //Pick the max buffer size and scroll to that spot, this is mainly done to reducre the flickering.
            var maxY = -1;
            var maxX = -1;
            DotConsoleRegion maxYRegion = null;
            int current = maxY;

            //Always pick the parent if child regions don't need to scroll the window.
            var wndY = wnd.Top + wnd.Height;

            foreach (var region in regions)
            { 
                current = region.currentBufferSize.Y + region.Options.Orgin.Y;
                if (current > maxY)
                {
                    maxY = current;
                    maxYRegion = region;
                }
            }

            current = this.currentBufferSize.Y + Options.Orgin.Y;
          
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
            maxX = Math.Min(maxYRegion.currentBufferSize.X + maxYRegion.Options.Orgin.X, maxYRegion.Options.BufferSize.X);

            var position = new Coordinates() { X = Math.Min(maxX, maxYRegion.Options.BufferSize.X - 1), Y = Math.Min(maxY, maxYRegion.Options.BufferSize.Y - 1) };

            if (savedCursorPosition.X != position.X || savedCursorPosition.Y != position.Y)
            {
                Renderer.SetCursorPosition(position);
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
            return Write(text, BackgroundColor, ForegroundColor, false);
        }

        /// <summary>
        /// Writes text into the output buffer and depending on the [fill] param clears and fills the whole line first with selected colors.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public WriteRef Write(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {
            int textLen = text != null ? text.Length : 0;
            int len = fill ? Options.BufferSize.X : textLen;

            AlterLine(text, this.currentBufferSize.Y, this.currentBufferSize.X, len, backgroundColor, foregroundColor);

            var @ref = new WriteRef(this.currentBufferSize.Y, this.currentBufferSize.X, textLen);

            currentBufferSize.X += text.Length;

            return @ref;
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text)
        {
            return WriteLine(text, BackgroundColor, ForegroundColor, false);
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {
            PreRender();
            try
            {
                this.currentBufferSize.X = 0;

                //Back up colors.
                var foregroundCopy = ForegroundColor;
                var backgroundCopy = BackgroundColor;
                ForegroundColor = foregroundColor;
                BackgroundColor = backgroundColor;

                var lineId = WriteLineToBuffer(text, fill);

                //Restore Colors.
                ForegroundColor = foregroundCopy;
                BackgroundColor = backgroundCopy;

                return new WriteRef(lineId, this.currentBufferSize.X, text != null ? text.Length : 0);
            }
            finally
            {
                PostRender();
            }
        }

        /// <summary>
        /// Alters the existing line with new text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId)
        {
            return AlterLine(text, relativeLineId, 0, text.Length, BackgroundColor, ForegroundColor);
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
            int len = fill ? Options.BufferSize.X : text.Length;

            return AlterLine(text, relativeLineId, 0, len, BackgroundColor, ForegroundColor);
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
            try
            {
                //Back up colors.
                var foregroundCopy = ForegroundColor;
                var backgroundCopy = BackgroundColor;
                ForegroundColor = foregroundColor;
                BackgroundColor = backgroundColor;

                if (Options.BufferSize.Y > relativeLineId)
                    FillBuffer(text, relativeLineId, relativeColumnId, columnLength);
                else
                    FillBuffer(text, relativeLineId - 1, relativeColumnId, columnLength);

                //Restore Colors.
                ForegroundColor = foregroundCopy;
                BackgroundColor = backgroundCopy;

                return new WriteRef(relativeLineId, relativeColumnId, columnLength);
            }
            finally
            {
                PostRender();
            }
        }

        /// <summary>
        /// Reads data from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef Read()
        {
            return InputLoop.ReadInput(
                new ReadOptions()
                {
                    BackgroundColor = BackgroundColor,
                    ForegroundColor = ForegroundColor
                });
        }

        /// <summary>
        /// Reads data from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef Read(ReadOptions options)
        {
            return InputLoop.ReadInput(options);
        }

        /// <summary>
        /// Reads a single key from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef ReadKey()
        {
            return InputLoop.ReadInput(
                new ReadOptions()
                {
                    BackgroundColor = BackgroundColor,
                    ForegroundColor = ForegroundColor,
                    ReadLength = 1
                });
        }

        /// <summary>
        /// Clears the output buffer.
        /// </summary>
        public void Clear()
        {
            PreRender();
            try
            {
                this.currentBufferSize.X = 0;
                this.currentBufferSize.Y = 0;

                for (int y = 0; y < this.contentBuffer.GetLengthOfY(); y++)
                {
                    for (int x = 0; x < this.contentBuffer.GetLengthOfX(); x++)
                    {
                        this.contentBuffer.Cells[y, x].Attributes = 0;
                        this.contentBuffer.Cells[y, x].Char = 0;
                    }
                }
            }
            finally
            {
                PostRender();
            }
        }

        /// <summary>
        /// Sets the cursor position using the provided coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetCursorPosition(Coordinates orgin)
        {
            //Use the renderer.
            this.Renderer.SetCursorPosition(orgin);
        }

        /// <summary>
        /// Gets the cursor position.
        /// </summary>
        /// <returns></returns>
        public Coordinates GetCursorPosition()
        {
            //Use the renderer.
            return this.Renderer.GetCursorPosition();
        }

        /// <summary>
        /// Gets the buffer position.
        /// </summary>
        /// <returns></returns>
        public Coordinates GetBufferPosition()
        {
            return this.currentBufferSize;
        }

        /// <summary>
        /// Sets the input buffer position using the provided coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetBufferPosition(Coordinates orgin)
        {
            this.currentBufferSize = orgin;
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        private int WriteLineToBuffer(string text, bool fill)
        {
            var lineId = currentBufferSize.Y;
            var colId =  currentBufferSize.X;

            int textLen = text != null ? text.Length : 0;
            int len = fill ? Options.BufferSize.X : textLen;

            var window = Renderer.GetOutputBufferWindow();

            if (Options.BufferSize.Y > currentBufferSize.Y)
            {
                FillBuffer(text, lineId, colId, len);
                
                currentBufferSize.Y++;
            }
            else
            {
                //Right shift the buffer.
                var @new = new OutputCell[Options.BufferSize.Y, Options.BufferSize.X];
                
                Array.Copy(contentBuffer.Cells, Options.BufferSize.X, @new, 0, this.contentBuffer.Length - Options.BufferSize.X);

                this.contentBuffer.Cells = @new;
                lineId = currentBufferSize.Y - 1;

                FillBuffer(text, lineId, colId, len);
            }

            return lineId;
        }

        /// <summary>
        /// Renders the buffer on the screen.
        /// </summary>
        public void Render(RenderingStrategy strategy)
        {
             var window = Renderer.GetOutputBufferWindow();

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
            int height = Options.Orgin.Y + window.Top + (contentBuffer.GetLengthOfY());
            int width = Options.Orgin.X + window.Left + (contentBuffer.GetLengthOfX());

            int top = Options.Orgin.Y + window.Top;
            int left = Options.Orgin.X + window.Left;

            //If we're rendering at the bottom the orgin moves the content up.
            if (Options.Position == ContentPosition.Bottom)
            {
                height = window.Height - Options.Orgin.Y;

                int sizeOfY = contentBuffer.GetLengthOfY();

                if (sizeOfY < window.Height)
                    top = window.Height - Options.Orgin.Y - (contentBuffer.GetLengthOfY());
            }

            if (Options.WillScrollContent == true)
            {
                top = Options.Orgin.Y;
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
                    savedContentBuffer = Renderer.ReadOutput(new Region() { Left = left, Top = top, Height = height, Width = width });
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
                Renderer.WriteOutput(savedCoordsWithOffset, this.contentBuffer);
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
                    Renderer.WriteOutput(savedCoordsWithOffset, savedContentBuffer);
                }
            }
        }

        private CellBuffer Restore(DotConsoleRegion owner, Region region)
        {
            CellBuffer result = new CellBuffer(Options.BufferSize.Y, Options.BufferSize.X);

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
            for (int y = 0; y < Options.BufferSize.Y; y++)
            {
                for (int x = 0; x < Options.BufferSize.X; x++)
                {
                    target[savedCoordsWithOffset.Y + y, savedCoordsWithOffset.X + x] = source[y, x];
                }
            }
        }

        private void FillBuffer(string text, int row, int col, int colLen)
        {
            //TODO Move this code out of here to ColorMap.
            ConsoleColor foregroundCopy = ConsoleColor.White;
            ConsoleColor backgroundCopy = ConsoleColor.Black;
            bool fcInMap = Renderer.ColorMap.TryGetMappedColor(ForegroundColor, out foregroundCopy);
            bool bcInMap = Renderer.ColorMap.TryGetMappedColor(BackgroundColor, out backgroundCopy);

            if (fcInMap == false) //add this color to map.
            {
                Renderer.ColorMap.AddColor(ForegroundColor);
                Renderer.ColorMap.TryGetMappedColor(ForegroundColor, out foregroundCopy);
            }

            if(bcInMap == false)
            {
                Renderer.ColorMap.AddColor(BackgroundColor);
                Renderer.ColorMap.TryGetMappedColor(BackgroundColor, out backgroundCopy);
            }

            int textIndex = 0;
            for (int idx = col; idx < col + colLen; idx++)
            {
                if (idx >= Options.BufferSize.X)
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

                    this.contentBuffer.Cells[row, idx].Attributes = (ushort)DotConsoleNative.ToNativeConsoleColor(foregroundCopy, backgroundCopy);
                }
                textIndex++;
            }
        
        }
    }
}
