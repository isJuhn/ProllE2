using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;

namespace ProllE2
{
    class DebuggingILGenerator
    {
        ILGenerator ILGen;
        TextWriter textWriter;
        List<string> instructions;
        List<DebuggingLabel> labels;
        
        public DebuggingILGenerator(ILGenerator ILGen, TextWriter textWriter)
        {
            this.ILGen = ILGen;
            this.textWriter = textWriter;
            instructions = new List<string>();
            labels = new List<DebuggingLabel>();
        }

        public void PrintAll()
        {
            foreach (var inst in instructions)
            {
                textWriter.WriteLine(inst);
            }
        }

        private void AddOpString(OpCode opcode, string arg = "")
        {
            instructions.Add(ILGen.ILOffset.ToString("X") + ": " + opcode.Name + " " + arg);
        }

        public void Emit(OpCode opcode)
        {
            AddOpString(opcode);
            ILGen.Emit(opcode);
        }

        public void Emit(OpCode opcode, byte arg)
        {
            AddOpString(opcode, arg.ToString("X"));
            ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, int arg)
        {
            AddOpString(opcode, arg.ToString("X"));
            ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
            AddOpString(opcode, meth.Name);
            ILGen.Emit(opcode, meth);
        }

        public void Emit(OpCode opcode, Label label)
        {
            DebuggingLabel dbgLabel = labels.Find(e => e.GetLabel() == label);
            dbgLabel.AddRef(instructions.Count);
            AddOpString(opcode, dbgLabel.GetAddr() != -1 ? "label_" + dbgLabel.GetAddr().ToString("X") : "label_");
            ILGen.Emit(opcode, label);
        }

        public Label DefineLabel()
        {
            Label label = ILGen.DefineLabel();
            labels.Add(new DebuggingLabel(label));
            return label;
        }

        public void MarkLabel(Label label)
        {
            instructions.Add("label_" + ILGen.ILOffset.ToString("X"));

            DebuggingLabel dbgLabel = labels.Find(e => e.GetLabel() == label);
            dbgLabel.SetAddr(ILGen.ILOffset);
            foreach (int refAddr in dbgLabel.GetRefs())
            {
                instructions[refAddr] += dbgLabel.GetAddr().ToString("X");
            }

            ILGen.MarkLabel(label);
        }
    }
}
