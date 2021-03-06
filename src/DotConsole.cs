﻿#region Licence
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
using dotCmd.Controls;
using dotCmd.DataStructures;
using dotCmd.Native;
using dotCmd.Rendering;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dotCmd
{
    /// <summary>
    /// Represents the standard input, output, error streams and renderable regions for console applications.
    /// </summary>
    public class DotConsole : IConsole
    {
        private List<DotConsoleRegion> regions = new List<DotConsoleRegion>();
        private DotConsoleRegion main = null;
        private DotConsoleRenderer renderer = null;

        public DotConsole()
        {
            Initialize();
        }

        private void Initialize()
        {
            renderer = new DotConsoleRenderer();

            //Set encoding.
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            //Create the main content region.
            //This might not be the efficient way since content regions are extremly expensive so this may change.
            var size = renderer.GetOutputBufferWindowSize();

            var options = new RegionCreationOptions(renderer, null, size);
            options.WillScrollContent = true;
            
            main = new DotConsoleRegion(options);
               
            this.BackgroundColor = main.Options.BackgroundColor;
            this.ForegroundColor = main.Options.ForegroundColor;
  
            Console.CursorVisible = true;

            Controls = new List<IConsoleControl>();
        }

        public IEnumerable<IConsoleControl> Controls { get; private set; }

        /// <summary>
        /// Gets the Console Renderer
        /// </summary>
        public IConsoleRenderer Renderer
        {
            get { return renderer; }
        }

        /// <summary>
        /// Get the console main rendering surface area.
        /// </summary>
        public DotConsoleRegion RootRegion
        {
            get { return main; }
        }

        /// <summary>
        /// Gets/Sets the Console Background Color.
        /// </summary>
        public Color BackgroundColor
        {
            get { return main.BackgroundColor; }
            set { SetBackgroudColors(value); }
        }

        /// <summary>
        /// Gets/Sets the Console Foreground Color.
        /// </summary>
        public Color ForegroundColor
        {
            get { return main.ForegroundColor; }
            set { SetForegroundColors(value); }
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

        /// <summary>
        /// Writes text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef Write(string text)
        {
            //Write to main region.
            return main.Write(text);
        }

        /// <summary>
        /// Writes text into the output buffer and depending on the [fill] param clears and fills the whole line first with selected colors.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public WriteRef Write(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {
            return main.Write(text, backgroundColor, foregroundColor, fill);
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text)
        {
            //Write to main region.
            return main.WriteLine(text);
        }

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public WriteRef WriteLine(string text, Color backgroundColor, Color foregroundColor, bool fill)
        {
            return main.WriteLine(text, backgroundColor, foregroundColor, fill);
        }

        /// <summary>
        /// Alters the existing line with new text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId)
        {
            return main.AlterLine(text, relativeLineId);
        }

        /// <summary>
        /// Alters the existing line with new text and depending on the [cleraFirst] param clears the whole line first.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <param name="clearFirst"></param>
        /// <returns></returns>
        public WriteRef AlterLine(string text, int relativeLineId, bool clearFirst)
        {
            return main.AlterLine(text, relativeLineId, clearFirst);
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
            return main.AlterLine(text, relativeLineId, relativeColumnId, columnLength, backgroundColor, foregroundColor);

        }

        /// <summary>
        /// Reads data from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef Read()
        {
            return main.Read();
        }

        /// <summary>
        /// Reads data from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef Read(ReadOptions options)
        {
            return main.Read(options);
        }

        /// <summary>
        /// Reads a single key from the input buffer until a break key(s) is found.
        /// </summary>
        /// <returns></returns>
        public ReadRef ReadKey()
        {
            return main.ReadKey();
        }

        /// <summary>
        /// Clears the output buffer.
        /// </summary>
        public void Clear()
        {
            main.Clear();
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
            return this.main.GetBufferPosition();
        }

        /// <summary>
        /// Sets the input buffer position using the provided coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        public void SetBufferPosition(Coordinates orgin)
        {
            this.main.SetBufferPosition(orgin);
        }

        public void AddControl(IConsoleControl control)
        {
            control.Initialize(main);

            ((List<IConsoleControl>)this.Controls).Add(control);
        }

        private void SetForegroundColors(Color value)
        {
            main.ForegroundColor = value;
            ConsoleColor consoleColor = ConsoleColor.White;
            if (renderer.ColorMap.TryGetMappedColor(value, out consoleColor) == false)
            {
                renderer.ColorMap.ChangeColor(ConsoleColor.White, value);
            }

            Console.ForegroundColor = consoleColor;
            renderer.ForegroundColor = consoleColor;
        }

        private void SetBackgroudColors(Color value)
        {
            main.BackgroundColor = value;
            ConsoleColor consoleColor = ConsoleColor.Black;
            if (renderer.ColorMap.TryGetMappedColor(value, out consoleColor) == false)
            {
                renderer.ColorMap.ChangeColor(ConsoleColor.Black, value);
            }

            Console.BackgroundColor = consoleColor;
            renderer.BackgroundColor = consoleColor;
        }
    }
}
