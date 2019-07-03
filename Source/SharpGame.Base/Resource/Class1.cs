using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Resource
{

    class SJSON
    {
#if false
        static void parseError(string source, int at, string expected)
        {
            var token = String.fromCharCode(source[at]);
            var near = source.toString('utf8', at, Math.min(at + 25, source.length + 1));
            var line = 1;
            for (int i = 0; i < at; ++i)
                if (source[i] == 10) ++line;

            //return new SyntaxError(`Unexpected token '${token}', expected '${expected}' on line ${ line } near '${near}'.`);
        }

        static int[] characterMask(string str)
        {
            var mask = new int[4];
            for (var i = 0; i < str.Length; ++i)
            {
                int c = str[i];
                mask[c / 32] |= (1 << (c % 32));
            }
            return mask;
        }

        static bool hasChar(int[] mask, int c)
        {
            return (mask[c / 32] & (1 << (c % 32))) != 0;
        }

        static int[] NUMBER = characterMask("-+0123456789");
        static int[] NUMBER_EXP = characterMask(".eE");
        static int[] ID_TERM = characterMask(" \t\n=:");
        static int[] WHITESPACE = characterMask(" \n\r\t,");

        static void parse(string s)
        {

            int i = 0;
            //if (typeof(s) === 'string') s = Buffer(s);

            void ws()
            {
                while (i < s.Length)
                {
                    if (s[i] == 47)
                    { // "/"
                        ++i;
                        if (s[i] == 47)
                            while (s[++i] != 10) ; // "\n"
                        else if (s[i] == 42) // "*"
                            while (s[++i] != 42) ;
                    }
                    else if (!hasChar(WHITESPACE, s[i]))
                    {
                        break;
                    }
                    ++i;
                }
            }

            void consume(char c)
            {
                ws();
                if (s[i++] != c)
                    throw parseError(s, i - 1, String.fromCharCode(c));
            }

            void consumeKeyword(string kw)
            {
                ws();
                //const chars = Buffer(kw);
                foreach (var c in kw)
                {
                    if (s[i++] != c)
                        throw parseError(s, i - 1, String.fromCharCode(c));
                }
            }

            object pvalue()
            {
                ws();
                var c = s[i];
                if (hasChar(NUMBER, c)) return pnumber();
                if (c == 123) return pobject();  // "{"
                if (c == 91) return parray();   // "["
                if (c == 34) return pstring();  // "
                if (c == 116) { consumeKeyword("true"); return true; }
                if (c == 102) { consumeKeyword("false"); return false; }
                if (c == 110) { consumeKeyword("null"); return null; }
                throw parseError(s, i, "number, {, [, \", true, false or null");
            }

            double pnumber()
            {
                ws();
                int start = i;
                bool isFloat = false;
                for (; i < s.Length; ++i)
                {
                    int c = s[i];
                    bool expc = hasChar(NUMBER_EXP, c);
                    isFloat |= expc;
                    if (!expc && !hasChar(NUMBER, c))
                        break;
                }
                const n = s.toString('utf8', start, i);
                return isFloat ? parseFloat(n) : parseInt(n);
            }

            void pstring()
            {
                // Literal string
                if (s[i] === 34 && s[i + 1] === 34 && s[i + 2] === 34)
                {
                    i += 3;
                    const start = i;
                    for (; s[i] !== 34 || s[i + 1] !== 34 || s[i + 2] !== 34; ++i) ;
                    i += 3;
                    return s.toString('utf8', start, i - 3);
                }

                const start = i;
                let escape = false;
                consume(34);
                for (; s[i] !== 34; ++i)
                { // unescaped "
                    if (s[i] === 92)
                    {
                        ++i;
                        escape = true;
                    }
                }
                consume(34);
                if (!escape)
                    return s.toString('utf8', start + 1, i - 1);

                i = start;
                var octets = [];
                consume(34);
                for (; s[i] !== 34; ++i)
                { // unescaped "
                    if (s[i] === 92)
                    {
                        ++i;
                        if (s[i] == 98) octets.push(8); // \b
                        else if (s[i] == 102) octets.push(12); // \f
                        else if (s[i] == 110) octets.push(10); // \n
                        else if (s[i] == 114) octets.push(13); // \r
                        else if (s[i] == 116) octets.push(9); // \t
                        else if (s[i] == 117)
                        { // \u
                            ++i;
                            octets.push(16 * (s[i] - 48) + s[i + 1] - 48);
                            i += 2;
                            octets.push(16 * (s[i] - 48) + s[i + 1] - 48);
                            i += 2;
                        }
                        else octets.push(s[i]); // \" \\ \/
                    }
                    else
                        octets.push(s[i]);
                }
                consume(34);
                return Buffer(octets).toString('utf8');
            }

            void parray()
            {
                const ar = [];
                ws();
                consume(91); // "["
                ws();
                for (; s[i] !== 93; ws()) // "]"
                    ar.push(pvalue());

                consume(93);
                return ar;
            }

            void pidentifier()
            {
                ws();
                if (i === s.length)  // Catch whitespace EOF
                    return null;
                if (s[i] === 34)
                    return pstring();

                const start = i;
                for (; !hasChar(ID_TERM, s[i]); ++i) ;
                return s.toString("utf8", start, i);
            }

            void pobject()
            {
                const object = Object.setPrototypeOf({ },  null);
            consume(123); // "{"
            ws();
            for (; s[i] !== 125; ws())
            { // "}"
                const key = pidentifier();
                ws();
                (s[i] === 58) ? consume(58) : consume(61); // ":" or "="
                object[key] = pvalue();
            }
            consume(125); // "}"
            return object;
        }

        void proot()
        {
            ws();
            if (s[i] === 123)
                return pobject();

            const object = Object.setPrototypeOf({ },  null);
            while (i < s.length)
            { // "}"
                const key = pidentifier();
                ws();
                if (i === s.length)  // Catch whitespace EOF
                    break;
                (s[i] === 58) ? consume(58) : consume(61); // ":" or "="
                object[key] = pvalue();
            }

            ws();
            if (i != s.length)
                throw parseError(s, i, "end-of-string");

            return object;
        }

    return proot();
    }

    static void stringify(rootObj)
    {
        var nbTabs = 0;

        const endLine = () =>
        {
            let v = '\n';
            let i = 0;
            for (i; i < nbTabs; i++)
            {
                v += '\t';
            }
            return v;
        };

        string sstring(s)
        {
            if (s.match(/\r |\n /))
            {
                return '"""' + s + '"""';
            }
            else
            {
                let r = "";
                for (const symbol of s) {
            switch (symbol)
            {
                case '"':
                    r += '\\"';
                    break;
                case '\\':
                    r += '\\\\';
                    break;
                case '/':
                    r += '\/';
                    break;
                case '\b':
                    r += '\\b';
                    break;
                case '\f':
                    r += '\\f';
                    break;
                case '\n':
                    r += '\\n';
                    break;
                case '\r':
                    r += '\\r';
                    break;
                case '\t':
                    r += '\\t';
                    break;
                default:
                    r += symbol;
                    break;
            }
        }
        return '"' + r + '"';
    }
}

        string snumber(n)
{
    return '' + n;
}

        string sbool(b)
{
    return b ? 'true' : 'false';
}

        void sarray(arr)
{
    string s = '[';
    string k;
    nbTabs++; //indentation
    for (k in arr)
    {
        s += endLine() + svalue(arr[k]);
    }
    nbTabs--; //end indentation
    return s + endLine() + ']';
}

        void getObjKey(k)
{
    return k.match(/\s |=/) ? sstring(k) : k;
}

string sobj(obj)
{
    let s = '{';
    let k;
    nbTabs++; //indentation
    for (k in obj)
    {
        s += endLine() + getObjKey(k) + ' = ' + svalue(obj[k]);
    }
    nbTabs--; //end indentation
    return s + endLine() + '}';
}

string svalue(v)
{
    switch (typeof v) {
        case 'object':
            if (Array.isArray(v))
            {
                return sarray(v);
            }
            return sobj(v);
        case 'number':
            return snumber(v);
        case 'boolean':
            return sbool(v);
        case 'string':
            return sstring(v);
    }
}

string sroot(r)
{
    //If the root is an object loop through key here to not add '{ }' and indentation
    if (typeof r === 'object' && !Array.isArray(r))
    {
        var s = '';
        var k;
        for (k in r)
        {
            s += getObjKey(k) + ' = ' + svalue(r[k]) + endLine();
        }
        return s;
    }
    return svalue(r);
}
    return sroot(rootObj);
  }

#endif

}



}
