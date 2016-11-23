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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Controls
{
    public class ProgressBar : IConsoleControl
    {
        public ProgressBar() {}

        public ProgressBar(DotConsoleRegion parent)
        {
            Initialize(parent);
        }

        private DotConsoleRegion progressBarRegion;
        private bool isFilled;
        private WriteRef @ref;
        private Coordinates bufferSize;
        
        public void Initialize(DotConsoleRegion parent)
        {
            bufferSize = new Coordinates(parent.Options.BufferSize.X, 3);

            RegionCreationOptions init = new RegionCreationOptions(parent.Renderer, parent, bufferSize);
            init.Orgin = new Coordinates(0, 1);
            init.Position = DotConsoleRegion.ContentPosition.Top;

            progressBarRegion = new DotConsoleRegion(init);
        }

        public Color BackgroundColor
        {
            get { return progressBarRegion.BackgroundColor; }
            set { progressBarRegion.BackgroundColor = value; }
        }

        public Color ForegroundColor
        {
            get { return progressBarRegion.ForegroundColor; }
            set { progressBarRegion.ForegroundColor = value; }
        }

        public bool IsVisible
        {
            get { return progressBarRegion.IsVisible; }
            set { progressBarRegion.IsVisible = value; }
        }

        public void SetProgress(int progress)
        {
            if (isFilled == false)
            {
                FillRegion();
                isFilled = true;
            }
            else
            {
                var wnd = progressBarRegion.Renderer.GetOutputBufferWindow();
                int hundred = progressBarRegion.Options.BufferSize.X;

                if(hundred > wnd.Width)
                    hundred = wnd.Width;

                hundred -= 2;

                decimal singlePercent = (decimal)hundred / 100;
                decimal progressInPercent = progress * singlePercent;

                int starsToGenerate = (int)Math.Round(progressInPercent);

                string progressBarLine = new string('*', starsToGenerate);

                if (hundred >= starsToGenerate)
                {
                    string remainder = new string(' ', hundred - starsToGenerate);

                    progressBarRegion.AlterLine(string.Format("[{0}{1}]", progressBarLine, remainder), @ref.RelativeRowIndex);
                }
            }
        }

        private void FillRegion()
        {
            progressBarRegion.WriteLine(" ", progressBarRegion.BackgroundColor, progressBarRegion.ForegroundColor, true);
            @ref = progressBarRegion.WriteLine(" ", progressBarRegion.BackgroundColor, progressBarRegion.ForegroundColor, true);
            progressBarRegion.WriteLine(" ", progressBarRegion.BackgroundColor, progressBarRegion.ForegroundColor, true);
        }
    }
}
