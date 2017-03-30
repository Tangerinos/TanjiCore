using System;

using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class IfGreaterEqualIns : Jumper
    {
        public IfGreaterEqualIns()
            : base(OPCode.IfGe)
        { }
        public IfGreaterEqualIns(FlashReader input)
            : base(OPCode.IfGe, input)
        { }

        public override bool? RunCondition(ASMachine machine)
        {
            var right = machine.Values.Pop();
            var left = machine.Values.Pop();
            if (left == null || right == null) return null;

            return (Convert.ToDouble(left) >= Convert.ToDouble(right));
        }
    }
}