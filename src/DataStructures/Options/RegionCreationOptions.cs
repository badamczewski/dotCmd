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
using dotCmd.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.DataStructures
{
    public class RegionCreationOptions
    {
        public IConsoleRenderer Renderer { get; private set; }
        public DotConsoleRegion Parent { get; private set; }
        public Coordinates BufferSize { get; private set; }

        //Optional parameters.
        public Coordinates Orgin { get; set; }
        public dotCmd.DotConsoleRegion.ContentPosition Position { get; set; }
        public bool WillScrollContent { get; set; }
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public bool IsVisible { get; set; }

        public RegionCreationOptions(IConsoleRenderer renderer, DotConsoleRegion parent, Coordinates bufferSize)
        {
            this.Renderer = renderer;
            this.Parent = parent;
            this.BufferSize = bufferSize;

            this.Orgin = default(Coordinates);
            this.WillScrollContent = false;
            this.ForegroundColor = new Color(255, 255, 255);
            this.BackgroundColor = new Color(0, 0, 100);
            this.Position = DotConsoleRegion.ContentPosition.Top;
            this.IsVisible = true;
        }
    }
}
