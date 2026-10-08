using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GearSets.Core.Model
{
    /// <summary>
    /// Just enough JSON for the save format, so vanilla installs need no JSON library.
    /// Objects are Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, integers long,
    /// other numbers double.
    /// </summary>
    public static class MiniJson
    {
        public static string Write(object value)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> map:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (KeyValuePair<string, object> kv in map)
                    {
                        if (!first)
                            sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        WriteValue(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                }
                case IList<object> list:
                {
                    sb.Append('[');
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');
                        WriteValue(sb, list[i]);
                    }
                    sb.Append(']');
                    break;
                }
                default:
                    throw new ArgumentException("MiniJson can't write " + v.GetType().Name);
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        public static object Parse(string json)
        {
            if (json == null)
                throw new FormatException("null input");
            var p = new Parser(json);
            p.SkipWs();
            object v = p.Value();
            p.SkipWs();
            if (!p.AtEnd)
                throw new FormatException("trailing characters at " + p.Pos);
            return v;
        }

        private sealed class Parser
        {
            private readonly string _s;
            public int Pos;

            public Parser(string s)
            {
                _s = s;
            }

            public bool AtEnd { get { return Pos >= _s.Length; } }

            public void SkipWs()
            {
                while (Pos < _s.Length && char.IsWhiteSpace(_s[Pos]))
                    Pos++;
            }

            private char Peek()
            {
                if (Pos >= _s.Length)
                    throw new FormatException("unexpected end");
                return _s[Pos];
            }

            private void Expect(char c)
            {
                if (Peek() != c)
                    throw new FormatException("expected '" + c + "' at " + Pos);
                Pos++;
            }

            public object Value()
            {
                SkipWs();
                char c = Peek();
                if (c == '{') return Object();
                if (c == '[') return Array();
                if (c == '"') return String();
                if (Literal("true")) return true;
                if (Literal("false")) return false;
                if (Literal("null")) return null;
                return Number();
            }

            private bool Literal(string word)
            {
                if (string.CompareOrdinal(_s, Pos, word, 0, word.Length) != 0)
                    return false;
                Pos += word.Length;
                return true;
            }

            private Dictionary<string, object> Object()
            {
                var map = new Dictionary<string, object>();
                Expect('{');
                SkipWs();
                if (Peek() == '}')
                {
                    Pos++;
                    return map;
                }
                while (true)
                {
                    SkipWs();
                    string key = String();
                    SkipWs();
                    Expect(':');
                    map[key] = Value();
                    SkipWs();
                    if (Peek() == ',')
                    {
                        Pos++;
                        continue;
                    }
                    Expect('}');
                    return map;
                }
            }

            private List<object> Array()
            {
                var list = new List<object>();
                Expect('[');
                SkipWs();
                if (Peek() == ']')
                {
                    Pos++;
                    return list;
                }
                while (true)
                {
                    list.Add(Value());
                    SkipWs();
                    if (Peek() == ',')
                    {
                        Pos++;
                        continue;
                    }
                    Expect(']');
                    return list;
                }
            }

            private string String()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (true)
                {
                    char c = Peek();
                    Pos++;
                    if (c == '"')
                        return sb.ToString();
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }
                    char e = Peek();
                    Pos++;
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (Pos + 4 > _s.Length)
                                throw new FormatException("bad \\u escape");
                            sb.Append((char)int.Parse(_s.Substring(Pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            Pos += 4;
                            break;
                        default: throw new FormatException("bad escape at " + Pos);
                    }
                }
            }

            private object Number()
            {
                int start = Pos;
                while (Pos < _s.Length && "+-0123456789.eE".IndexOf(_s[Pos]) >= 0)
                    Pos++;
                if (Pos == start)
                    throw new FormatException("unexpected '" + _s[Pos] + "' at " + Pos);
                string n = _s.Substring(start, Pos - start);
                long l;
                if (long.TryParse(n, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out l))
                    return l;
                double d;
                if (double.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    return d;
                throw new FormatException("bad number " + n);
            }
        }
    }
}
