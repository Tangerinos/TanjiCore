﻿using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class GetScopeObjectIns : ASInstruction
    {
        public byte ScopeIndex { get; set; }

        public GetScopeObjectIns()
            : base(OPCode.GetScopeObject)
        { }
        public GetScopeObjectIns(FlashReader input)
            : this()
        {
            ScopeIndex = input.ReadByte();
        }

        public override int GetPushCount()
        {
            return 1;
        }
        public override void Execute(ASMachine machine)
        {
            machine.Values.Push(null);
        }

        protected override void WriteValuesTo(FlashWriter output)
        {
            output.Write(ScopeIndex);
        }
    }
}