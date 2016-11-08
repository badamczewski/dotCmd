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
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Rendering
{
    /// <summary>
    /// Maps ConsoleColors to RGB Colors and sends this configuration to 
    /// the current ConsoleHost session.
    /// </summary>
    public class ColorMap
    {
        private Dictionary<ConsoleColor, Color> colorMap = new Dictionary<ConsoleColor, Color>();
        private SafeFileHandle consoleHandle;
        //we have 13 color slots left from 1 to 14 how we arrived at this value ??? There are 15 ConsoleColors. 
        private int index = 1;
        
        /// <summary>
        /// Initializes and constructs the console map using the console handle.
        /// </summary>
        /// <param name="consoleHandle"></param>
        public ColorMap(SafeFileHandle consoleHandle)
        {
            this.consoleHandle = consoleHandle;

            ConstructMap();
        }

        /// <summary>
        /// Consctructs the Console Color map.
        /// </summary>
        private void ConstructMap()
        {
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX screenBufferInfo = DotConsoleNative.GetConsoleScreenBufferInfoExtended(consoleHandle);

            colorMap.Add(ConsoleColor.Black, Convert(screenBufferInfo.black.Color));
            colorMap.Add(ConsoleColor.White, Convert(screenBufferInfo.white.Color));
            colorMap.Add(ConsoleColor.Red, Convert(screenBufferInfo.red.Color));
            colorMap.Add(ConsoleColor.Green, Convert(screenBufferInfo.green.Color));
            colorMap.Add(ConsoleColor.Blue, Convert(screenBufferInfo.blue.Color));
            colorMap.Add(ConsoleColor.Gray, Convert(screenBufferInfo.gray.Color));
            colorMap.Add(ConsoleColor.Cyan, Convert(screenBufferInfo.cyan.Color));
            colorMap.Add(ConsoleColor.Magenta, Convert(screenBufferInfo.magenta.Color));

            colorMap.Add(ConsoleColor.DarkRed, Convert(screenBufferInfo.darkRed.Color));
            colorMap.Add(ConsoleColor.DarkGreen, Convert(screenBufferInfo.darkGreen.Color));
            colorMap.Add(ConsoleColor.DarkBlue, Convert(screenBufferInfo.darkBlue.Color));
            colorMap.Add(ConsoleColor.DarkGray, Convert(screenBufferInfo.darkGray.Color));
            colorMap.Add(ConsoleColor.DarkYellow, Convert(screenBufferInfo.darkYellow.Color));
            colorMap.Add(ConsoleColor.DarkCyan, Convert(screenBufferInfo.darkCyan.Color));
            colorMap.Add(ConsoleColor.DarkMagenta, Convert(screenBufferInfo.darkMagenta.Color));
        }

        /// <summary>
        /// Adds a new color to the map. 
        /// This remaps the current (not used) console color and increments the console color index
        /// so that the next not used color can be maped.
        /// </summary>
        /// <param name="targetColor"></param>
        public void AddColor(Color targetColor)
        {
            if (index >= (int)ConsoleColor.White)
            {
                index = 1;
            }

            ConsoleColor consoleColor = (ConsoleColor)index++;
            ChangeColor(consoleColor, targetColor);
        }

        /// <summary>
        /// Updates the map using the specified ConsoleColor key.
        /// </summary>
        /// <param name="keyColor"></param>
        /// <param name="targetColor"></param>
        public void ChangeColor(ConsoleColor keyColor, Color targetColor)
        {
            colorMap[keyColor] = targetColor;
            MapToConsoleHost(keyColor, targetColor);
        }

        /// <summary>
        /// Tries to find the Key in the map by using the RGB Color.
        /// </summary>
        /// <param name="targetColor"></param>
        /// <param name="keyColor"></param>
        /// <returns></returns>
        public bool TryGetMappedColor(Color targetColor, out ConsoleColor keyColor)
        {
            foreach(var c in colorMap)
            {
                if(c.Value.Equals(targetColor))
                {
                    keyColor = c.Key;
                    return true;
                }
            }

            keyColor = ConsoleColor.Black;
            return false;
        }

        /// <summary>
        /// Send the updated Color to the console host under the specified ConsoleColor key.
        /// </summary>
        /// <param name="keyColor"></param>
        /// <param name="targetColor"></param>
        private void MapToConsoleHost(ConsoleColor keyColor, Color targetColor)
        {
            ConsoleHostNativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX screenBufferInfo = DotConsoleNative.GetConsoleScreenBufferInfoExtended(consoleHandle);

            switch(keyColor)
            {
                case ConsoleColor.Black:
                    screenBufferInfo.black.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.White:
                    screenBufferInfo.white.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Blue:
                    screenBufferInfo.blue.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Cyan:
                    screenBufferInfo.cyan.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Red:
                    screenBufferInfo.red.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Green:
                    screenBufferInfo.green.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Magenta:
                    screenBufferInfo.magenta.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Gray:
                    screenBufferInfo.gray.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.Yellow:
                    screenBufferInfo.yellow.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkYellow:
                    screenBufferInfo.darkYellow.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkRed:
                    screenBufferInfo.darkRed.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkGreen:
                    screenBufferInfo.darkGreen.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkBlue:
                    screenBufferInfo.darkBlue.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkMagenta:
                    screenBufferInfo.darkMagenta.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkGray:
                    screenBufferInfo.darkGray.Color = Convert(colorMap[keyColor]);
                    break;
                case ConsoleColor.DarkCyan:
                    screenBufferInfo.darkCyan.Color = Convert(colorMap[keyColor]);
                    break;
            }

            // SUPER WHACKY CODE HERE.
            // WHY???!!! 
            // This looks to me like a bug but it's needed since buffer info seems to shrink one line at a time in every direction :|
            screenBufferInfo.window.Bottom++;
            screenBufferInfo.window.Right++;

            DotConsoleNative.SetConsoleScreenBufferInfoExtended(consoleHandle, screenBufferInfo);
        }

        /// <summary>
        /// Converts an RGB color to native console color.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static uint Convert(Color color)
        {
            return color.R + (color.G << 8) + (color.B << 16);
        }

        /// <summary>
        /// Converts native console color to RGB color.
        /// </summary>
        /// <param name="nativeColor"></param>
        /// <returns></returns>
        private static Color Convert(uint nativeColor)
        {
            Color color = new DataStructures.Color();
            color.R = (0x000000FFU & nativeColor);
            color.G = (0x0000FF00U & nativeColor) >> 8;
            color.B = (0x00FF0000U & nativeColor) >> 16;

            return color;
        }
    }
}
