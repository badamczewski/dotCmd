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
        private IConsoleRenderer renderer = null;

        public DotConsole()
        {
            Initialize();
        }

        private void Initialize()
        {
            renderer = new DotConsoleRenderer();
            //Set main thread name.
            System.Threading.Thread.CurrentThread.Name = ".Console host main thread";
            //Set encoding.
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            //Create the main content region.
            //This might not be the efficient way since content regions are extremly expensive so this may change.
            var size = renderer.GetOutputBufferWindowSize();
            main = new DotConsoleRegion(renderer, size);

            Console.BackgroundColor = main.BackgroundColor;
            Console.ForegroundColor = main.ForegroundColor;
            Console.CursorVisible = true;
        }

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
        public ConsoleColor BackgroundColor
        {
            get { return main.BackgroundColor; }
            set { SetBackgroudColors(value); }
        }

        /// <summary>
        /// Gets/Sets the Console Foreground Color.
        /// </summary>
        public ConsoleColor ForegroundColor
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
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        public int WriteLine(string text)
        {
            //Write to main region.
            return main.WriteLine(text);
        }

        /// <summary>
        /// Updates a line of text using the relative line index of the output buffer.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        public int WriteLine(string text, int relativeLineId)
        {
            //Write to main region.
            return main.UpdateLine(text, relativeLineId);
        }

        /// <summary>
        /// Updates a line by clearing it and appending the text using the relative line index of the output buffer.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        public int UpdateLine(string text, int relativeLineId)
        {
            //Write to main region.
            return main.UpdateLine(text, relativeLineId);
        }

        private void SetForegroundColors(ConsoleColor value)
        {
            main.ForegroundColor = value;
            Console.ForegroundColor = value;
            renderer.ForegroundColor = value;
        }

        private void SetBackgroudColors(ConsoleColor value)
        {
            main.BackgroundColor = value;
            Console.BackgroundColor = value;
            renderer.BackgroundColor = value;
        }
    }
}
