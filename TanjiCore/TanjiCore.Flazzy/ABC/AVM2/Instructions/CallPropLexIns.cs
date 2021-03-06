﻿using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class CallPropLexIns : ASInstruction
    {
        public ASMultiname PropertyName
        {
            get { return ABC.Pool.Multinames[PropertyNameIndex]; }
        }
        public int PropertyNameIndex { get; set; }

        /// <summary>
        /// Gets or sets the number of arguments present on the stack.
        /// </summary>
        public int ArgCount { get; set; }

        public CallPropLexIns(ABCFile abc)
            : base(OPCode.CallPropLex, abc)
        { }
        public CallPropLexIns(ABCFile abc, FlashReader input)
            : this(abc)
        {
            PropertyNameIndex = input.ReadInt30();
            ArgCount = input.ReadInt30();
        }
        public CallPropLexIns(ABCFile abc, int propertyNameIndex)
            : this(abc)
        {
            PropertyNameIndex = propertyNameIndex;
        }
        public CallPropLexIns(ABCFile abc, int propertyNameIndex, int argCount)
           : this(abc, propertyNameIndex)
        {
            ArgCount = argCount;
        }

        public override int GetPopCount()
        {
            return (ArgCount + ResolveMultinamePops(PropertyName) + 1);
        }
        public override int GetPushCount()
        {
            return 1;
        }
        public override void Execute(ASMachine machine)
        {
            for (int i = 0; i < ArgCount; i++)
            {
                machine.Values.Pop();
            }
            ResolveMultiname(machine, PropertyName);
            object obj = machine.Values.Pop();
            machine.Values.Push(null);
        }

        protected override void WriteValuesTo(FlashWriter output)
        {
            output.WriteInt30(PropertyNameIndex);
            output.WriteInt30(ArgCount);
        }
    }
}