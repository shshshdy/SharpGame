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

        public Parser()
        {
        }

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
        }

        public List<Token> Tokens { get; protected set; }

        public List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                var stmt = Declaration();
                if(stmt != null)
                {
                    statements.Add(stmt);
                }
            }

            return statements;
        }

        protected virtual Stmt Declaration()
        {
            return null;
        }

        [DebuggerStepThrough]
        protected Token Consume(TokenType type, String message)
        {
            if (Check(type)) return Advance();

            throw Error(Peek(), message);
        }

        [DebuggerStepThrough]
        protected ParseError Error(Token token, String message)
        {
            Log.Error(token + message);
            return new ParseError();
        }

        [DebuggerStepThrough]
        protected void Synchronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().Type == SEMICOLON) return;

                switch (Peek().Type)
                {
                    case CLASS:
                    case FUN:
                    case VAR:
                    case FOR:
                    case IF:
                    case WHILE:
                    case PRINT:
                    case RETURN:
                        return;
                }

                Advance();
            }
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
