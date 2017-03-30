using System;

namespace TanjiCore.Flazzy.ABC.AVM2.Instructions
{
    public class PushTrueIns : Primitive
    {
        public override object Value
        {
            get { return true; }
            set { throw new NotSupportedException(); }
        }

        public PushTrueIns()
            : base(OPCode.PushTrue)
        { }
    }
}