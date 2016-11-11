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
using dotCmd.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd
{
    /// <summary>
    /// Describes the possible operations that the user can do using the Console.
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        int WriteLine(string text);

        /// <summary>
        /// Writes a line of text into the output buffer.
        /// </summary>
        /// <param name="text"></param>
        int WriteLine(string text, Color backgroundColor, Color foregroundColor, bool fill);

        /// <summary>
        /// Alters the existing line with new text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <returns></returns>
        int AlterLine(string text, int relativeLineId);
   
        /// <summary>
        /// Alters the existing line with new text and depending on the [cleraFirst] param clears the whole line first.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        int AlterLine(string text, int relativeLineId, bool fill);
     
        /// <summary>
        /// Alters the existing line at the specified column (X) position with new text and color palete.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="relativeLineId"></param>
        /// <param name="relativeColumnId"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="foregroundColor"></param>
        /// <returns></returns>
        int AlterLine(string text, int relativeLineId, int relativeColumnId, int columnLength, Color backgroundColor, Color foregroundColor);

        
    }
}
