using Hassium.Interpreter;
using Hassium.Lexer;

namespace Hassium.Parser.Ast
{
    public class IfNode: AstNode
    {
        public AstNode Predicate
        {
            get
            {
                return Children[0];
            }
        }
        public AstNode Body
        {
            get
            {
                return Children[1];
            }
        }
        public AstNode ElseBody
        {
            get
            {
                return Children[2];
            }
        }

        public IfNode(int position, AstNode predicate, AstNode body) : this(position, predicate, body, new CodeBlock(position))
        {
        }

        public IfNode(int position, AstNode predicate, AstNode body, AstNode elseBody) : base(position)
        {
            Children.Add(predicate);
            Children.Add(body);
            Children.Add(elseBody);
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }
    }
}

