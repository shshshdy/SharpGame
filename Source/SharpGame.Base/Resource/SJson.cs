

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public class JSElement : Dictionary<string, object> { }
    /// <summary>
    /// Provides functions for encoding and decoding files in the simplified JSON format.
    /// </summary>
    public class SJSON
	{
		/// <summary>
		///  Encodes the Hashtable t in the simplified JSON format. The Hashtable can
		///  contain, numbers, bools, strings, ArrayLists and Hashtables.
		/// </summary>
		public static string Encode(JSElement t)
		{
			StringBuilder sb = new StringBuilder();
			WriteRootObject(t, sb);
			sb.Append('\n');
			return sb.ToString();
		}

		/// <summary>
		/// Encodes the object o in the simplified JSON format (not as a root object).
		/// </summary>
		public static string EncodeObject(object o)
		{
			StringBuilder sb = new StringBuilder();
			Write(o, sb, 0);
			return sb.ToString();
		}

		/// <summary>
		/// Decodes a SJSON bytestream into a Hashtable with numbers, bools, strings,
		/// ArrayLists and Hashtables.
		/// </summary>
		public static JSElement Decode(string sjson)
		{
			int index = 0;
			return ParseRootObject(sjson, ref index);
		}
        
		public static JSElement Load(string path)
		{
			string text = System.IO.File.ReadAllText(path);
			return Decode(text) as JSElement;
		}
        
		public static void Save(JSElement h, string path)
		{
			string s = Encode(h);
            System.IO.File.WriteAllText(path, s);
		}

		static void WriteRootObject(JSElement t, StringBuilder builder)
		{
		   WriteObjectFields(t, builder, 0);
		}

		static void WriteObjectFields(JSElement t, StringBuilder builder, int indentation)
		{
			var keys = new SortedSet<string>(t.Keys);
            
			foreach (string key in keys)
            {
				WriteNewLine(builder, indentation);
				builder.Append(key);
				builder.Append(" = ");
				Write(t[key], builder, indentation);
			}
		}

		static void WriteNewLine(StringBuilder builder, int indentation)
		{
			builder.Append('\n');
			for (int i = 0; i < indentation; ++i)
				builder.Append('\t');
		}

		static void Write(object o, StringBuilder builder, int indentation)
		{
            if (o == null)
                builder.Append("null");

            switch (o)
            {
                case bool v:
                    builder.Append(v ? "true" : "false");
                    break;
                case int v:
                    builder.Append(v);
                    break;
                case float v:
                    builder.Append(v);
                    break;
                case double v:
                    builder.Append(v);
                    break;
                case string v:
                    WriteString(v, builder);
                    break;
                case ArrayList v:
                    WriteArray(v, builder, indentation);
                    break;
                case JSElement v:
                    write_object(v, builder, indentation);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

		static void WriteString(string s, StringBuilder builder)
		{
			builder.Append('"');
			for (int i=0; i<s.Length; ++i) {
				char c = s[i];
				if (c == '"' || c == '\\')
					builder.Append('\\');
				builder.Append(c);
			}
			builder.Append('"');
		}

		static void WriteArray(ArrayList a, StringBuilder builder, int indentation)
		{
			builder.Append('[');
			foreach (object item in a) {
				builder.Append(' ');
				Write(item, builder, indentation+1);
			}
			builder.Append(" ]");
		}

		static void write_object(JSElement t, StringBuilder builder, int indentation)
		{
			builder.Append('{');
			WriteObjectFields(t, builder, indentation+1);
			WriteNewLine(builder, indentation);
			builder.Append('}');
		}

		static JSElement ParseRootObject(string json, ref int index)
		{
			JSElement ht = new JSElement();
			while (!AtEnd(json, ref index)) {
				string key = ParseIdentifier(json, ref index);
				Consume(json, ref index, "=");
				object value = ParseValue(json, ref index);
				ht[key] = value;
			}
			return ht;
		}

		static bool AtEnd(string json, ref int index)
		{
			SkipWhiteSpace(json, ref index);
			return (index >= json.Length);
		}

		static void SkipWhiteSpace(string json, ref int index)
		{
			while (index < json.Length) {
				char c = json[index];
				if (c == '/')
					SkipComment(json, ref index);
				else if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == ',')
					++index;
				else
					break;
			}
		}

		static void SkipComment(string json, ref int index)
		{
			char next = json[index + 1];
			if (next == '/')
			{
				while (index + 1 < json.Length && json[index] != '\n')
					++index;
				++index;
			}
			else if (next == '*')
			{
				while (index + 2 < json.Length && (json[index] != '*' || json[index + 1] != '/'))
					++index;
				index += 2;
			}
			else
				Debug.Assert(false);
		}

		static string ParseIdentifier(string json, ref int index)
		{
			SkipWhiteSpace(json, ref index);

			if (json[index] == '"')
				return ParseString(json, ref index);

			StringBuilder s = new StringBuilder();
			while (true)
            {
				char c = (char)json[index];
				if (c == ' ' || c == '\t' || c == '\n' || c == '=')
					break;
				s.Append(c);
				++index;
			}
			return (string)s.ToString();
		}

		static void Consume(string json, ref int index, string consume)
		{
			SkipWhiteSpace(json, ref index);
			for (int i = 0; i< consume.Length; ++i)
            {
				if (json[index] != consume[i])
					Debug.Assert(false);
				++index;
			}
		}

		static object ParseValue(string json, ref int index)
		{
			char c = Next(json, ref index);

			if (c == '{')
				return ParseObject(json, ref index);
			else if (c == '[')
				return ParseArray(json, ref index);
			else if (c == '"')
				return ParseString(json, ref index);
			else if (c == '-' || c >= '0' && c <= '9')
				return ParseNumber(json, ref index);
			else if (c == 't')
			{
				Consume(json, ref index, "true");
				return true;
			}
			else if (c == 'f')
			{
				Consume(json, ref index, "false");
				return false;
			}
			else if (c == 'n')
			{
				Consume(json, ref index, "null");
				return null;
			}
			else
			{
				Debug.Assert(false);
				return null;
			}

		}

		static char Next(string json, ref int index)
		{
			SkipWhiteSpace(json, ref index);
			return json[index];
		}

		static JSElement ParseObject(string json, ref int index)
		{
			JSElement ht = new JSElement();
			Consume(json, ref index, "{");
			SkipWhiteSpace(json, ref index);

			while (Next(json, ref index) != '}') {
				string key = ParseIdentifier(json, ref index);
				Consume(json, ref index, "=");
				object value = ParseValue(json, ref index);
				ht[key] = value;
			}
			Consume(json, ref index, "}");
			return ht;
		}

		static ArrayList ParseArray(string json, ref int index)
		{
			ArrayList a = new ArrayList();
			Consume(json, ref index, "[");
			while (Next(json, ref index) != ']') {
				object value = ParseValue(json, ref index);
				a.Add(value);
			}
			Consume(json, ref index, "]");
			return a;
		}

		static string ParseString(string json, ref int index)
		{
			StringBuilder s = new StringBuilder();

			Consume(json, ref index, "\"");
			while (true) {
				char c = (char)json[index];
				++index;
				if (c == '"')
					break;
				else if (c != '\\')
					s.Append(c);
				else {
                    char q = (char)json[index];
					++index;
					if (q == '"' || q == '\\' || q == '/')
						s.Append(q);
					else if (q == 'b') s.Append('\b');
					else if (q == 'f') s.Append('\f');
					else if (q == 'n') s.Append('\n');
					else if (q == 'r') s.Append('\r');
					else if (q == 't') s.Append('\t');
					else if (q == 'u')
					{
						Debug.Assert(false);
					}
					else
					{
						Debug.Assert(false);
					}
				}
			}
			//s.Append('\0');
			return (string)s.ToString();
		}

		static unsafe double ParseNumber(string json, ref int index)
		{
			int end = index;
			while (end < json.Length && "0123456789+-.eE".IndexOf((char)json[end]) != -1)
				++end;
            
            string num = json.Substring(index, end - index);                
            index = end;
            return double.Parse((string)num);
            
        }
	}
}
