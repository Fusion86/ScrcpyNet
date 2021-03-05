using System;
using System.Text;

namespace ScrcpyNet.Extensions
{
    public static class SpanExtensions
    {
        public static string ToHexString(this Span<byte> bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }
    }
}
