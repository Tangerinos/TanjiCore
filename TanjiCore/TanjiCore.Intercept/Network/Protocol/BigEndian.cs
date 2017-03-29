using System;
using System.Text;

namespace TanjiCore.Intercept.Network.Protocol
{
    public static class BigEndian
    {
        public static int GetSize(string value)
        {
            return (Encoding.UTF8.GetByteCount(value) + 2);
        }

        public static byte[] GetBytes(int value)
        {
            var buffer = new byte[4];
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)value;
            return buffer;
        }
        public static byte[] GetBytes(bool value)
        {
            var buffer = new byte[1] { 0 };
            buffer[0] = (byte)(value ? 1 : 0);

            return buffer;
        }
        public static byte[] GetBytes(double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return data;
        }
        public static byte[] GetBytes(ushort value)
        {
            var buffer = new byte[2];
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)value;
            return buffer;
        }
        public static byte[] GetBytes(string value)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(value);
            byte[] lengthData = GetBytes((ushort)stringData.Length);

            var buffer = new byte[lengthData.Length + stringData.Length];
            Buffer.BlockCopy(lengthData, 0, buffer, 0, lengthData.Length);
            Buffer.BlockCopy(stringData, 0, buffer, lengthData.Length, stringData.Length);

            return buffer;
        }

        public static int ToInt32(byte[] value, int startIndex)
        {
            int result = (value[startIndex++] << 24);
            result += (value[startIndex++] << 16);
            result += (value[startIndex++] << 8);
            result += (value[startIndex]);
            return result;
        }
        public static bool ToBoolean(byte[] value, int startIndex)
        {
            return (value[startIndex] == 1);
        }
        public static double ToDouble(byte[] value, int startIndex)
        {
            var copy = new byte[value.Length - startIndex];
            Buffer.BlockCopy(value, startIndex, copy, 0, copy.Length);

            Array.Reverse(copy);
            return BitConverter.ToDouble(copy, 0);
        }
        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            int result = (value[startIndex++] << 8);
            result += (value[startIndex]);
            return (ushort)result;
        }
        public static string ToString(byte[] value, int startIndex)
        {
            ushort stringLength =
                ToUInt16(value, startIndex);

            string result = Encoding.UTF8
                .GetString(value, startIndex + 2, stringLength);

            return result;
        }
    }
}