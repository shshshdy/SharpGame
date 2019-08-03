using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    using ChildMap = System.Collections.Generic.Dictionary<String, List<AstNode>>;

    public class AstNode
    {
        public String token_;
        public String value_;
        public bool isQuote_ = false;

        ChildMap children_;

        public AstNode(string type)
        {
            token_ = type;
        }

        public bool IsObject => children_ != null;

        public void AddChild(AstNode node)
        {
            if (children_ == null)
            {
                children_ = new ChildMap();
            }

            if (!children_.TryGetValue(node.token_, out var lst))
            {
                lst = new List<AstNode>();
                children_.Add(node.token_, lst);
            }

            lst.Add(node);

        }

        public AstNode GetChild(string name)
        {
            if (children_ != null)
            {
                if (children_.TryGetValue(name, out var lst))
                {
                    return lst[0];
                }
            }

            return null;
        }

        public void VisitChild(string key, Action<AstNode> fn)
        {
            if (children_ != null && fn != null)
            {
                if (children_.TryGetValue(key, out var lst))
                {
                    foreach (var node in lst)
                    {
                        fn.Invoke(node);
                    }
                }
            }
        }


        public void Print(int depth)
        {
            var space = new string(' ', depth*4);
            Console.WriteLine(space + token_ + " = " + value_);

            if (IsObject)
            {
                Console.WriteLine(space + "{");
                var it = children_.GetEnumerator();
                while (it.MoveNext())
                {
                    var lst = it.Current.Value;
                    foreach (var n in lst)
                    {
                        n.Print(depth + 1);
                    }
                }

                Console.WriteLine(space + "}\n");
            }

        }

    }

    public class AstParser
    {
        List<AstNode> root_ = new List<AstNode>();
        String sourceFile_;
        List<AstNode> parents_ = new List<AstNode>();
        AstNode current_;

        public AstParser()
        {
        }

        public AstNode Parent => parents_.Empty() ? null : parents_.Back();

        static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\r' || c == '\t' || c == ',' || c == ';';
        }

        static bool IsNewline(char c)
        {
            return c == '\n' || c == '\r';
        }

        static bool IsColon(char c)
        {
            return c == ':' || c == '=';
        }

        public void Print()
        {
            foreach (var n in root_)
            {
                n.Print(0);
            }
        }

        public unsafe bool Parse(string str)
        {
            var charArray = str.ToCharArray();
            fixed(char* buf = charArray)
            if (!Tokenize(buf, str.Length))
            {
                return false;
            }

            return !root_.Empty();
        }

        // State enums
        const int READY = 0;
        const int COMMENT = 1;
        const int MULTICOMMENT = 2;
        const int WORD = 3;
        const int QUOTE = 4;
        const int POSSIBLECOMMENT = 5;
        const int SOURCE = 6;

        unsafe bool Tokenize(char* buf, int size)
        {

            // Set up some constant characters of interest
            const char quote = '\"', slash = '/', backslash = '\\', openbrace = '{', closebrace = '}', colon = ':', star = '*', cr = '\r', lf = '\n';
            char c = '0', lastc = '0';

            String lexeme = "";
            uint line = 1, state = READY, lastQuote = 0;

            // Iterate over the input
            char* i = buf, end = buf + size;
            while (i != end)
            {
                lastc = c;
                c = *i;

                if (c == quote)
                    lastQuote = line;

                switch (state)
                {
                    case READY:
                        if (c == slash && lastc == slash)
                        {
                            // Comment start, clear out the lexeme
                            lexeme = "";
                            state = COMMENT;
                        }
                        else if (c == star && lastc == slash)
                        {
                            lexeme = "";
                            state = MULTICOMMENT;
                        }
                        else if (c == quote)
                        {
                            // Clear out the lexeme ready to be filled with quotes!
                            lexeme = c.ToString();
                            state = QUOTE;
                        }
                        else if (IsNewline(c))
                        {
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                        }
                        else if (IsColon(c))
                        {
                        }
                        else if (!IsWhitespace(c))
                        {
                            lexeme = c.ToString();
                            if (c == slash)
                                state = POSSIBLECOMMENT;
                            else
                                state = WORD;
                        }
                        break;
                    case COMMENT:
                        if (IsNewline(c))
                        {
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        break;
                    case MULTICOMMENT:
                        if (c == slash && lastc == star)
                            state = READY;
                        break;
                    case POSSIBLECOMMENT:
                        if (c == slash && lastc == slash)
                        {
                            lexeme = "";
                            state = COMMENT;
                            break;
                        }
                        else if (c == star && lastc == slash)
                        {
                            lexeme = "";
                            state = MULTICOMMENT;
                            break;
                        }
                        else
                        {
                            state = WORD;
                        }

                        if (IsNewline(c))
                        {
                            SetToken(lexeme, line);
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (IsWhitespace(c))
                        {
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (IsColon(c))
                        {
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (c == openbrace || c == closebrace)
                        {
                            SetToken(lexeme, line);
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else
                        {
                            lexeme += c;
                        }
                        break;
                    case WORD:
                        if (IsNewline(c))
                        {
                            SetToken(lexeme, line);
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (IsWhitespace(c))
                        {
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (IsColon(c))
                        {
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else if (c == openbrace || c == closebrace)
                        {
                            SetToken(lexeme, line);
                            lexeme = c.ToString();
                            SetToken(lexeme, line);
                            state = READY;
                        }
                        else
                        {
                            lexeme += c;
                        }
                        break;
                    case QUOTE:
                        if (c != backslash)
                        {
                            // Allow embedded quotes with escaping
                            if (c == quote && lastc == backslash)
                            {
                                lexeme += c;
                            }
                            else if (c == quote)
                            {
                                lexeme += c;
                                SetToken(lexeme, line);
                                state = READY;
                            }
                            else
                            {
                                // Backtrack here and allow a backslash normally within the quote
                                if (lastc == backslash)
                                    lexeme = lexeme + "\\" + c;
                                else
                                    lexeme += c;
                            }
                        }
                        break;
                }

                // Separate check for newlines just to track line numbers
                if (c == cr || (c == lf && lastc != cr))
                    line++;

                i++;
            }

            // Check for valid exit states
            if (state == WORD)
            {
                if (!lexeme.Empty())
                    SetToken(lexeme, line);
            }
            else
            {
                if (state == QUOTE)
                {
                    Log.Error("no matching \" found for \" at line " + lastQuote);
                    return false;
                }
            }

            return true;
        }

        void SetToken(String lexeme, uint line)
        {
            const char openBracket = '{', closeBracket = '}', quote = '\"';

            // Check the user token map first
            if (lexeme.Length == 1 && IsNewline(lexeme[0]))
            {
            }
            else if (lexeme.Length == 1 && lexeme[0] == openBracket)
            {
                System.Diagnostics.Debug.Assert(current_ != null);
                parents_.Add(current_);
                current_ = null;
            }
            else if (lexeme.Length == 1 && lexeme[0] == closeBracket)
            {
                current_ = null;
                parents_.Pop();
            }
            else
            {
                bool isQuote = false;
                // This is either a non-zero length phrase or quoted phrase
                if (lexeme.Length >= 2 && lexeme[0] == quote && lexeme[lexeme.Length - 1] == quote)
                {
                    isQuote = true;
                }

                if (current_ != null)
                {
                    if (current_.value_.Empty() && !current_.isQuote_)
                    {
                        current_.value_ = isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme;
                    }
                    else
                    {
                        CreateNode(isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme, line);
                    }
                }
                else
                {
                    CreateNode(isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme, line);
                }

                current_.isQuote_ = isQuote;
            }


        }

        void CreateNode(string type, uint line)
        {
            current_ = new AstNode(type);
            AstNode parent = Parent;
            if (parent != null)
            {
                parent.AddChild(current_);
            }
            else
            {
                root_.Add(current_);
            }
        }
    }
}
