using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class SetLocalIns : Local
    {
        public SetLocalIns(int register)
            : base(OPCode.SetLocal, register)
        { }
        public SetLocalIns(FlashReader input)
            : base(OPCode.SetLocal, input)
        { }
    }
}