using TanjiCore.Flazzy.IO;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class GetLocalIns : Local
    {
        public GetLocalIns(int register)
            : base(OPCode.GetLocal, register)
        { }
        public GetLocalIns(FlashReader input)
            : base(OPCode.GetLocal, input)
        { }
    }
}