using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class   Interpreter : Cpu
    {
        delegate bool OpcodeDelegate(Opcode opcode);
        OpcodeDelegate[] opcodeFunctions;

        public Interpreter()
            : base()
        {
            opcodeFunctions = new OpcodeDelegate[] { Nop, Add, Sub, Mul, Div, Not, Or, And, Xor, Je, Jne, Jg, Jl, Jmp, Mov, Cmp, };
        }

        public override bool Setup() => true;

        public override bool Run()
        {
            while (!flags.Halt)
            {
                Opcode opcode = new Opcode(GetInstruction(registers[0]));

                if (!Execute(opcode))
                {
                    return false;
                }
                 //todo: start using again
                if ((memory[254]) != 0)
                {
                    Console.WriteLine(memory[255]);
                    memory[254] = 0;
                }

                if (registers[0] > 250)
                {
                    flags.Halt = true;
                }

                if (!registers.GetResetJumped())
                {
                    registers.IncrementPC();
                }
            }

            return true;
        }

        public bool Execute(Opcode opcode)
        {
#if DEBUG
            // todo: support arbitrary TextWriter
            Console.WriteLine($"Interpreter: {registers[0]}, {opcode.ToString()}");
#endif
            return opcodeFunctions[(int)opcode.GetOp()](opcode);
        }

        public int GetValue(AddressingMode addrMode, int source)
        {
            int ret = 0;
            switch (addrMode)
            {
                case AddressingMode.reg:
                    ret = registers[source];
                    break;
                case AddressingMode.memory:
                    ret = memory[source];
                    break;
                case AddressingMode.immediate:
                    ret = source;
                    break;
                case AddressingMode.memAtReg:
                    ret = memory[registers[source]];
                    break;
            }
            return ret;
        }

        public int GetSource(Opcode opcode)
        {
            return GetValue(opcode.GetSourceAddrMode(), opcode.GetSourceValue());
        }

        public int GetDest(Opcode opcode)
        {
            return GetValue(opcode.GetDestAddrMode(), opcode.GetDestValue());
        }

        public bool SetValue(AddressingMode addrMode, int dest, int value)
        {
            switch (addrMode)
            {
                case AddressingMode.reg:
                    registers[dest] = value;
                    break;
                case AddressingMode.memory:
                    memory[dest] = value;
                    break;
                case AddressingMode.immediate:
                    return false;
                case AddressingMode.memAtReg:
                    memory[registers[dest]] = value;
                    break;
            }
            return true;
        }

        public bool SetDest(Opcode opcode, int value)
        {
            return SetValue(opcode.GetDestAddrMode(), opcode.GetDestValue(), value);
        }

        public bool JumpIf(int dest, bool condition)
        {
            if (condition)
            {
                registers[0] = dest;
            }
            return true;
        }

        private bool Nop(Opcode opcode)
        {
            return true;
        }

        private bool Add(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) + GetSource(opcode));
        }

        private bool Sub(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) - GetSource(opcode));
        }

        private bool Mul(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) * GetSource(opcode));
        }

        private bool Div(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) / GetSource(opcode));
        }

        private bool Not(Opcode opcode)
        {
            return SetDest(opcode, ~GetDest(opcode));
        }

        private bool Or(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) | GetSource(opcode));
        }

        private bool And(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) & GetSource(opcode));
        }

        private bool Xor(Opcode opcode)
        {
            return SetDest(opcode, GetDest(opcode) ^ GetSource(opcode));
        }

        private bool Je(Opcode opcode)
        {
            return JumpIf(GetDest(opcode), flags.Equal);
        }

        private bool Jne(Opcode opcode)
        {
            return JumpIf(GetDest(opcode), !flags.Equal);
        }

        private bool Jg(Opcode opcode)
        {
            return JumpIf(GetDest(opcode), flags.Greater);
        }

        private bool Jl(Opcode opcode)
        {
            return JumpIf(GetDest(opcode), flags.Less);
        }

        private bool Jmp(Opcode opcode)
        {
            return JumpIf(GetDest(opcode), true);
        }

        private bool Mov(Opcode opcode)
        {
            return SetDest(opcode, GetSource(opcode));
        }

        private bool Cmp(Opcode opcode)
        {
            int value1 = GetDest(opcode);
            int value2 = GetSource(opcode);
            flags.Equal = value1 == value2;
            flags.Greater = value1 > value2;
            flags.Less = value1 < value2;
            return true;
        }
    }
}
