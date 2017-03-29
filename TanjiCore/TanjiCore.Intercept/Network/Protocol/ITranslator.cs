namespace TanjiCore.Intercept.Network.Protocol
{
    public interface ITranslator
    {
        bool IsTranslatingPrimitives { get; }

        string Print(string field);
        byte[] Translate(string type, string value);
    }
}