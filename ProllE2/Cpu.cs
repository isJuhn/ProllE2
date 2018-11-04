using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    abstract class Cpu
    {
        protected Flags flags;
        protected Registers registers;
        protected Memory memory;

        public Cpu()
        {
            flags = new Flags();
            registers = new Registers();
            memory = new Memory(256);
        }

        public abstract bool Setup();

        public abstract bool Run();

        public int GetInstruction(int addr)
        {
            return (memory[addr] << 16) + (memory[addr + 1] << 8) + memory[addr + 2];
        }

        public bool LoadProgram(string path)
        {
            string[] instructions = System.IO.File.ReadAllLines(path);
            for (int i = 0; i < instructions.Length; i++)
            {
                memory[i * 3] = byte.Parse(instructions[i].Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                memory[i * 3 + 1] = byte.Parse(instructions[i].Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                memory[i * 3 + 2] = byte.Parse(instructions[i].Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return Setup();
        }
    }
}
