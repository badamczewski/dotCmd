using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.Native
{
    internal enum KeyEventType : ushort
    {
        KEY_EVENT = 0x0001,
        MOUSE_EVENT = 0x0002,
        WINDOW_BUFFER_SIZE_EVENT = 0x0004,
        MENU_EVENT = 0x0008,
        FOCUS_EVENT = 0x0010
    }
}
