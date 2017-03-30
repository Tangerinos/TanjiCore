using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class JumpIns : Jumper
    {
        public JumpIns()
            : base(OPCode.Jump)
        { }
        public JumpIns(FlashReader input)
            : base(OPCode.Jump, input)
        { }

        public override bool? RunCondition(ASMachine machine)
        {
            return true;
        }
    }
}