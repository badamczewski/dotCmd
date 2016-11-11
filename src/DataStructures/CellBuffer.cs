using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.DataStructures
{
    /// <summary>
    /// Cell buffer that's a wrapper arround a two dimenisional array of OutputCells.
    /// </summary>
    [DebuggerTypeProxy(typeof(CellBufferDebugView))]
    public class CellBuffer
    {
        public OutputCell[,] Cells { get; set; }

        public CellBuffer(int sizeOfY, int sizeOfX)
        {
            Cells = new OutputCell[sizeOfY, sizeOfX];
        }

        public OutputCell this[int y, int x]
        {
            get
            {
                return Cells[y, x];
            }
            set
            {
                Cells[y, x] = value;
            }
        }

        public int GetLengthOfX()
        {
            return Cells.GetLength(1);
        }

        public int GetLengthOfY()
        {
            return Cells.GetLength(0);
        }

        public int Length
        {
            get { return Cells.Length; }
        }

        /// <summary>
        /// This debug proxy makes it easier to debug any sort of off by one errors
        /// when persisiting a buffer to console host.
        /// </summary>
        internal class CellBufferDebugView
        {
            private CellBuffer buffer;

            public CellBufferDebugView(CellBuffer buffer)
            {
                this.buffer = buffer;
            }

            public string[] lines
            {
                get
                {
                    string[] result = new string[this.buffer.GetLengthOfY()];

                    for(int y = 0; y < this.buffer.GetLengthOfY(); y++)
                    {
                        for(int x = 0; x < this.buffer.GetLengthOfX(); x++)
                        {
                            result[y] += (char)this.buffer.Cells[y, x].Char;
                        }
                    }

                    return result;
                }
            }
        }
    }
}
