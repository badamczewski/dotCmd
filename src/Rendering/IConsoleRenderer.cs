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

namespace dotCmd.Rendering
{
    public interface IConsoleRenderer
    {
        /// <summary>
        /// Gets the output buffer window size.
        /// </summary>
        /// <returns></returns>
        Coordinates GetOutputBufferWindowSize();

        /// <summary>
        /// Gets the output buffer windows as a rectangle.
        /// </summary>
        /// <returns></returns>
        Region GetOutputBufferWindow();

        /// <summary>
        /// Sets the cursor position.
        /// </summary>
        /// <param name="orgin"></param>
        void SetCursorPosition(Coordinates orgin);

        /// <summary>
        /// Writes lines of text into the output buffer at a specified coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        /// <param name="content"></param>
        void WriteOutput(Coordinates orgin, string[] content);
   
        /// <summary>
        /// Writes cell matrix of text into the output buffer at a specified coordinates.
        /// </summary>
        /// <param name="orgin"></param>
        /// <param name="content"></param>
        void WriteOutput(Coordinates orgin, CellBuffer cellBuffer);

        /// <summary>
        /// Reads cell matrix form the output buffer using the provided rectangle.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        CellBuffer ReadOutput(Region region);

        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }

        ColorMap ColorMap { get; }
    }
}
