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
    /// Represents an input loop, that reads content from the input buffer.
    /// </summary>
    public class DotConsoleInputLoop : IConsoleInputLoop
    {
        private IConsole parent;

        public DotConsoleInputLoop(IConsole parent)
        {
            this.parent = parent;
        }

        //Create a single input buffer.
        private Lazy<SafeFileHandle> inputBuffer = new Lazy<SafeFileHandle>(DotConsoleNative.CreateInputBuffer);

        /// <summary>
        /// Reads the input from the input buffer.
        /// </summary>
        public ReadRef ReadInput(InputOptions options)
        {
            var handle = GetInputBuffer();
            uint read = 0;

            var readRef = new ReadRef();

            StringBuilder inputBuffer = new StringBuilder(options.InitialContent);

            int currentLength = 0;

            //Save current mode.
            uint currentMode = 0;
            ConsoleHostNativeMethods.GetConsoleMode(handle.DangerousGetHandle(), out currentMode);

            ConsoleModes newMode = (ConsoleModes)currentMode &
                            ~ConsoleModes.WindowInput &
                            ~ConsoleModes.MouseInput &
                            ~ConsoleModes.ProcessedInput;

            ConsoleHostNativeMethods.SetConsoleMode(handle.DangerousGetHandle(), (uint)newMode);
            while (true)
            {
                
                var inputs = DotConsoleNative.ReadConsoleKeys(handle, 1, out read);

                if(inputs != null && inputs.Length > 0)
                {
                    if (read == 1)
                    {
                        var input = inputs[0];

                        if (input.KeyEvent.repeatCount == 0)
                        {
                            //for some bizzare reson this can return a key or record
                            //but repeat count is zero so we should not process it in any way.
                            //This can happen if we're after mouse inputs such as mouse move.
                            continue;
                        }

                        if (input.KeyEvent.keyDown == true)
                        {
                            
                            if (input.EventType == (ushort)KeyEventType.KEY_EVENT)
                            {
                                if (input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Return)
                                {
                                    break;
                                }

                                if (input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Left)
                                {
                                    //set cursor position.
                                    //Should we use the cursor position from renderrer ? or perhaps use
                                    //the one from the output buffer of the console that we're using ?

                                    var currentCursorPosition = parent.GetCursorPosition();
                                    currentCursorPosition.X -= 1;

                                    parent.SetCursorPosition(currentCursorPosition);
                                    parent.SetBufferPosition(currentCursorPosition);
                                }
                                else if (input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Right)
                                {
                                    //set cursor position.
                                    //Should we use the cursor position from renderrer ? or perhaps use
                                    //the one from the output buffer of the console that we're using ?

                                    var currentCursorPosition = parent.GetCursorPosition();
                                    currentCursorPosition.X += 1;

                                    parent.SetCursorPosition(currentCursorPosition);
                                    parent.SetBufferPosition(currentCursorPosition);
                                }
                                else if(input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Back)
                                {
                                    var currentCursorPosition = parent.GetCursorPosition();
                                    currentCursorPosition.X -= 1;

                                    inputBuffer.Remove(inputBuffer.Length - 1, 1);
                                    if (options.NoEcho == false)
                                        parent.AlterLine(string.Empty, currentCursorPosition.Y, currentCursorPosition.X, 1, options.BackgroundColor, options.ForegroundColor);

                                    parent.SetCursorPosition(currentCursorPosition);
                                    parent.SetBufferPosition(currentCursorPosition);
                                }
                                else
                                {
                                    if(options.StopOnUpArrow && input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Up)
                                    {
                                        readRef.IsUpArrow = true;
                                        break;
                                    }
                                    else if(options.StopOnDownArrow && input.KeyEvent.virtualKeyCode == (ushort)VirtualInputCode.Down)
                                    {
                                        readRef.IsDownArrow = true;
                                        break;
                                    }

                                    if (options.StopChars != null)
                                    {
                                        if (options.StopChars.Contains(input.KeyEvent.unicodeChar))
                                        {
                                            readRef.StopChar = (input.KeyEvent.unicodeChar);
                                            break;
                                        }
                                    }

                                    currentLength++;

                                    inputBuffer.Append(input.KeyEvent.unicodeChar);
                                    if (options.NoEcho == false)
                                        parent.Write(input.KeyEvent.unicodeChar.ToString(), options.BackgroundColor, options.ForegroundColor, false);

                                    //Maximum allowed chars read, break the input loop.
                                    if (options.ReadLength.HasValue && currentLength >= options.ReadLength)
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            //Restore saved mode.
            ConsoleHostNativeMethods.SetConsoleMode(handle.DangerousGetHandle(), currentMode);
            
            readRef.ReadInput = inputBuffer.ToString();
            
            return readRef;
        }
    
        public void RegisterBreakHandler(BreakHandler handler)
        {
            ConsoleHostNativeMethods.SetConsoleCtrlHandler(handler, true);
        }

        /// <summary>
        /// Creates the input buffer.
        /// </summary>
        /// <returns></returns>
        private SafeFileHandle GetInputBuffer()
        {
            return inputBuffer.Value;
        }
    }
}
