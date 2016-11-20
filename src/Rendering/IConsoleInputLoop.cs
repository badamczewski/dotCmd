using dotCmd.DataStructures;
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
    public interface IConsoleInputLoop
    {
        /// <summary>
        /// Reads the input from the input buffer.
        /// </summary>
        ReadRef ReadInput(InputOptions options);
    }
}
