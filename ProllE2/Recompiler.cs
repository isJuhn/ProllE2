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
        delegate bool RecompiledMethodDelegate(byte[] memory, byte[] registers, bool[] flags);
        OpcodeDelegate[] opcodeFunctions;
        RecompiledMethodDelegate recompiledMethod;

#if DEBUG
        DebuggingILGenerator ILGen;
#else
        ILGenerator ILGen;
#endif

        MethodInfo writeLineMI = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) });

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
            return recompiledMethod(memory.GetUnderlyingArray(), registers.GetUnderlyingArray(), flags.GetUnderlyingArray());
        }

        public bool Recompile()
        {
            // guest addr -> opcode map
            Dictionary<int, Opcode> opcodes = new Dictionary<int, Opcode>();
            addrToLabel = new Dictionary<int, Label>();
            DynamicMethod method = new DynamicMethod("method", typeof(bool), new Type[] { typeof(byte[]), typeof(byte[]), typeof(bool[]) }, typeof(Recompiler).Module);

#if DEBUG
            ILGen = new DebuggingILGenerator(method.GetILGenerator(), Console.Out);
#else
            ILGen = method.GetILGenerator();
#endif

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
                    Label label = addrToLabel[opcodeTuple.Key];
                    ILGen.MarkLabel(label);
                    addrToLabel[opcodeTuple.Key] = label;
                }

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
            ILGen.Emit(OpCodes.Ldc_I4_1);
            ILGen.Emit(OpCodes.Ret);

            recompiledMethod = (RecompiledMethodDelegate)method.CreateDelegate(typeof(RecompiledMethodDelegate));

#if DEBUG
            ILGen.PrintAll();
#endif

            return true;
        }

        private bool EmitLdc_I4(int value)
        {
            switch (value)
            {
                case 0:
                    ILGen.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    ILGen.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    ILGen.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    ILGen.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    ILGen.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    ILGen.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    ILGen.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    ILGen.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    ILGen.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    ILGen.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
            return true;
        }

        private bool PrintCheck()
        {
            Label label = ILGen.DefineLabel();

            // if (memory[0xfe] != 0)
            ILGen.Emit(OpCodes.Ldarg_0);
            ILGen.Emit(OpCodes.Ldc_I4, 0xfe);
            ILGen.Emit(OpCodes.Ldelem_U1);
            ILGen.Emit(OpCodes.Brfalse_S, label);

            // Console.WriteLine(memory[0xff]);
            ILGen.Emit(OpCodes.Ldarg_0);
            ILGen.Emit(OpCodes.Ldc_I4, 0xff);
            ILGen.Emit(OpCodes.Ldelem_U1);
            ILGen.Emit(OpCodes.Call, writeLineMI);

            // memory[0xfe] = 0;
            ILGen.Emit(OpCodes.Ldarg_0);
            ILGen.Emit(OpCodes.Ldc_I4, 0xfe);
            ILGen.Emit(OpCodes.Ldc_I4_0);
            ILGen.Emit(OpCodes.Stelem_I1);

            ILGen.MarkLabel(label);
            return true;
        }

        private bool GetValue(AddressingMode addrMode, int source)
        {
            switch (addrMode)
            {
                case AddressingMode.reg:
                    ILGen.Emit(OpCodes.Ldarg_1);
                    EmitLdc_I4(source);
                    ILGen.Emit(OpCodes.Ldelem_U1);
                    break;
                case AddressingMode.memory:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    EmitLdc_I4(source);
                    ILGen.Emit(OpCodes.Ldelem_U1);
                    break;
                case AddressingMode.immediate:
                    EmitLdc_I4(source);
                    break;
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    ILGen.Emit(OpCodes.Ldarg_1);
                    EmitLdc_I4(source);
                    ILGen.Emit(OpCodes.Ldelem_U1);
                    ILGen.Emit(OpCodes.Ldelem_U1);
                    break;
                default:
                    return false;
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
                    EmitLdc_I4(opcode.GetDestValue());
                    break;
                case AddressingMode.memory:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    EmitLdc_I4(opcode.GetDestValue());
                    break;
                case AddressingMode.immediate:
                    return false;
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Ldarg_0);
                    ILGen.Emit(OpCodes.Ldarg_1);
                    EmitLdc_I4(opcode.GetDestValue());
                    ILGen.Emit(OpCodes.Ldelem_U1);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool SetValue(Opcode opcode)
        {
            switch (opcode.GetDestAddrMode())
            {
                case AddressingMode.reg:
                    ILGen.Emit(OpCodes.Stelem_I1);
                    break;
                case AddressingMode.memory:
                case AddressingMode.memAtReg:
                    ILGen.Emit(OpCodes.Stelem_I1);
                    if (opcode.GetSourceAddrMode() == AddressingMode.immediate && opcode.GetSourceValue() != 0xFE)
                    {
                        break;
                    }
                    return PrintCheck();
                case AddressingMode.immediate:
                default:
                    return false;
            }
            return true;
        }

        private bool ArithmeticOperation(Opcode opcode, OpCode arithmeticOp)
        {
            LoadDest(opcode);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(arithmeticOp);
            SetValue(opcode);
            return true;
        }

        private bool BranchOperation(Opcode opcode, OpCode branchOp, int flagsIndex)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Ldc_I4_S, flagsIndex);
            ILGen.Emit(OpCodes.Ldelem_U1);
            ILGen.Emit(branchOp, addrToLabel[opcode.GetDestValue()]);
            return true;
        }

        private bool Nop(Opcode opcode)
        {
            return true;
        }

        private bool Add(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Add);
        }

        private bool Sub(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Sub);
        }

        private bool Mul(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Mul);
        }

        private bool Div(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Div);
        }

        private bool Not(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Not);
        }

        private bool Or(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Or);
        }

        private bool And(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.And);
        }

        private bool Xor(Opcode opcode)
        {
            return ArithmeticOperation(opcode, OpCodes.Xor);
        }

        private bool Je(Opcode opcode)
        {
            return BranchOperation(opcode, OpCodes.Brtrue, 0);
        }

        private bool Jne(Opcode opcode)
        {
            return BranchOperation(opcode, OpCodes.Brfalse, 0);
        }

        private bool Jg(Opcode opcode)
        {
            return BranchOperation(opcode, OpCodes.Brtrue, 1);
        }

        private bool Jl(Opcode opcode)
        {
            return BranchOperation(opcode, OpCodes.Brtrue, 2);
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
            ILGen.Emit(OpCodes.Ldc_I4_0);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Ceq);
            ILGen.Emit(OpCodes.Stelem_I1);

            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Ldc_I4_1);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Cgt);
            ILGen.Emit(OpCodes.Stelem_I1);

            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Ldc_I4_2);
            GetDestValue(opcode);
            GetSourceValue(opcode);
            ILGen.Emit(OpCodes.Clt);
            ILGen.Emit(OpCodes.Stelem_I1);
            return true;
        }
    }
}
