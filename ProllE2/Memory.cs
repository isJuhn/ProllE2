using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class Memory
    {
        byte[] memory;

        public Memory(int size)
        {
            memory = new byte[size];
        }

        public int this[int addr]
        {
            get
            {
                return memory[addr];
            }

            set
            {
                memory[addr] = (byte)value;
            }
        }

        public byte[] GetUnderlyingArray()
        {
            return memory;
        }
    }
}
