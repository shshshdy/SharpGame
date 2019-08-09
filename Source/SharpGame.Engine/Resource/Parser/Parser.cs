using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    using static TokenType;
    public abstract class Stmt
    {
    }

    public class Parser
    {
        protected class ParseError : Exception
        {
        }

        private int _current = 0;

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
        }

        public List<Token> Tokens { get; }

        public List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        protected virtual Stmt Declaration()
        {
            return null;
        }

        [DebuggerStepThrough]
        protected bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        [DebuggerStepThrough]
        protected bool Check(TokenType tokenType)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == tokenType;
        }

        [DebuggerStepThrough]
        protected Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        [DebuggerStepThrough]
        protected bool IsAtEnd()
        {
            return Peek().Type == EOF;
        }

        [DebuggerStepThrough]
        protected Token Peek()
        {
            return Tokens[_current];
        }

        [DebuggerStepThrough]
        protected Token Previous()
        {
            return Tokens[_current - 1];
        }
    }
}
