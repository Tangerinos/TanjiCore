using TanjiCore.Flazzy.IO;
using TanjiCore.Flazzy.Records;

namespace TanjiCore.Flazzy.Tags
{
    public class SetBackgroundColorTag : TagItem
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public SetBackgroundColorTag()
            : base(TagKind.SetBackgroundColor)
        { }
        public SetBackgroundColorTag(HeaderRecord header, FlashReader input)
            : base(header)
        {
            R = input.ReadByte();
            G = input.ReadByte();
            B = input.ReadByte();
        }

        public override int GetBodySize()
        {
            int size = 0;
            size += sizeof(byte);
            size += sizeof(byte);
            size += sizeof(byte);
            return size;
        }

        protected override void WriteBodyTo(FlashWriter output)
        {
            output.Write(new byte[] { R, G, B });
        }
    }
}