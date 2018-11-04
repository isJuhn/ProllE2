using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class Registers
    {
        byte[] regs;
        bool jumped;

        public Registers()
        {
            regs = new byte[8];
            jumped = false;
        }

        public int this[int index]
        {
            get
            {
                return regs[index];
            }

            set
            {
                regs[index] = (byte)value;
                jumped = index == 0 ? true : false;
            }
        }

        public void IncrementPC()
        {
            regs[0] += 3;
        }

        public bool GetResetJumped()
        {
            bool ret = jumped;
            jumped = false;
            return ret;
        }

        public byte[] GetUnderlyingArray()
        {
            return regs;
        }
    }
}
