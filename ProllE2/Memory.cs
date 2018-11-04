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

                // todo: check this in IL after every memory write
                if (addr == 254 && value != 0)
                {
                    Console.WriteLine(memory[255]);
                    memory[254] = 0;
                }
            }
        }

        public byte[] GetUnderlyingArray()
        {
            return memory;
        }
    }
}
