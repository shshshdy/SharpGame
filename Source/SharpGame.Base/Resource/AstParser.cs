using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    using ChildMap = System.Collections.Generic.Dictionary<String, List<AstNode>>;

    public class AstNode
    {
        public string token;
        public string value;
        public bool isQuote = false;

        ChildMap children;

        public AstNode(string type)
        {
            token = type;
        }

        public bool IsObject => children != null;
        public int ChildCount => children == null ? 0 : children.Count;
        public ChildMap Children => children;

        public int GetChild(string key, out List<AstNode> child)
        {
            if (children != null)
            {
                if (children.TryGetValue(key, out child))
                {
                    return child.Count;
                }
            }

            child = null;
            return 0;

        }

        public void AddChild(AstNode node)
        {
            if (children == null)
            {
                children = new ChildMap();
            }

            if (!children.TryGetValue(node.token, out var lst))
            {
                lst = new List<AstNode>();
                children.Add(node.token, lst);
            }

            lst.Add(node);

        }

        public AstNode GetChild(string name)
        {
            if (children != null)
            {
                if (children.TryGetValue(name, out var lst))
                {
                    return lst[0];
                }
            }

            return null;
        }

        public void VisitChild(string key, Action<AstNode> fn)
        {
            if (children != null && fn != null)
            {
                if (children.TryGetValue(key, out var lst))
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
            Console.WriteLine(space + token + " = " + value);

            if (IsObject)
            {
                Console.WriteLine(space + "{");
                var it = children.GetEnumerator();
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
        List<AstNode> root = new List<AstNode>();
        List<AstNode> parents = new List<AstNode>();
        AstNode current;

        public AstParser()
        {
        }

        public List<AstNode> Root => root;

        public AstNode Parent => parents.Empty() ? null : parents.Back();

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
            foreach (var n in root)
            {
                n.Print(0);
            }
        }

        public unsafe bool Parse(string str)
        {
            fixed(char* buf = str)
            {
                if (!Tokenize(buf, str.Length))
                {
                    return false;
                }
            }

            return !root.Empty();
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

            StringBuilder lexeme = new StringBuilder();
            //String lexeme = "";
            uint line = 1, state = READY, lastQuote = 0;
            int sourceStack = 0;
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
                            lexeme.Clear();
                            state = COMMENT;
                        }
                        else if (c == star && lastc == slash)
                        {
                            lexeme.Clear();
                            state = MULTICOMMENT;
                        }
                        else if (c == quote)
                        {
                            // Clear out the lexeme ready to be filled with quotes!
                            lexeme.Clear();
                            lexeme.Append(c);
                            state = QUOTE;
                        }
                        else if (IsNewline(c))
                        {
                            lexeme.Clear();
                            lexeme.Append(c);
                            SetToken(lexeme.ToString(), line);
                        }
                        else if (IsColon(c))
                        {
                        }
                        
                        else if (c == openbrace)
                        {
                            var node = current;
                          
                            bool isSource = current.token.StartsWith("@");
                            lexeme.Clear();
                            lexeme.Append(c);

                            if (isSource)
                            {
                                //enter source
                                SetToken(lexeme.ToString(), line, true);

                                Debug.Assert(sourceStack == 0);
                                lexeme.Clear();
                                sourceStack++;
                                state = SOURCE;
                            }
                            else
                            {
                                state = WORD;
                            }
                          
                        }
                        else if (!IsWhitespace(c))
                        {
                            lexeme.Clear();
                            lexeme.Append(c);
                            if (c == slash)
                                state = POSSIBLECOMMENT;
                            else
                                state = WORD;
                        }
                        break;
                    case COMMENT:
                        if (IsNewline(c))
                        {
                            lexeme.Clear();
                            lexeme.Append(c);
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        break;
                    case MULTICOMMENT:
                        if (c == slash && lastc == star)
                            state = READY;
                        break;
                    case SOURCE:
                                 
                        if (c == openbrace)
                        {
                            sourceStack++;
                            lexeme.Append(c);
                        }
                        else if(c == closebrace)
                        {
                            sourceStack--;

                            //exit source
                            if (sourceStack == 0)
                            {
                                SetToken(lexeme.ToString(), line, true);
                                lexeme.Clear();
                                lexeme.Append(c);
                                SetToken(lexeme.ToString(), line, true);
                                state = READY;
                            }
                            else
                            {
                                lexeme.Append(c);
                            }
                        }
                        else
                        {
                            lexeme.Append(c);

                        }
                                           

                        break;
                    case POSSIBLECOMMENT:
                        if (c == slash && lastc == slash)
                        {
                            lexeme.Clear();
                            state = COMMENT;
                            break;
                        }
                        else if (c == star && lastc == slash)
                        {
                            lexeme.Clear();
                            state = MULTICOMMENT;
                            break;
                        }
                        else
                        {
                            state = WORD;
                        }

                        if (IsNewline(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            lexeme.Clear();
                            lexeme.Append(c);
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (IsWhitespace(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (IsColon(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (c == openbrace || c == closebrace)
                        {
                            bool isSource = false;
                            if (c == openbrace)
                            {
                                isSource =(lexeme.Length > 0 && lexeme[0] == '@');
                            }

                            SetToken(lexeme.ToString(), line);
                            lexeme.Clear();
                            lexeme.Append(c.ToString());
                            SetToken(lexeme.ToString(), line, isSource);

                            //enter source
                            if(isSource)
                            {
                                if(sourceStack == 0)
                                {
                                    sourceStack++;
                                    state = SOURCE;
                                    break;
                                }
                            }

                            state = READY;
                        }
                        else
                        {
                            lexeme.Append(c);
                        }
                        break;
                    case WORD:
                        if (IsNewline(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            lexeme.Clear();
                            lexeme.Append(c.ToString());
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (IsWhitespace(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (IsColon(c))
                        {
                            SetToken(lexeme.ToString(), line);
                            state = READY;
                        }
                        else if (c == openbrace || c == closebrace)
                        {
                            bool isSource = false;
                            if (c == openbrace)
                            {
                                isSource = lexeme.Length > 0 && lexeme[0] == '@';
                            }

                            SetToken(lexeme.ToString(), line);
                            lexeme.Clear();
                            lexeme.Append(c.ToString());
                            SetToken(lexeme.ToString(), line, isSource);

                            //enter source
                            if (isSource)
                            {
                                if (sourceStack == 0)
                                {
                                    sourceStack++;
                                    state = SOURCE;
                                    break;
                                }
                            }
                            state = READY;
                        }
                        else
                        {
                            lexeme.Append(c);
                        }
                        break;
                    case QUOTE:
                        if (c != backslash)
                        {
                            // Allow embedded quotes with escaping
                            if (c == quote && lastc == backslash)
                            {
                                lexeme.Append(c);
                            }
                            else if (c == quote)
                            {
                                lexeme.Append(c);
                                SetToken(lexeme.ToString(), line);
                                state = READY;
                            }
                            else
                            {
                                // Backtrack here and allow a backslash normally within the quote
                                if (lastc == backslash)
                                    lexeme.Append("\\").Append(c);
                                else
                                    lexeme.Append(c);
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
                if (lexeme.Length > 0)
                    SetToken(lexeme.ToString(), line);
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

        void SetToken(string lexeme, uint line, bool source = false)
        {
            const char openBracket = '{', closeBracket = '}', quote = '\"';

            // Check the user token map first
            if (lexeme.Length == 1 && IsNewline(lexeme[0]))
            {
            }
            else if (lexeme.Length == 1 && lexeme[0] == openBracket)
            {
                System.Diagnostics.Debug.Assert(current != null);
                if(source)
                {

                }
                else
                {
                    parents.Add(current);
                    current = null;
                }
            }
            else if (lexeme.Length == 1 && lexeme[0] == closeBracket)
            {
                if (source)
                {
                    current = null;
                }
                else
                {

                    current = null;
                    parents.Pop();
                }
            }
            else
            {
                bool isQuote = false;
                // This is either a non-zero length phrase or quoted phrase
                if (lexeme.Length >= 2 && lexeme[0] == quote && lexeme[lexeme.Length - 1] == quote)
                {
                    isQuote = true;
                }

                if (current != null)
                {
                    if (source)
                    {
                        current.value = lexeme;
                    }
                    else
                    {
                        if (current.value.Empty() && !current.isQuote)
                        {
                            current.value = isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme;
                        }
                        else
                        {
                            CreateNode(isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme, line);
                        }
                    }
               
                }
                else
                {
                    if(source)
                    {
                        CreateNode(lexeme, line);
                    }
                    else
                    {
                        CreateNode(isQuote ? lexeme.Substring(1, lexeme.Length - 2) : lexeme, line);
                    }
                }

                current.isQuote = isQuote || source;
            }


        }

        void CreateNode(string type, uint line)
        {
            current = new AstNode(type);
            AstNode parent = Parent;
            if (parent != null)
            {
                parent.AddChild(current);
            }
            else
            {
                root.Add(current);
            }
        }
    }
}
