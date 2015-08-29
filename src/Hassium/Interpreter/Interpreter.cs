/* Credit to contributer Zdimension, who added the lines in interpretBinaryOp for the
implementation of string concat amoung other additions and foreach loop*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hassium.Functions;
using Hassium.Parser.Ast;

namespace Hassium
{
    /// <summary>
    /// Interpreter.
    /// </summary>
    public class Interpreter
    {
        public Stack<StackFrame> CallStack = new Stack<StackFrame>();
        public static Dictionary<string, object> Globals = new Dictionary<string, object>();

        public static Dictionary<string, object> Constants = new Dictionary<string, object>
        {
            {"true", true},
            {"false", false},
            {"null", null},
        };

        private SymbolTable table;
        private AstNode code;
        /// <summary>
        /// Initializes a new instance of the <see cref="Hassium.Interpreter"/> class.
        /// </summary>
        /// <param name="symbolTable">Symbol table.</param>
        /// <param name="code">Code.</param>
        public Interpreter(SymbolTable symbolTable, AstNode code)
        {
            this.code = code;
            table = symbolTable;
            foreach (var entry in GetFunctions())
                Globals.Add(entry.Key, entry.Value);
        }

        public object GetVariable(string name)
        {
            if (Constants.ContainsKey(name))
                return Constants[name];
            if (CallStack.Count > 0 && CallStack.Peek().Locals.ContainsKey(name))
                return CallStack.Peek().Locals[name];
            if (Globals.ContainsKey(name))
                return Globals[name];
            else throw new ArgumentException("The variable '" + name + "' doesn't exist.");
        }

        public bool HasVariable(string name, bool onlyglobal = false)
        {
            return onlyglobal
                ? Globals.ContainsKey(name) || Constants.ContainsKey(name)
                : Globals.ContainsKey(name) || Constants.ContainsKey(name) ||
                  (CallStack.Count > 0 && (CallStack.Peek().Scope.Symbols.Contains(name) || CallStack.Peek().Locals.ContainsKey(name)));
        }

        public void SetGlobalVariable(string name, object value)
        {
            if (Constants.ContainsKey(name))
                throw new ArgumentException("Can't change the value of the internal constant '" + name + "'.");

            Globals[name] = value;
        }

        public void SetLocalVariable(string name, object value)
        {
            if (Constants.ContainsKey(name))
                throw new ArgumentException("Can't change the value of the internal constant '" + name + "'.");

            if (CallStack.Count > 0)
                CallStack.Peek().Locals[name] = value;
        }

        public void SetVariable(string name, object value, bool forceglobal = false, bool onlyexist = false)
        {
            if (!forceglobal && CallStack.Count > 0 && (!onlyexist || (CallStack.Peek().Scope.Symbols.Contains(name) || CallStack.Peek().Locals.ContainsKey(name))))
                SetLocalVariable(name, value);
            else
                SetGlobalVariable(name, value);
        }

        public void FreeVariable(string name, bool forceglobal = false)
        {
            if(Constants.ContainsKey(name)) throw new ArgumentException("Can't delete internal constant '" + name + "'.");
            if (forceglobal)
            {
                if(!Globals.ContainsKey(name)) throw new ArgumentException("The global variable '" + name + "' doesn't exist.");
                Globals.Remove(name);
            }
            else
            {
                if(!HasVariable(name)) throw new ArgumentException("The variable '" + name + "' doesn't exist.");
                if (CallStack.Count > 0 && (CallStack.Peek().Scope.Symbols.Contains(name) || CallStack.Peek().Locals.ContainsKey(name)))
                    CallStack.Peek().Locals.Remove(name);
                else
                    Globals.Remove(name);
            }
        }

        private bool firstExecute = true;
        /// <summary>
        /// Execute this instance.
        /// </summary>
        public void Execute()
        {
            foreach (var node in code.Children)
            {
                if (node is FuncNode && firstExecute)
                {
                    var fnode = ((FuncNode)node);
                    var scope = table.ChildScopes[fnode.Name];
                    SetVariable(fnode.Name, new HassiumFunction(this, fnode, scope));
                }
            }

            if (!Globals.ContainsKey("main"))
            {
                Console.WriteLine("Could not execute, no main entry point of program!");
                Environment.Exit(-1);
            }

            firstExecute = false;
            foreach (var node in code.Children)
            {
                if (node is FuncNode)
                {
                    var fnode = ((FuncNode)node);
                    var scope = table.ChildScopes[fnode.Name];
                    //If there is a main, let it be the main entry point of the program
                    if (fnode.Name == "main")
                    {
                        new HassiumFunction(this, fnode, scope).Invoke(new object[0]);
                        return;
                    }
                }
                else
                    ExecuteStatement(node);
            }
        }

        /*private void interpretBinaryAssign(BinOpNode node)
        {
            SetVariable(node.Left.ToString(), interpretBinaryOp(node, true), false, true);         
        }*/

        /// <summary>
        /// Interprets the binary op.
        /// </summary
        /// <returns>The binary op.</returns>
        /// <param name="node">Node.</param>
        private object interpretBinaryOp(BinOpNode node)
        {
            var right = EvaluateNode(node.Right);
            if (node.BinOp == BinaryOperation.Assignment)
            {
                if (node.Left is ArrayGetNode)
                {
                    var call = (ArrayGetNode)(node.Left);
                    Dictionary<object, object> theArray = null;
                    var evaluated = GetVariable(call.Target.ToString());
                    if (evaluated is string)
                        theArray = evaluated.ToString().Select((s, i) => new { s, i }).ToDictionary(x => (object)(x.i), x => (object)(x.s));
                    else if (evaluated is Dictionary<object, object>) theArray = ((Dictionary<object, object>)evaluated);
                    else
                    {
                        throw new Exception("The [] operator only applies to objects of type Array or String.");
                    }
                    object arid = null;

                    if (call.Arguments.Children.Count > 0)
                        arid = EvaluateNode(call.Arguments.Children[0]);

                    var theValue = (node.IsOpAssign && arid != null)
                        ? interpretBinaryOp(theArray[arid], right, node.AssignOperation)
                        : right;

                    if (arid == null) theArray.Add(theArray.Count, theValue);
                    else theArray[arid] = theValue;

                    SetVariable(call.Target.ToString(), theArray);
                }
                else
                {
                    if (!(node.Left is IdentifierNode))
                        throw new Exception("Not a valid identifier");
                    if (node.IsOpAssign)
                        SetVariable(node.Left.ToString(),
                            interpretBinaryOp(new BinOpNode(node.AssignOperation, node.Left, node.Right)));
                    SetVariable(node.Left.ToString(), right);
                }
                return right;
            }
            var left = EvaluateNode(node.Left);
            return interpretBinaryOp(left, right, node.IsOpAssign ? node.AssignOperation : node.BinOp);
        }

        /// <summary>
        /// Interprets a binary op
        /// </summary>
        /// <param name="left">The left-hand parameter</param>
        /// <param name="right">The right-hand parameter</param>
        /// <param name="_op">The operation type</param>
        /// <returns>The result of the operation</returns>
        private object interpretBinaryOp(object left, object right, BinaryOperation _op = default(BinaryOperation))
        {
            if (left is AstNode) left = EvaluateNode((AstNode) left);
            if (left is int) left = (double) (int) left; 
            if (right is AstNode) right = EvaluateNode((AstNode)right);
            if (right is int) left = (double)(int)right;
            switch (_op)
            {
                case BinaryOperation.Addition:
                    if (left is string || right is string)
                        return left + right.ToString();
                    return Convert.ToDouble(left) + Convert.ToDouble(right);
                case BinaryOperation.Subtraction:
                    return Convert.ToDouble(left) - Convert.ToDouble(right);
                case BinaryOperation.Division:
                    return Convert.ToDouble(left) / Convert.ToDouble(right);
                case BinaryOperation.Multiplication:
                    if ((left is string && right is double) ||
                        right is string && left is double)
                    {
                        if (left is string)
                            return string.Concat(Enumerable.Repeat(left, Convert.ToInt32(right)));
                        else
                            return string.Concat(Enumerable.Repeat(right, Convert.ToInt32(left)));
                    }
                    return Convert.ToDouble(left) * Convert.ToDouble(right);
                case BinaryOperation.Equals:
                    //if (left is double || right is double) return ((double) left) == ((double) right);
                    return left.ToString() == right.ToString();
                case BinaryOperation.LogicalAnd:
                    return Convert.ToBoolean(left) && Convert.ToBoolean(right);
                case BinaryOperation.LogicalOr:
                    return Convert.ToBoolean(left) || Convert.ToBoolean(right);
                case BinaryOperation.NotEqualTo:
                    return left.GetHashCode() != right.GetHashCode();
                case BinaryOperation.LessThan:
                    return Convert.ToDouble(left) < Convert.ToDouble(right);
                case BinaryOperation.GreaterThan:
                    return Convert.ToDouble(left) > Convert.ToDouble(right);
                case BinaryOperation.GreaterOrEqual:
                    return Convert.ToDouble(left) >= Convert.ToDouble(right);
                case BinaryOperation.LesserOrEqual:
                    return Convert.ToDouble(left) <= Convert.ToDouble(right);
                case BinaryOperation.Xor:
                    return Convert.ToInt32(left) ^ Convert.ToInt32(right);
                case BinaryOperation.BitwiseAnd:
                    return Convert.ToInt32(left) & Convert.ToInt32(right);
                case BinaryOperation.BitwiseOr:
                    return Convert.ToInt32(left) | Convert.ToInt32(right);
                case BinaryOperation.BitshiftLeft:
                    return Convert.ToInt32(left) << Convert.ToInt32(right);
                case BinaryOperation.BitshiftRight:
                    return Convert.ToInt32(left) >> Convert.ToInt32(right);
                case BinaryOperation.Modulus:
                    return Convert.ToDouble(left) % Convert.ToDouble(right);

                case BinaryOperation.Pow:
                    return Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));
                case BinaryOperation.Root:
                    return Math.Pow(Convert.ToDouble(left), 1.0 / Convert.ToDouble(right));
                    
                case BinaryOperation.NullCoalescing:
                    return left ?? right;
            }
            // Raise error
            return -1;
        }
        /// <summary>
        /// Interprets the unary op.
        /// </summary>
        /// <returns>The unary op.</returns>
        /// <param name="node">Node.</param>
        private object interpretUnaryOp(UnaryOpNode node)
        {
            switch (node.UnOp)
            {
                case UnaryOperation.Not:
                    return !Convert.ToBoolean((EvaluateNode(node.Value)));
                case UnaryOperation.Negate:
                    return -Convert.ToDouble((EvaluateNode(node.Value)));
                case UnaryOperation.Complement:
                    return ~(int)Convert.ToDouble((EvaluateNode(node.Value)));
            }
            //Raise error
            return -1;
        }

        private int inLoop = 0;
        private bool continueLoop = false;
        private bool breakLoop = false;

        private int inFunc = 0;
        private bool returnFunc = false;
        /// <summary>
        /// Executes the statement.
        /// </summary>
        /// <param name="node">Node.</param>
        public void ExecuteStatement(AstNode node)
        {
            if (CallStack.Count > 0 && CallStack.Peek().ReturnValue != null)
                return;
            if (node is CodeBlock)
            {
                foreach (var anode in node.Children)
                {
                    ExecuteStatement(anode);
                    if (continueLoop || breakLoop || returnFunc) return;
                }
            }
            else if (node is IfNode)
            {
                var ifStmt = (IfNode) (node);
                if ((bool) (EvaluateNode(ifStmt.Predicate)))
                    ExecuteStatement(ifStmt.Body);
                else
                    ExecuteStatement(ifStmt.ElseBody);
            }
            else if (node is WhileNode)
            {
                var whileStmt = (WhileNode) (node);
                inLoop++;
                if ((bool) (EvaluateNode(whileStmt.Predicate)))
                    while ((bool) EvaluateNode(whileStmt.Predicate))
                    {
                        ExecuteStatement(whileStmt.Body);
                        if (continueLoop) continueLoop = false;
                        if(breakLoop)
                        {
                            breakLoop = false;
                            break;
                        }
                    }
                else
                    ExecuteStatement(whileStmt.ElseBody);
                inLoop--;
            }
            else if (node is FuncNode && !firstExecute)
            {
                var fnode = ((FuncNode)node);
                var stackFrame = new StackFrame(table.ChildScopes[fnode.Name]);
                if (CallStack.Count > 0)
                {
                    stackFrame.Scope.Symbols.AddRange(CallStack.Peek().Scope.Symbols);
                    CallStack.Peek().Locals.All(x =>
                    {
                        stackFrame.Locals.Add(x.Key, x.Value);
                        return true;
                    });
                }
                SetVariable(fnode.Name, new HassiumFunction(this, fnode, stackFrame));
            }
            else if (node is ForNode)
            {
                var forStmt = (ForNode) (node);
                inLoop++;
                ExecuteStatement(forStmt.Left);
                while ((bool) EvaluateNode(forStmt.Predicate))
                {
                    ExecuteStatement(forStmt.Body);
                    if (continueLoop) continueLoop = false;
                    if(breakLoop)
                    {
                        breakLoop = false;
                        break;
                    }
                    ExecuteStatement(forStmt.Right);
                }
                inLoop--;
            }
            else if (node is ForEachNode)
            {
                var forStmt = (ForEachNode) (node);
                var needlestmt = forStmt.Needle;
                var haystackstmt = EvaluateNode(forStmt.Haystack);
                var haystack = haystackstmt as Dictionary<object, object>;
                if (haystackstmt is string)
                    haystack = haystackstmt.ToString()
                        .ToArray()
                        .Select((s, i) => new {s, i})
                        .ToDictionary(x => (object)x.i, x => (object)x.s);
                inLoop++;
                var keyvname = "";
                var valvname = "";
                if (needlestmt is ArrayInitializerNode)
                {
                    keyvname = ((ArrayInitializerNode) needlestmt).Value[0].ToString();
                    valvname = ((ArrayInitializerNode)needlestmt).Value[1].ToString();
                }
                else
                {
                    valvname = needlestmt.ToString();
                }
                if(keyvname != "") SetVariable(keyvname, null);
                SetVariable(valvname, null);
                if (haystack == null)
                    throw new ArgumentException("'" + haystackstmt +
                                                "' is not an array and therefore can not be used in foreach.");

                foreach (var needle in (keyvname != "" ? haystack : (IEnumerable)(haystack.Values)))
                {
                    if (keyvname != "") SetVariable(keyvname, ((KeyValuePair<object, object>)needle).Key);
                    SetVariable(valvname, keyvname != "" ? ((KeyValuePair<object, object>)needle).Value : needle);
                    ExecuteStatement(forStmt.Body);
                    if (continueLoop) continueLoop = false;
                    if(breakLoop)
                    {
                        breakLoop = false;
                        break;
                    }
                }
                if(keyvname != "") FreeVariable(keyvname);
                FreeVariable(valvname);
                inLoop--;
            }
            else if (node is TryNode)
            {
                var tryStmt = (TryNode) (node);
                try
                {
                    ExecuteStatement(tryStmt.Body);
                }
                catch
                {
                    ExecuteStatement(tryStmt.CatchBody);
                }
                finally
                {
                    ExecuteStatement(tryStmt.FinallyBody);
                }
            }
            else if (node is ThreadNode)
            {
                var threadStmt = (ThreadNode) (node);
                Task.Factory.StartNew(() => ExecuteStatement(threadStmt.Node));
            }
            else
            {
                EvaluateNode(node);
                if (continueLoop || breakLoop || returnFunc) return;
            }
        }
        /// <summary>
        /// Evaluates the node.
        /// </summary>
        /// <returns>The node.</returns>
        /// <param name="node">Node.</param>
        public object EvaluateNode(AstNode node)
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
            else if (node is UnaryOpNode)
            {
                return interpretUnaryOp((UnaryOpNode)node);
            }
            else if (node is ConditionalOpNode)
            {
                var ifStmt = (ConditionalOpNode)(node);
                if ((bool)(EvaluateNode(ifStmt.Predicate)))
                    return EvaluateNode(ifStmt.Body);
                else
                    return EvaluateNode(ifStmt.ElseBody);
            }
            else if(node is LambdaFuncNode)
            {
                var funcNode = (LambdaFuncNode)(node);
                var stackFrame = new StackFrame(table.ChildScopes["lambda_" + funcNode.GetHashCode()]);
                if (CallStack.Count > 0)
                {
                    stackFrame.Scope.Symbols.AddRange(CallStack.Peek().Scope.Symbols);
                    CallStack.Peek().Locals.All(x =>
                    {
                        stackFrame.Locals.Add(x.Key, x.Value);
                        return true;
                    });
                }
                return new HassiumFunction(this, (FuncNode)funcNode, stackFrame);
            }
            else if (node is FunctionCallNode)
            {
                var call = (FunctionCallNode)node;

                var target = EvaluateNode(call.Target) as IFunction;
                //if (target is HassiumFunction) ((HassiumFunction) target).stackFrame = null;

                if (target == null)
                    throw new Exception("Attempt to run a non-valid function!");

                var arguments = new object[call.Arguments.Children.Count];
                for (var x = 0; x < call.Arguments.Children.Count; x++)
                {
                    arguments[x] = EvaluateNode(call.Arguments.Children[x]);
                    if (arguments[x] is double && (((double)(arguments[x])) % 1 == 0))
                        arguments[x] = (int)(double)arguments[x];
                }
                inFunc++;
                var ret = target.Invoke(arguments);
                if (returnFunc)
                    returnFunc = false;
                inFunc--;
                if (ret is object[]) ret = ((object[])ret).Select((s, i) => new { s, i }).ToDictionary(x => (object)x.i, x => (object)x.s);
                return ret;
            }
            else if (node is IdentifierNode)
            {
                return GetVariable(((IdentifierNode)node).Identifier);
            }
            else if (node is ArrayInitializerNode)
            {
                return
                    ((ArrayInitializerNode) node).Value.Select(
                        pair =>
                            new KeyValuePair<object, object>(pair.Key is AstNode ? EvaluateNode((AstNode)(pair.Key)) : pair.Key,
                                pair.Value is AstNode ? EvaluateNode((AstNode) (pair.Value)) : pair.Value))
                        .ToDictionary(x => x.Key, x => x.Value);
            }
            else if (node is MentalNode)
            {
                var mnode = ((MentalNode)node);
                if(!HasVariable(mnode.Name)) throw new Exception("The operand of an increment or decrement operator must be a variable, property or indexer");
                var oldValue = GetVariable(mnode.Name);
                switch (mnode.OpType)
                {
                    case "++":
                        SetVariable(mnode.Name, Convert.ToDouble(GetVariable(mnode.Name)) + 1);
                        break;
                    case "--":
                        SetVariable(mnode.Name, Convert.ToDouble(GetVariable(mnode.Name)) - 1);
                        break;
                    default:
                        throw new Exception("Unknown operation " + mnode.OpType);
                }
                return mnode.IsBefore ? GetVariable(mnode.Name) : oldValue;
            }
            else if (node is ArrayGetNode)
            {
                var call = (ArrayGetNode)node;
                Dictionary<object, object> theArray = null;
                var evaluated = GetVariable(call.Target.ToString());
                if (evaluated is string)
                    theArray = evaluated.ToString().Select((s, i) => new {s, i}).ToDictionary(x => (object)(x.i), x => (object)(x.s));
                else if (evaluated is Dictionary<object, object>) theArray = ((Dictionary<object, object>) evaluated);
                else
                {
                    throw new Exception("The [] operator only applies to objects of type Array or String.");
                }
                object arid = null;

                if(call.Arguments.Children.Count > 0)
                    arid = EvaluateNode(call.Arguments.Children[0]);

                object theValue = null;
                if (arid == null) return theArray.Last().Value;
                else theValue = theArray[(object)arid];
                if (theValue is AstNode) return EvaluateNode((AstNode) theValue);
                else return theValue;
            }
            else if (node is ReturnNode)
            {
                if (inFunc == 0) throw new Exception("'return' cannot be used outside a function");
                var returnStmt = (ReturnNode)(node);
                CallStack.Peek().ReturnValue = EvaluateNode(returnStmt.Value);
                returnFunc = true;
            }
            else if (node is ContinueNode)
            {
                if (inLoop == 0) throw new Exception("'continue' cannot be used outside a loop");
                continueLoop = true;
            }
            else if (node is BreakNode)
            {
                if (inLoop == 0) throw new Exception("'break' cannot be used outside a loop");
                breakLoop = true;
            }
            else
            {
                //Raise error
            }

            return 0;
        }
        /// <summary>
        /// Gets the functions.
        /// </summary>
        /// <returns>The functions.</returns>
        /// <param name="path">Path.</param>
        public static Dictionary<string, InternalFunction> GetFunctions(string path = "")
        {
            var result = new Dictionary<string, InternalFunction>();

            var testAss = path == "" ? Assembly.GetExecutingAssembly() : Assembly.LoadFrom(path);

            foreach (var type in testAss.GetTypes())
            {
                if (type.GetInterface(typeof(ILibrary).FullName) != null)
                {
                    if (type.GetMethod("GetFunctions") != null) // TODO: DEPRECATED
                    {
                        var method = type.GetMethod("GetFunctions");
                        var rdict =
                            (Dictionary<string, InternalFunction>)
                                (method.Invoke(Activator.CreateInstance(type, null), null));
                        foreach (var entry in rdict)
                            result.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        foreach (var myfunc in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            var theattr1 = myfunc.GetCustomAttributes(typeof (IntFunc), true);
                            foreach (var theattr in theattr1.OfType<IntFunc>())
                            {
                                var rfunc = new InternalFunction(
                                    (HassiumFunctionDelegate)
                                        Delegate.CreateDelegate(typeof (HassiumFunctionDelegate), myfunc));
                                result.Add(theattr.Name, rfunc);
                                if(theattr.Alias != "") result.Add(theattr.Alias, rfunc);
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}

