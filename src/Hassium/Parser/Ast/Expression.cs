﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hassium.Interpreter;

namespace Hassium.Parser.Ast
{
    public class Expression : AstNode
    {
        public Expression(int pos, AstNode child) : base(pos)
        {
            Children.Add(child);
        }

        public override object Visit(IVisitor visitor)
        {
            return visitor.Accept(this);
        }
    }
}
