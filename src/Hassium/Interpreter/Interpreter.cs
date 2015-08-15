using System;
using System.Collections.Generic;

namespace Hassium
{
    public class Interpreter
    {
        public static Dictionary<string, object> variables = new Dictionary<string, object>();
        private AstNode code;

        public Interpreter(AstNode code)
        {
            variables = new Dictionary<string, object>();
            this.code = code;
            variables.Add("print", new InternalFunction(BuiltInFunctions.Print));
            variables.Add("strcat", new InternalFunction(BuiltInFunctions.Strcat));
            variables.Add("input", new InternalFunction(BuiltInFunctions.Input));
            variables.Add("strlen", new InternalFunction(BuiltInFunctions.Strlen));
            variables.Add("cls", new InternalFunction(BuiltInFunctions.Cls));
            variables.Add("getch", new InternalFunction(BuiltInFunctions.Getch));
            variables.Add("puts", new InternalFunction(BuiltInFunctions.Puts));
            variables.Add("free", new InternalFunction(BuiltInFunctions.Free));
            variables.Add("exit", new InternalFunction(BuiltInFunctions.Exit));
            variables.Add("system", new InternalFunction(BuiltInFunctions.System));
            variables.Add("mdir", new InternalFunction(BuiltInFunctions.Mdir));
            variables.Add("ddir", new InternalFunction(BuiltInFunctions.DDir));
            variables.Add("dfile", new InternalFunction(BuiltInFunctions.Dfile));
            variables.Add("sstr", new InternalFunction(BuiltInFunctions.Sstr));
            variables.Add("begins", new InternalFunction(BuiltInFunctions.Begins));
            variables.Add("type", new InternalFunction(BuiltInFunctions.Type));
            variables.Add("pow", new InternalFunction(BuiltInFunctions.Pow));
            variables.Add("sqrt", new InternalFunction(BuiltInFunctions.Sqrt));
            variables.Add("dowstr", new InternalFunction(BuiltInFunctions.DowStr));
            variables.Add("dowfile", new InternalFunction(BuiltInFunctions.DowFile));
            variables.Add("upfile", new InternalFunction(BuiltInFunctions.UpFile));
            variables.Add("throw", new InternalFunction(BuiltInFunctions.Throw));
        }

        public void Execute()
        {
            foreach (AstNode node in this.code.Children)
            {
                evaluateNode(node);
            }
        }

        private object interpretBinaryOp(BinOpNode node)
        {
            switch (node.BinOp)
            {
                case BinaryOperation.Addition:
                    return (double)(evaluateNode(node.Left)) + (double)(evaluateNode(node.Right));
                case BinaryOperation.Subtraction:
                    return (double)(evaluateNode(node.Left)) - (double)(evaluateNode(node.Right));
                case BinaryOperation.Division:
                    return (double)(evaluateNode(node.Left)) / (double)(evaluateNode(node.Right));
                case BinaryOperation.Multiplication:
                    return (double)(evaluateNode(node.Left)) * (double)(evaluateNode(node.Right));
                case BinaryOperation.Assignment:
                    if (!(node.Left is IdentifierNode))
                        throw new Exception("Not a valid identifier");
                    object right = evaluateNode(node.Right);
                    if (variables.ContainsKey(node.Left.ToString()))
                        variables.Remove(node.Left.ToString());
                    variables.Add(node.Left.ToString(), right);
                    return right.ToString();
            }
            // Raise error
            return -1;
        }

        private object evaluateNode(AstNode node)
        {
            if (node is NumberNode)
            {
                return ((NumberNode)node).Value;
            }
            else if (node is StringNode)
            {
                return ((StringNode)node).Value;
            }
            else if (node is BinOpNode)
            {
                return interpretBinaryOp((BinOpNode)node);
            }
            else if (node is IdentifierNode)
            {
                if (variables.ContainsKey(node.ToString()))
                    return variables[node.ToString()];
                else
                    throw new Exception("Undefined variable");
            }
            else if (node is FunctionCallNode)
            {
                FunctionCallNode call = node as FunctionCallNode;
                IFunction target = evaluateNode(call.Target) as IFunction;
                if (target == null)
                    throw new Exception("Attempt to run a non-valid function!");
                object[] arguments = new object[call.Arguments.Children.Count];
                for (int x = 0; x < call.Arguments.Children.Count; x++)
                    arguments[x] = evaluateNode(call.Arguments.Children[x]);
                return target.Invoke(arguments);
            }

            return 0;
        }
    }
}

