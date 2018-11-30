using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class DebuggingLabel
    {
        Label label;
        int addr;
        List<int> refs;

        public DebuggingLabel(Label label)
        {
            this.label = label;
            addr = -1;
            refs = new List<int>();
        }

        public Label GetLabel() => label;

        public int GetAddr() => addr;

        public int SetAddr(int addr) => this.addr = addr;

        public List<int> GetRefs() => refs;

        public void AddRef(int addr)
        {
            refs.Add(addr);
        }
    }
}
