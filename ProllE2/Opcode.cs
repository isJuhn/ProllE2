using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class Opcode
    {
        int data;

        public Opcode()
        {
            data = 0;
        }

        public Opcode(int data)
        {
            this.data = data;
        }

        public Op GetOp() => (Op)(data >> 20);

        public AddressingMode GetDestAddrMode() => (AddressingMode)((data >> 18) & 0x3);

        public AddressingMode GetSourceAddrMode() => (AddressingMode)((data >> 8) & 0x3);

        public int GetDestValue() => (data >> 10) & 0xff;

        public int GetSourceValue() => data & 0xff;

        public override string ToString()
        {
            return $"{GetOp().ToString()}, {(int)GetDestAddrMode()}, {GetDestValue()}, {(int)GetSourceAddrMode()}, {GetSourceValue()}";
        }
    }

    enum Op
    {
        nop = 0x0,
        add = 0x1,
        sub = 0x2,
        mul = 0x3,
        div = 0x4,
        not = 0x5,
        or  = 0x6,
        and = 0x7,
        xor = 0x8,
        je  = 0x9,
        jne = 0xa,
        jg  = 0xb,
        jl  = 0xc,
        jmp = 0xd,
        mov = 0xe,
        cmp = 0xf,
    }

    enum AddressingMode
    {
        reg = 0x0,
        memory = 0x1,
        immediate = 0x2,
        memAtReg = 0x3,
    }
}
