using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotCmd.DataStructures
{
    public struct WriteRef
    {
        public WriteRef(int relativeRowIndex, int relativeColIndex, int WriteLength) : this()
        {
            this.RelativeColIndex = relativeColIndex;
            this.RelativeRowIndex = relativeRowIndex;
            this.WriteLength = WriteLength;
        }

        public int RelativeRowIndex { get; set; }
        public int RelativeColIndex { get; set; }
        public int WriteLength { get; set; }
    }
}
