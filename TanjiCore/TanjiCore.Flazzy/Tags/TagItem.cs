using TanjiCore.Flazzy.IO;
using TanjiCore.Flazzy.Records;

namespace TanjiCore.Flazzy.Tags
{
    public abstract class TagItem : FlashItem
    {
        public TagKind Kind
        {
            get { return Header.Kind; }
        }
        public HeaderRecord Header { get; }

        protected override string DebuggerDisplay
        {
            get { return Kind.ToString(); }
        }

        public TagItem(TagKind kind)
            : this(new HeaderRecord(kind))
        { }
        public TagItem(HeaderRecord header)
        {
            Header = header;
        }

        public abstract int GetBodySize();

        public override void WriteTo(FlashWriter output)
        {
            Header.Length = GetBodySize();
            Header.WriteTo(output);
            WriteBodyTo(output);
        }
        protected virtual void WriteBodyTo(FlashWriter output)
        { }
    }
}