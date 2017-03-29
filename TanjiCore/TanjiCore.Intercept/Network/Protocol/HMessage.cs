using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TanjiCore.Intercept.Network.Protocol
{
    [DebuggerDisplay("Header: {Header}, Length: {Length} | {ToString()}")]
    public class HMessage
    {
        private static readonly Regex _paramGrabber;
        private static readonly Regex _fieldGrabber;

        private readonly List<byte> _body;

        private string _toStringCache;
        private byte[] _toBytesCache, _bodyBuffer;

        private int _position;
        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private ushort _header;
        public ushort Header
        {
            get { return _header; }
            set
            {
                if (!IsCorrupted && _header != value)
                {
                    _header = value;
                    ResetCache();
                }
            }
        }

        public int Readable => GetReadableBytes(Position);
        public int Length => (_body.Count + (!IsCorrupted ? 2 : 0));

        public bool IsCorrupted { get; }

        private readonly List<object> _read;
        public IReadOnlyList<object> ValuesRead { get; }

        private readonly List<object> _written;
        public IReadOnlyList<object> ValuesWritten { get; }

        static HMessage()
        {
            _fieldGrabber = new Regex(@"%(?<field>.*?)%", RegexOptions.IgnoreCase);
            _paramGrabber = new Regex(@"{(?<type>s|i|b|d|u|\$(?<special>.*?)):(?<value>[^}]*)\}", RegexOptions.IgnoreCase);
        }
        private HMessage()
        {
            _body = new List<byte>();
            _read = new List<object>();
            _written = new List<object>();

            ValuesRead = _read.AsReadOnly();
            ValuesWritten = _written.AsReadOnly();
        }
        
        public HMessage(byte[] data)
            : this()
        {
            IsCorrupted = (data.Length < 6 ||
                (BigEndian.ToInt32(data, 0) != data.Length - 4));

            if (!IsCorrupted)
            {
                Header = BigEndian.ToUInt16(data, 4);

                _bodyBuffer = new byte[data.Length - 6];
                Buffer.BlockCopy(data, 6, _bodyBuffer, 0, data.Length - 6);
            }
            else _bodyBuffer = data;
            _body.AddRange(_bodyBuffer);
        }
        public HMessage(string value)
            : this(ToBytes(value))
        { }
        public HMessage(ushort header, params object[] values)
            : this(Construct(header, values))
        {
            _written.AddRange(values);
        }

        #region Read Methods
        public int ReadInteger()
        {
            return ReadInteger(ref _position);
        }
        public int ReadInteger(int position)
        {
            return ReadInteger(ref position);
        }
        public int ReadInteger(ref int position)
        {
            int value = BigEndian.ToInt32(_bodyBuffer, position);
            position += 4;

            _read.Add(value);
            return value;
        }

        public double ReadDouble()
        {
            return ReadDouble(ref _position);
        }
        public double ReadDouble(int position)
        {
            return ReadDouble(ref position);
        }
        public double ReadDouble(ref int position)
        {
            double value = BigEndian.ToDouble(_bodyBuffer, position);
            position += 8;

            _read.Add(value);
            return value;
        }

        public ushort ReadShort()
        {
            return ReadShort(ref _position);
        }
        public ushort ReadShort(int position)
        {
            return ReadShort(ref position);
        }
        public ushort ReadShort(ref int position)
        {
            ushort value = BigEndian.ToUInt16(_bodyBuffer, position);
            position += 2;

            _read.Add(value);
            return value;
        }

        public bool ReadBoolean()
        {
            return ReadBoolean(ref _position);
        }
        public bool ReadBoolean(int position)
        {
            return ReadBoolean(ref position);
        }
        public bool ReadBoolean(ref int position)
        {
            bool value = BigEndian.ToBoolean(_bodyBuffer, position);
            position += 1;

            _read.Add(value);
            return value;
        }

        public string ReadString()
        {
            return ReadString(ref _position);
        }
        public string ReadString(int position)
        {
            return ReadString(ref position);
        }
        public string ReadString(ref int position)
        {
            string value = BigEndian.ToString(_bodyBuffer, position);
            position += BigEndian.GetSize(value);

            _read.Add(value);
            return value;
        }

        public byte[] ReadBytes(int length)
        {
            return ReadBytes(length, ref _position);
        }
        public byte[] ReadBytes(int length, int position)
        {
            return ReadBytes(length, ref position);
        }
        public byte[] ReadBytes(int length, ref int position)
        {
            var value = new byte[length];
            Buffer.BlockCopy(_bodyBuffer, position, value, 0, length);
            position += length;

            _read.Add(value);
            return value;
        }
        #endregion
        #region Write Methods
        public void WriteInteger(int value)
        {
            WriteInteger(value, _body.Count);
        }
        public void WriteInteger(int value, int position)
        {
            byte[] encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }
        public void WriteDouble(double value)
        {
            WriteDouble(value, _body.Count);
        }
        public void WriteDouble(double value, int position)
        {
            byte[] encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }
        public void WriteShort(ushort value)
        {
            WriteShort(value, _body.Count);
        }
        public void WriteShort(ushort value, int position)
        {
            byte[] encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteBoolean(bool value)
        {
            WriteBoolean(value, _body.Count);
        }
        public void WriteBoolean(bool value, int position)
        {
            byte[] encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteString(string value)
        {
            WriteString(value, _body.Count);
        }
        public void WriteString(string value, int position)
        {
            if (value == null)
                value = string.Empty;

            byte[] encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteBytes(byte[] value)
        {
            WriteBytes(value, _body.Count);
        }
        public void WriteBytes(byte[] value, int position)
        {
            WriteObject(value, value, position);
        }

        private void WriteObjects(params object[] values)
        {
            _written.AddRange(values);
            _body.AddRange(GetBytes(values));

            Refresh();
        }
        private void WriteObject(byte[] encoded, object value, int position)
        {
            _written.Add(value);
            _body.InsertRange(position, encoded);

            Refresh();
        }
        #endregion
        #region Remove Methods
        public void RemoveInteger()
        {
            RemoveInteger(_position);
        }
        public void RemoveInteger(int position)
        {
            RemoveBytes(4, position);
        }

        public void RemoveShort()
        {
            RemoveShort(_position);
        }
        public void RemoveShort(int position)
        {
            RemoveBytes(2, position);
        }

        public void RemoveBoolean()
        {
            RemoveBoolean(_position);
        }
        public void RemoveBoolean(int position)
        {
            RemoveBytes(1, position);
        }

        public void RemoveString()
        {
            RemoveString(_position);
        }
        public void RemoveString(int position)
        {
            int readable = (_body.Count - position);
            if (readable < 2) return;

            ushort stringLength =
                BigEndian.ToUInt16(_bodyBuffer, position);

            if (readable >= (stringLength + 2))
                RemoveBytes(stringLength + 2, position);
        }

        public void RemoveBytes(int length)
        {
            RemoveBytes(length, _position);
        }
        public void RemoveBytes(int length, int position)
        {
            _body.RemoveRange(position, length);
            Refresh();
        }
        #endregion
        #region Replace Methods
        public void ReplaceInteger(int value)
        {
            ReplaceInteger(value, _position);
        }
        public void ReplaceInteger(int value, int position)
        {
            RemoveInteger(position);
            WriteInteger(value, position);
        }

        public void ReplaceShort(ushort value)
        {
            ReplaceShort(value, _position);
        }
        public void ReplaceShort(ushort value, int position)
        {
            RemoveShort(position);
            WriteShort(value, position);
        }

        public void ReplaceBoolean(bool value)
        {
            ReplaceBoolean(value, _position);
        }
        public void ReplaceBoolean(bool value, int position)
        {
            RemoveBoolean(position);
            WriteBoolean(value, position);
        }

        public void ReplaceString(string value)
        {
            ReplaceString(value, _position);
        }
        public void ReplaceString(string value, int position)
        {
            int oldLength = Length;

            RemoveString(position);
            WriteString(value, position);

            if (position < _position)
            {
                _position +=
                    ((oldLength - Length) * -1);
            }
        }
        #endregion

        public bool IsStringReadable()
        {
            return IsStringReadable(_position);
        }
        public bool IsStringReadable(int position)
        {
            int readable = (_body.Count - position);
            if (readable < 2) return false;

            ushort stringLength =
                BigEndian.ToUInt16(_bodyBuffer, position);

            return (readable >= (stringLength + 2));
        }

        public int GetReadableBytes(int position)
        {
            return (_body.Count - position);
        }

        private void Refresh()
        {
            ResetCache();
            _bodyBuffer = _body.ToArray();
        }
        private void ResetCache()
        {
            _toBytesCache = null;
            _toStringCache = null;
        }

        public override string ToString()
        {
            return _toStringCache ??
                (_toStringCache = ToString(ToBytes()));
        }
        public static string ToString(byte[] data)
        {
            string result = Encoding.GetEncoding(0).GetString(data);
            for (int i = 0; i <= 13; i++)
            {
                result = result.Replace(
                    ((char)i).ToString(), "[" + i + "]");
            }
            return result;
        }

        public byte[] ToBytes()
        {
            if (IsCorrupted)
                _toBytesCache = _bodyBuffer;

            return _toBytesCache ??
                (_toBytesCache = Construct(Header, _bodyBuffer));
        }
        public static byte[] ToBytes(string packet)
        {
            return ToBytes(packet, null);
        }
        public static byte[] ToBytes(string packet, ITranslator translator)
        {
            for (int i = 0; i <= 13; i++)
            {
                packet = packet.Replace(
                    "[" + i + "]", ((char)i).ToString());
            }

            if (translator != null)
            {
                MatchCollection constantMatches = _fieldGrabber.Matches(packet);
                foreach (Match match in constantMatches)
                {
                    string field = match.Groups["field"].Value.ToLower();
                    packet = packet.Replace($"%{field}%", translator.Print(field));
                }
            }

            MatchCollection paramMatches = _paramGrabber.Matches(packet);
            foreach (Match match in paramMatches)
            {
                string type = match.Groups["type"].Value.ToLower();
                string value = match.Groups["value"].Value;

                byte[] data = null;
                if (translator?.IsTranslatingPrimitives ?? false)
                {
                    data = translator.Translate((type.StartsWith("$") ?
                        type.Substring(1) : type), value);
                }
                else
                {
                    switch (type)
                    {
                        default:
                        {
                            // Jump over the '$' character.
                            data = translator?.Translate(type.Substring(1), value);
                            break;
                        }
                        case "s":
                        {
                            data = BigEndian.GetBytes(value);
                            break;
                        }
                        case "u":
                        {
                            ushort.TryParse(value, out ushort uValue);
                            data = BigEndian.GetBytes(uValue);
                            break;
                        }
                        case "i":
                        {
                            int.TryParse(value, out int iValue);
                            data = BigEndian.GetBytes(iValue);
                            break;
                        }
                        case "d":
                        {
                            double.TryParse(value, out double dValue);
                            data = BigEndian.GetBytes(dValue);
                            break;
                        }
                        case "b":
                        {
                            if (!byte.TryParse(value, out byte bValue))
                            {
                                data = BigEndian.GetBytes(
                                    (value.Trim().ToLower() == "true"));
                            }
                            else data = new[] { bValue };
                            break;
                        }
                    }
                }
                if (data != null)
                {
                    packet = packet.Replace(match.Value,
                        Encoding.GetEncoding(0).GetString(data));
                }
            }
            if (packet.StartsWith("{l}") && packet.Length >= 5)
            {
                byte[] lengthData = BigEndian.GetBytes(packet.Length - 3);
                packet = Encoding.GetEncoding(0).GetString(lengthData) + packet.Substring(3);
            }
            return Encoding.GetEncoding(0).GetBytes(packet);
        }

        public static byte[] GetBytes(params object[] values)
        {
            var buffer = new List<byte>();
            foreach (object value in values)
            {
                /*switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Byte: buffer.Add((byte)value); break;
                    case TypeCode.Boolean: buffer.Add(Convert.ToByte((bool)value)); break;
                    case TypeCode.Int32: buffer.AddRange(BigEndian.GetBytes((int)value)); break;
                    case TypeCode.UInt16: buffer.AddRange(BigEndian.GetBytes((ushort)value)); break;
                    case TypeCode.Double: buffer.AddRange(BigEndian.GetBytes((double)value)); break;

                    default:
                    case TypeCode.String:
                    {
                        byte[] data =
                            value as byte[] ?? BigEndian.GetBytes(value.ToString());

                        buffer.AddRange(data);
                        break;
                    }
                }*/
            }
            return buffer.ToArray();
        }
        public static byte[] Construct(ushort header, params object[] values)
        {
            byte[] body = GetBytes(values);
            var buffer = new byte[6 + body.Length];

            byte[] headerData = BigEndian.GetBytes(header);
            byte[] lengthData = BigEndian.GetBytes(2 + body.Length);

            Buffer.BlockCopy(lengthData, 0, buffer, 0, 4);
            Buffer.BlockCopy(headerData, 0, buffer, 4, 2);
            Buffer.BlockCopy(body, 0, buffer, 6, body.Length);
            return buffer;
        }

        public void Skip(string pattern)
        {
            MatchCollection matches =
                Regex.Matches(pattern, @"{(?<type>s|i|b|d)(:(?<quant>\d+))?\}");

            for (int i = 0; i < matches.Count;)
            {
                Match match = matches[i];
                string typeG = match.Groups["type"].Value;

                int quant = 1;
                string quantG = match.Groups["quant"].Value;
                if (!string.IsNullOrWhiteSpace(quantG) &&
                    !int.TryParse(quantG, out quant))
                {
                    quant = 1;
                }

                for (int j = 0; j < quant; j++, i++)
                {
                    switch (typeG)
                    {
                        case "s":
                        ReadString();
                        break;

                        case "i":
                        ReadInteger();
                        break;

                        case "b":
                        ReadBoolean();
                        break;
                        case "d":
                        ReadDouble();
                        break;
                    }
                }
            }
        }
        public T ReadAfter<T>(string pattern)
        {
            Skip(pattern);

            /*switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String: return (T)(object)ReadString();
                case TypeCode.Int32: return (T)(object)ReadInteger();
                case TypeCode.Byte: return (T)(object)ReadBytes(1);
                case TypeCode.Boolean: return (T)(object)ReadBoolean();
                case TypeCode.Double: return (T)(object)ReadDouble();
                default: return default(T);
            }*/

            return default(T);
        }
    }
}