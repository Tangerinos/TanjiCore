﻿using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class NewClassIns : ASInstruction
    {
        public ASClass Class
        {
            get { return ABC.Classes[ClassIndex]; }
        }
        public int ClassIndex { get; set; }

        public NewClassIns(ABCFile abc)
            : base(OPCode.NewClass, abc)
        { }
        public NewClassIns(ABCFile abc, FlashReader input)
            : this(abc)
        {
            ClassIndex = input.ReadInt30();
        }

        public override int GetPopCount()
        {
            return 1;
        }
        public override int GetPushCount()
        {
            return 1;
        }
        public override void Execute(ASMachine machine)
        {
            object baseType = machine.Values.Pop();
            machine.Values.Push(null);
        }

        protected override void WriteValuesTo(FlashWriter output)
        {
            output.WriteInt30(ClassIndex);
        }
    }
}