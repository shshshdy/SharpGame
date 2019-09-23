using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [MessagePackFormatter(typeof(StringIDFormatter))]
    public struct StringID : IEquatable<StringID>
    {
        public string Str { get; }

        [IgnoreDataMember]
        public int Hash { get; }

        public readonly static StringID Empty = string.Empty;

        public StringID(string str)
        {
            Str = str;

            if(str != null)
            {
                Hash = str.GetHashCode();
            }
            else
            {
                Hash = 0;
            }
        }

        [IgnoreDataMember]
        public bool IsNullOrEmpty => string.IsNullOrEmpty(Str);

        public static implicit operator StringID(string value)
        {
            return new StringID(value);
        }

        public static implicit operator string(StringID value)
        {
            return value.Str;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(StringID left, StringID right)
        {
            return left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(StringID left, StringID right)
        {
            return !left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref StringID other)
        {
            return Hash == other.Hash;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(StringID other)
        {
            return Equals(ref other);
        }
        
        public override bool Equals(object value)
        {
            if(!(value is StringID))
                return false;

            var strongValue = (StringID)value;
            return Equals(ref strongValue);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        public override string ToString()
        {
            return Str;
        }

        // serialize/deserialize internal field.
        class StringIDFormatter : IMessagePackFormatter<StringID>
        {
            public int Serialize(ref byte[] bytes, int offset, StringID value, IFormatterResolver formatterResolver)
            {
                return formatterResolver.GetFormatterWithVerify<string>().Serialize(ref bytes, offset, value.Str, formatterResolver);
            }

            public StringID Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                var id = formatterResolver.GetFormatterWithVerify<string>().Deserialize(bytes, offset, formatterResolver, out readSize);
                return new StringID(id);
            }
        }

    }

}
