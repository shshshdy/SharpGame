using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using static TokenType;

    public class Layout : Stmt
    {

    }

    public class LayoutParser : Parser
    {
        public LayoutParser(List<Token> tokens) : base(tokens)
        {
        }

        protected override Stmt Declaration()
        {
            try
            {
                if (Match(LAYOUT))
                {

                }

                return null;
            }
            catch (ParseError)
            {
                //    Synchronize();
                return null;
            }
        }

    }
}
