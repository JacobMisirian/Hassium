using System;

namespace Hassium
{
    public enum BinaryOperation
    {
        Addition,
        Subtraction,
        Division,
        Multiplication
    }

    public class BinOpNode : AstNode
    {
        public BinaryOperation BinOp { set; get; }
        public AstNode Left
        {
            get 
            {
                return this.Children [0];
            }
        }

        public AstNode Right
        {
            get
            {
                return this.Children [1];
            }
        }

        public BinOpNode(BinaryOperation type, AstNode left, AstNode right)
        {
            this.BinOp = type;
            this.Children.Add(left);
            this.Children.Add(right);
        }
    }
}

