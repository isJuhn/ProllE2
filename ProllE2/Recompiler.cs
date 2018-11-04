using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProllE2
{
    class Recompiler : Cpu
    {
        delegate bool OpcodeDelegate(Opcode opcode);
        delegate bool RecompiledMethodDelegate(Memory memory, Registers registers, Flags flags);
        OpcodeDelegate[] opcodeFunctions;
        RecompiledMethodDelegate recompiledMethod;
        ILGenerator ILGen;

        // todo: remove
        MethodInfo regsGetMI = typeof(Registers).GetMethod("get_Item", new Type[] { typeof(int) });
        MethodInfo regsSetMI = typeof(Registers).GetMethod("set_Item", new Type[] { typeof(int), typeof(int) });
        MethodInfo memoryGetMI = typeof(Memory).GetMethod("get_Item", new Type[] { typeof(int) });
        MethodInfo memorySetMI = typeof(Memory).GetMethod("set_Item", new Type[] { typeof(int), typeof(int) });
        MethodInfo flagsGetEqualMI = typeof(Flags).GetMethod("get_Equal");
        MethodInfo flagsSetEqualMI = typeof(Flags).GetMethod("set_Equal", new Type[] { typeof(bool) });
        MethodInfo flagsGetGreaterMI = typeof(Flags).GetMethod("get_Greater");
        MethodInfo flagsSetGreaterMI = typeof(Flags).GetMethod("set_Greater", new Type[] { typeof(bool) });
        MethodInfo flagsGetLessMI = typeof(Flags).GetMethod("get_Less");
        MethodInfo flagsSetLessMI = typeof(Flags).GetMethod("set_Less", new Type[] { typeof(bool) });

        // guest addr -> label map
        Dictionary<int, Label> addrToLabel;

        // limitations: instructions aligned by 3, no self-modifying code, only static branching, no writing to r0 for jumps
        public Recompiler() : base()
        {
            opcodeFunctions = new OpcodeDelegate[] { Nop, Add, Sub, Mul, Div, Not, Or, And, Xor, Je, Jne, Jg, Jl, Jmp, Mov, Cmp, };
        }

        public override bool Setup()
        {
            return Recompile();
        }

        public override bool Run()
        {
            // todo: use the underlying array instead of these wrappers
            return recompiledMethod(memory, registers, flags);
        }

        public bool Recompile()
        {
            // guest addr -> opcode map
            Dictionary<int, Opcode> opcodes = new Dictionary<int, Opcode>();
            addrToLabel = new Dictionary<int, Label>();
            DynamicMethod method = new DynamicMethod("method", typeof(bool), new Type[] { typeof(Memory), typeof(Registers), typeof(Flags) }, typeof(Recompiler).Module);
            ILGen = method.GetILGenerator();

            // look for labels and add instructions
            for (int i = 0; i < 250; i += 3)
            {
                Opcode opcode = new Opcode(GetInstruction(i));
                Op op = opcode.GetOp();
                if (op == Op.je || op == Op.jg || op == Op.jl || op == Op.jmp || op == Op.jne)
                {
                    if (opcode.GetDestAddrMode() != AddressingMode.immediate)
                    {
                        return false;
                    }

                    addrToLabel[opcode.GetDestValue()] = ILGen.DefineLabel();
                }

                opcodes[i] = opcode;
            }

            foreach (var opcodeTuple in opcodes)
            {
                // mark label
                if (addrToLabel.ContainsKey(opcodeTuple.Key))
                {
                    var label = addrToLabel[opcodeTuple.Key];
                    ILGen.MarkLabel(label);
                    addrToLabel[opcodeTuple.Key] = label;
                }

                // todo: add logging for debug build
                // recompile instruction
                if (!opcodeFunctions[(int)opcodeTuple.Value.GetOp()](opcodeTuple.Value))
                {
                    return false;
                }
            }

            // mark lables outside the executable area
            foreach (var addrLabel in addrToLabel.Where(kv => kv.Key > 249))
            {
                ILGen.MarkLabel(addrLabel.Value);
            }

            //return true
            ILGen.Emit(OpCodes.Ldc_I4_S, (byte)1);
            ILGen.Emit(OpCodes.Ret);

            recompiledMethod = (RecompiledMethodDelegate)method.CreateDelegate(typeof(RecompiledMethodDelegate));

            return true;
        }

        private bool GetValue(AddressingMode addrMode, int source)
        {
            switch (addrMode)
            {
                case AddressingMode.reg:
                    ILGen.Emit(OpCodes.Ldarg_1);
                    ILGen.Emit(OpCodes.Ldc_I4, source);
                    ILGen.Emit(OpCodes.Callvirt, regsGetMI);
                    break;
                case AddressingMode.memory:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    ILGen.Emit(OpCodes.Ldc_I4, source);
                    ILGen.Emit(OpCodes.Callvirt, memoryGetMI);
                    break;
                case AddressingMode.immediate:
                    ILGen.Emit(OpCodes.Ldc_I4, source);
                    break;
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    ILGen.Emit(OpCodes.Ldarg_1);
                    ILGen.Emit(OpCodes.Ldc_I4, source);
                    ILGen.Emit(OpCodes.Callvirt, regsGetMI);
                    ILGen.Emit(OpCodes.Callvirt, memoryGetMI);
                    break;
            }
            return true;
        }

        private bool GetSourceValue(Opcode opcode)
        {
            return GetValue(opcode.GetSourceAddrMode(), opcode.GetSourceValue());
        }

        private bool GetDestValue(Opcode opcode)
        {
            return GetValue(opcode.GetDestAddrMode(), opcode.GetDestValue());
        }

        private bool LoadDest(Opcode opcode)
        {
            switch (opcode.GetDestAddrMode())
            {
                case AddressingMode.reg:
                    ILGen.Emit(OpCodes.Ldarg_1);
                    ILGen.Emit(OpCodes.Ldc_I4, opcode.GetDestValue());
                    break;
                case AddressingMode.memory:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    ILGen.Emit(OpCodes.Ldc_I4, opcode.GetDestValue());
                    break;
                case AddressingMode.immediate:
                    return false;
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Ldarg_1);
                    ILGen.Emit(OpCodes.Ldc_I4, opcode.GetDestValue());
                    ILGen.Emit(OpCodes.Callvirt, regsGetMI);
                    break;
            }
            return true;
        }

        private bool SetValue(Opcode opcode)
        {
            switch (opcode.GetDestAddrMode())
            {
                case AddressingMode.reg:
                    ILGen.Emit(OpCodes.Callvirt, regsSetMI);
                    break;
                case AddressingMode.memory:
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Callvirt, memorySetMI);
                    break;
                case AddressingMode.immediate:
                    return false;
            }
            return true;
        }

        private bool ArithmeticOperation(OpCode op, Opcode opcode)
        {
            LoadDest(opcode);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(op);
            SetValue(opcode);
            return true;
        }

        // todo: branch operation function, see ArithmeticOperation

        private bool Nop(Opcode opcode)
        {
            return true;
        }

        private bool Add(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Add, opcode);
        }

        private bool Sub(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Sub, opcode);
        }

        private bool Mul(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Mul, opcode);
        }

        private bool Div(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Div, opcode);
        }

        private bool Not(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Not, opcode);
        }

        private bool Or(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Or, opcode);
        }

        private bool And(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.And, opcode);
        }

        private bool Xor(Opcode opcode)
        {
            return ArithmeticOperation(OpCodes.Xor, opcode);
        }

        private bool Je(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Callvirt, flagsGetEqualMI);
            ILGen.Emit(OpCodes.Brtrue, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Jne(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Callvirt, flagsGetEqualMI);
            ILGen.Emit(OpCodes.Brfalse, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Jg(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Callvirt, flagsGetGreaterMI);
            ILGen.Emit(OpCodes.Brtrue, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Jl(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Callvirt, flagsGetLessMI);
            ILGen.Emit(OpCodes.Brtrue, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Jmp(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Br, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Mov(Opcode opcode)
        {
            LoadDest(opcode);
            GetSourceValue(opcode);
            SetValue(opcode);
            return true;
        }

        private bool Cmp(Opcode opcode)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Dup);
            ILGen.Emit(OpCodes.Dup);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Ceq);
            ILGen.Emit(OpCodes.Callvirt, flagsSetEqualMI);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Cgt);
            ILGen.Emit(OpCodes.Callvirt, flagsSetGreaterMI);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Clt);
            ILGen.Emit(OpCodes.Callvirt, flagsSetLessMI);
            return true;
        }
    }
}
