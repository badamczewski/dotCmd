using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Native
{
    [Flags]
    internal enum ConsoleModes : uint
    {
        //Input Modes
        ProcessedInput = 0x001,
        LineInput = 0x002,
        EchoInput = 0x004,
        WindowInput = 0x008,
        MouseInput = 0x010,
        Insert = 0x020,
        QuickEdit = 0x040,
        Extended = 0x080,
        AutoPosition = 0x100,
        // Output modes
        ProcessedOutput = 0x001,
        WrapEndOfLine = 0x002,
        VirtualTerminal = 0x004,
        NewLineAutoReturn = 0x008,
        GridWorldWide = 0x010
    }
}
