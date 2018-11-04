using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class Flags
    {
        bool[] flags;

        public Flags()
        {
            flags = new bool[4];
        }

        public bool Equal { get { return flags[0]; } set { flags[0] = value; } }

        public bool Greater { get { return flags[1]; } set { flags[1] = value; } }

        public bool Less { get { return flags[2]; } set { flags[2] = value; } }

        public bool Halt { get { return flags[3]; } set { flags[3] = value; } }

        public bool[] GetUnderlyingArray()
        {
            return flags;
        }
    }
}
