/* Credit to contributer Zdimension, who added the lines in interpretBinaryOp for the
implementation of string concat amoung other additions and foreach loop*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hassium.Functions;
using Hassium.HassiumObjects;
using Hassium.HassiumObjects.IO;
using Hassium.HassiumObjects.Math;
using Hassium.HassiumObjects.Types;
using Hassium.Lexer;
using Hassium.Parser;
using Hassium.Parser.Ast;
using Hassium.Semantics;

namespace Hassium.Interpreter
{
    /// <summary>
    /// Interpreter.
    /// </summary>
    public class Interpreter : IVisitor
    {
        public Stack<StackFrame> CallStack = new Stack<StackFrame>();
        public Dictionary<string, HassiumObject> Globals = new Dictionary<string, HassiumObject>();

        public Dictionary<string, HassiumObject> Constants = new Dictionary<string, HassiumObject>
        {
            {"true", new HassiumBool(true)},
            {"false", new HassiumBool(false)},
            {"File", new HassiumFile()},
            {"Directory", new HassiumDirectory()},
            {"Path", new HassiumPath()},
            {"Math", new HassiumMath()},
            {"null", null},
        };

        public AstNode Code { get; set; }

        public SymbolTable SymbolTable { get; set; }



        private bool forceMain;


        /// <summary>
        /// Initializes a new instance of the <see cref="Interpreter"/> class.
        /// </summary>
        public Interpreter(bool forcemain = true)
        {
            SymbolTable = new SymbolTable();
            forceMain = forcemain;
            LoadInternalFunctions();
        }

        public void LoadInternalFunctions()
        {
            foreach (var entry in GetFunctions())
                Globals.Add(entry.Key, entry.Value);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Interpreter"/> class.
        /// </summary>
        /// <param name="symbolTable">Symbol table.</param>
        /// <param name="code">Code.</param>
        /// <param name="forcemain">Enforce main entry point presence</param>
        public Interpreter(SymbolTable symbolTable, AstNode code, bool forcemain = true)
        {
            Code = code;
            SymbolTable = symbolTable;
            forceMain = forcemain;
            LoadInternalFunctions();
        }

        public HassiumObject GetVariable(string name, AstNode node)
        {
            if (Constants.ContainsKey(name))
                return Constants[name];
            if (CallStack.Count > 0 && CallStack.Peek().Locals.ContainsKey(name))
                return CallStack.Peek().Locals[name];
            if (Globals.ContainsKey(name))
                return Globals[name];
            else throw new ParseException("The variable '" + name + "' doesn't exist.", node);
        }

        public bool HasVariable(string name, bool onlyglobal = false)
        {
            return onlyglobal
                ? Globals.ContainsKey(name) || Constants.ContainsKey(name)
                : Globals.ContainsKey(name) || Constants.ContainsKey(name) ||
                  (CallStack.Count > 0 && (CallStack.Peek().Scope.Symbols.Contains(name) || CallStack.Peek().Locals.ContainsKey(name)));
        }

        public void SetGlobalVariable(string name, HassiumObject value, AstNode node)
        {
            if (Constants.ContainsKey(name))
                throw new ParseException("Can't change the value of the internal constant '" + name + "'.", node);

            Globals[name] = value;
        }

        public void SetLocalVariable(string name, HassiumObject value, AstNode node)
        {
            if (Constants.ContainsKey(name))
                throw new ParseException("Can't change the value of the internal constant '" + name + "'.", node);

            if (CallStack.Count > 0)
                CallStack.Peek().Locals[name] = value;
        }

        public void SetVariable(string name, HassiumObject value, AstNode node, bool forceglobal = false, bool onlyexist = false)
        {
            if (!forceglobal && CallStack.Count > 0 && (!onlyexist || (CallStack.Peek().Scope.Symbols.Contains(name) || CallStack.Peek().Locals.ContainsKey(name))) && !Globals.ContainsKey(name))
                SetLocalVariable(name, value, node);
            else
                SetGlobalVariable(name, value, node);
        }

        public void FreeVariable(string name, AstNode node, bool forceglobal = false)
        {
            if(Constants.ContainsKey(name)) throw new ParseException("Can't delete internal constant '" + name + "'.", node);
            if (forceglobal)
            {
                if(!Globals.ContainsKey(name)) throw new ParseException("The global variable '" + name + "' doesn't exist.", node);
                Globals.Remove(name);
            }
            else
            {
                if(!HasVariable(name)) throw new ParseException("The variable '" + name + "' doesn't exist.", node);
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
            foreach (var node in Code.Children)
            {
                if (node is FuncNode && firstExecute)
                {
                    var fnode = ((FuncNode)node);
                    var scope = SymbolTable.ChildScopes[fnode.Name];
                    SetVariable(fnode.Name, new HassiumFunction(this, fnode, scope), node);
                }
            }

            if (!Globals.ContainsKey("main") && forceMain)
            {
                Console.WriteLine("Could not execute, no main entry point of program!");
                Environment.Exit(-1);
            }

            firstExecute = false;
            foreach (var node in Code.Children)
            {
                if (node is FuncNode)
                {
                    var fnode = ((FuncNode)node);
                    var scope = SymbolTable.ChildScopes[fnode.Name];
                    //If there is a main, let it be the main entry point of the program
                    if (fnode.Name == "main")
                    {
                        inFunc++;
                        new HassiumFunction(this, fnode, scope).Invoke();
                        return;
                    }
                }
                else
                    node.Visit(this);
                    //ExecuteStatement(node);
            }
        }

        /// <summary>
        /// Interprets the binary op.
        /// </summary>
        /// <returns>The binary op.</returns>
        /// <param name="node">Node.</param>
        private HassiumObject interpretBinaryOp(BinOpNode node)
        {
            var right = (HassiumObject) node.Right.Visit(this);
            if (node.BinOp == BinaryOperation.Assignment)
            {
                if (node.Left is ArrayGetNode)
                {
                    var call = (ArrayGetNode)(node.Left);

                    if (!call.Target.CanBeIndexed)
                        throw new ParseException("The [] operator only applies to objects of type Array, Dictionary or String.", node);

                    if(!call.Target.CanBeModified)
                        throw new ParseException("The specified target cannot be modified.", node);

                    var evaluated = call.Target.Visit(this);
                    if (evaluated is HassiumDictionary)
                    {
                        var theArray = ((HassiumDictionary) evaluated);
                        HassiumObject arid = null;

                        if (call.Arguments.Children.Count > 0)
                            arid = (HassiumObject) call.Arguments.Children[0].Visit(this);

                        var theValue = (node.IsOpAssign && arid != null)
                            ? interpretBinaryOp(theArray[arid], right, node.AssignOperation)
                            : (HassiumObject)right;

                        if (arid == null)
                            theArray.Value.Add(new HassiumKeyValuePair(theArray.Value.Count, theValue));
                        else
                        {
                            if (theArray.Value.Any(cur => cur.Key.ToString() == arid.ToString()))
                            {
                                foreach (var cur in theArray.Value.Where(cur => cur.Key.ToString() == arid.ToString()))
                                {
                                    theArray.Value[cur.Key].Key = theValue;
                                    break;
                                }
                            }
                            else
                            {
                                theArray[arid] = theValue;
                            }
                        }

                        SetVariable(call.Target.ToString(), theArray, call);
                    }
                    else if (evaluated is HassiumArray || evaluated is HassiumString)
                    {
                        HassiumArray theArray = null;
                        if(evaluated is HassiumString)
                        {
                            theArray = new HassiumArray(evaluated.ToString().ToCharArray().Cast<object>());
                        }
                        theArray = ((HassiumArray) evaluated);

                        int arid = -1;
                        bool append = false;

                        if (call.Arguments.Children.Count > 0)
                            arid = (int) call.Arguments.Children[0].Visit(this);
                        else
                            append = true;

                        var theValue = node.IsOpAssign
                            ? interpretBinaryOp(theArray[(int)arid], right, node.AssignOperation)
                            : (HassiumObject) right;

                        if(append)
                            theArray.Add(new[] {theValue});
                        else
                        {
                            if(arid >= theArray.Value.Length)
                                throw new IndexOutOfRangeException();

                            theArray[arid] = theValue;
                        }

                        SetVariable(call.Target.ToString(), theArray, call);
                    }
                    else
                    {
                        throw new ParseException("The [] operator only applies to objects of type Array, Dictionary or String.", node);
                    }
                    
                }
                else if (node.Left is MemberAccessNode)
                {
                    var accessor = (MemberAccessNode) node.Left;
                    var target = (HassiumObject) accessor.Left.Visit(this);
                    target.SetAttribute(accessor.Member, right);
                }
                else
                {
                    if (!(node.Left is IdentifierNode))
                        throw new ParseException("Not a valid identifier", node);
                    SetVariable(node.Left.ToString(),
                        node.IsOpAssign
                            ? interpretBinaryOp(new BinOpNode(node.Position, node.AssignOperation, node.Left, node.Right))
                            : right, node);
                }
                return right;
            }
            var left = node.Left.Visit(this);
            return interpretBinaryOp(left, right, node.IsOpAssign ? node.AssignOperation : node.BinOp);
        }

        /// <summary>
        /// Interprets a binary op
        /// </summary>
        /// <param name="left">The left-hand parameter</param>
        /// <param name="right">The right-hand parameter</param>
        /// <param name="_op">The operation type</param>
        /// <returns>The result of the operation</returns>
        private HassiumObject interpretBinaryOp(object left, object right, BinaryOperation _op)
        {
            if (left is AstNode) left = ((AstNode) left).Visit(this);
            if (left is int) left = (double) (int) left; 
            if (right is AstNode) right = ((AstNode)right).Visit(this);
            if (right is int) right = (double)(int)right;
            switch (_op)
            {
                case BinaryOperation.Addition:
                    if (left is HassiumString || right is HassiumString)
                        return new HassiumString(left + right.ToString());
                    if (left is HassiumDate || right is HassiumDate)
                        return (HassiumDate) left + (HassiumDate) right;
                    if (left is HassiumKeyValuePair || right is HassiumKeyValuePair)
                        return new HassiumString(left + right.ToString());
                    return new HassiumNumber(Convert.ToDouble(left) + Convert.ToDouble(right));
                case BinaryOperation.Subtraction:
                    return new HassiumNumber(Convert.ToDouble(left) - Convert.ToDouble(right));
                case BinaryOperation.Division:
                    if(Convert.ToDouble(right) == 0.0) throw new DivideByZeroException("Cannot divide by zero");
                    return new HassiumNumber(Convert.ToDouble(left) / Convert.ToDouble(right));
                case BinaryOperation.Multiplication:
                    if ((left is HassiumString && right is HassiumNumber) ||
                        right is HassiumString && left is HassiumNumber)
                    {
                        if (left is HassiumString)
                            return new HassiumString(string.Concat(Enumerable.Repeat(left, Convert.ToInt32(right))));
                        else
                            return new HassiumString(string.Concat(Enumerable.Repeat(right, Convert.ToInt32(left))));
                    }
                    return new HassiumNumber(Convert.ToDouble(left) * Convert.ToDouble(right));
                case BinaryOperation.Equals:
                    return new HassiumBool(left.ToString() == right.ToString());
                case BinaryOperation.LogicalAnd:
                    return new HassiumBool(Convert.ToBoolean(left) && Convert.ToBoolean(right));
                case BinaryOperation.LogicalOr:
                    return new HassiumBool(Convert.ToBoolean(left) || Convert.ToBoolean(right));
                case BinaryOperation.NotEqualTo:
                    return new HassiumBool(left.GetHashCode() != right.GetHashCode());
                case BinaryOperation.LessThan:
                    return new HassiumBool(Convert.ToDouble(left) < Convert.ToDouble(right));
                case BinaryOperation.GreaterThan:
                    return new HassiumBool(Convert.ToDouble(left) > Convert.ToDouble(right));
                case BinaryOperation.GreaterOrEqual:
                    return new HassiumBool(Convert.ToDouble(left) >= Convert.ToDouble(right));
                case BinaryOperation.LesserOrEqual:
                    return new HassiumBool(Convert.ToDouble(left) <= Convert.ToDouble(right));
                case BinaryOperation.CombinedComparison:
                    if (new HassiumBool(interpretBinaryOp(left, right, BinaryOperation.GreaterThan))) return new HassiumNumber(1);
                    return new HassiumBool(interpretBinaryOp(left, right, BinaryOperation.LessThan)) ? new HassiumNumber(-1) : new HassiumNumber(0);
                case BinaryOperation.Xor:
                    return new HassiumNumber(Convert.ToInt32(left) ^ Convert.ToInt32(right));
                case BinaryOperation.BitwiseAnd:
                    return new HassiumNumber(Convert.ToInt32(left) & Convert.ToInt32(right));
                case BinaryOperation.BitwiseOr:
                    return new HassiumNumber(Convert.ToInt32(left) | Convert.ToInt32(right));
                case BinaryOperation.BitshiftLeft:
                    return new HassiumNumber(Convert.ToInt32(left) << Convert.ToInt32(right));
                case BinaryOperation.BitshiftRight:
                    return new HassiumNumber(Convert.ToInt32(left) >> Convert.ToInt32(right));
                case BinaryOperation.Modulus:
                    return new HassiumNumber(Convert.ToDouble(left) % Convert.ToDouble(right));

                case BinaryOperation.Pow:
                    return new HassiumNumber(Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right)));
                case BinaryOperation.Root:
                    return new HassiumNumber(Math.Pow(Convert.ToDouble(left), 1.0 / Convert.ToDouble(right)));
                    
                case BinaryOperation.NullCoalescing:
                    return HassiumObject.ToHassiumObject(left) ?? HassiumObject.ToHassiumObject(right);
            }
            // Raise error
            return new HassiumNumber(-1);
        }
        /// <summary>
        /// Interprets the unary op.
        /// </summary>
        /// <returns>The unary op.</returns>
        /// <param name="node">Node.</param>
        private HassiumObject interpretUnaryOp(UnaryOpNode node)
        {
            switch (node.UnOp)
            {
                case UnaryOperation.Not:
                    return !Convert.ToBoolean((object)node.Value.Visit(this));
                case UnaryOperation.Negate:
                    return -Convert.ToDouble((object)node.Value.Visit(this));
                case UnaryOperation.Complement:
                    return ~(int)Convert.ToDouble((object)node.Value.Visit(this));
            }
            //Raise error
            return -1;
        }

        private int inLoop;
        private bool continueLoop;
        private bool breakLoop;

        private int inFunc;
        private bool returnFunc;



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
                if (type.GetInterface(typeof (ILibrary).FullName) != null)
                {
                    foreach (var myfunc in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        var theattr1 = myfunc.GetCustomAttributes(typeof (IntFunc), true);
                        foreach (var theattr in theattr1.OfType<IntFunc>())
                        {
                            var rfunc = new InternalFunction(
                                (HassiumFunctionDelegate)
                                    Delegate.CreateDelegate(typeof (HassiumFunctionDelegate), myfunc), false, theattr.Constructor);

                            result.Add(theattr.Name, rfunc);
                            if (theattr.Alias != "") result.Add(theattr.Alias, rfunc);
                        }
                    }
                }
            }
            return result;
        }

        public object Accept(Expression expr)
        {
            VisitSubnodes(expr);
            return null;
        }

        public object Accept(ArgListNode node)
        {
            var arguments = new HassiumObject[node.Children.Count];
            for (var x = 0; x < node.Children.Count; x++)
            {
                arguments[x] = (HassiumObject)node.Children[x].Visit(this);
            }
            return arguments;
        }

        public object Accept(ArrayGetNode node)
        {
            var call = (ArrayGetNode)(node);

            if (!call.Target.CanBeIndexed)
                throw new ParseException("The [] operator only applies to objects of type Array, Dictionary or String.", node);

            var evaluated = (HassiumObject)call.Target.Visit(this);
            if (evaluated is HassiumDictionary)
            {
                var theArray = ((HassiumDictionary)evaluated);
                HassiumObject arid = null;

                if (call.Arguments.Children.Count > 0)
                    arid = (HassiumObject)call.Arguments.Children[0].Visit(this);

                if (arid == null)
                    return theArray.Value.Last().Value;
                else
                {
                    return theArray.Value.Any(cur => cur.Key.ToString() == arid.ToString()) ? theArray.Value.First(cur => cur.Key.ToString() == arid.ToString()).Value : theArray[arid];
                }
            }
            else if (evaluated is HassiumArray || evaluated is HassiumString)
            {
                HassiumArray theArray = null;
                if (evaluated is HassiumString)
                {
                    theArray = new HassiumArray(evaluated.ToString().ToCharArray().Cast<object>());
                }
                theArray = ((HassiumArray)evaluated);

                int arid = -1;
                bool append = false;

                if (call.Arguments.Children.Count > 0)
                    arid = (HassiumObject)call.Arguments.Children[0].Visit(this);
                else
                    append = true;

                if (append)
                    return theArray.Value.Last();
                else
                {
                    if (arid >= theArray.Value.Length)
                        throw new IndexOutOfRangeException();

                    return theArray[arid];
                }
            }
            else
            {
                throw new ParseException("The [] operator only applies to objects of type Array, Dictionary or String.", node);
            }
        }

        public object Accept(ArrayIndexerNode node)
        {
            return null;
        }

        public object Accept(ArrayInitializerNode node)
        {
            var ainode = ((ArrayInitializerNode)node);
            var content = ainode.Value;
            if (ainode.IsDictionary)
                return new HassiumDictionary(content.Select(
                    pair =>
                        new KeyValuePair<HassiumObject, HassiumObject>(
                            pair.Key is AstNode
                                ? (HassiumObject)((AstNode)(pair.Key)).Visit(this)
                                : HassiumObject.ToHassiumObject(pair.Key),
                            pair.Value is AstNode
                                ? (HassiumObject)((AstNode)(pair.Value)).Visit(this)
                                : HassiumObject.ToHassiumObject(pair.Value)))
                    .ToDictionary(x => x.Key, x => x.Value));
            else
                return new HassiumArray(content.Values.Select(x => x is AstNode ? (HassiumObject)((AstNode)x).Visit(this) : x));
        }

        public object Accept(BinOpNode node)
        {
            var bnode = (BinOpNode)node;
            return interpretBinaryOp(bnode);
        }

        public object Accept(BreakNode node)
        {
            if (inLoop == 0) throw new ParseException("'break' cannot be used outside a loop", node);
            breakLoop = true;
            return null;
        }

        public object Accept(CaseNode node)
        {
            node.Body.Visit(this);
            return null;
        }

        public object Accept(ClassNode node)
        {
            throw new NotImplementedException();
        }

        public object Accept(CodeBlock node)
        {
            VisitSubnodes(node);
            return null;
        }

        public object Accept(ConditionalOpNode node)
        {
            var ifStmt = (ConditionalOpNode)(node);
            if ((HassiumBool)(ifStmt.Predicate.Visit(this)))
            {
                return ifStmt.Body.Visit(this);
            }
            else
            {
                return ifStmt.ElseBody.Visit(this);
            }
        }

        public object Accept(ContinueNode node)
        {
            if (inLoop == 0) throw new ParseException("'continue' cannot be used outside a loop", node);
            continueLoop = true;
            return null;
        }

        public object Accept(ForEachNode node)
        {
            var forStmt = (ForEachNode)(node);
            var needlestmt = forStmt.Needle;
            var haystackstmt = forStmt.Haystack.Visit(this);

            inLoop++;
            if (haystackstmt is HassiumDictionary)
            {
                var theArray = ((HassiumDictionary)haystackstmt);

                var keyvname = "";
                var valvname = "";
                if (needlestmt is ArrayInitializerNode)
                {
                    keyvname = ((ArrayInitializerNode)needlestmt).Value[0].ToString();
                    valvname = ((ArrayInitializerNode)needlestmt).Value[1].ToString();
                }
                else
                {
                    valvname = needlestmt.ToString();
                }
                if (keyvname != "") SetVariable(keyvname, null, forStmt);
                SetVariable(valvname, null, forStmt);
                foreach (var needle in (keyvname != "" ? theArray : (IEnumerable)(theArray.Value.Select(x => x.Value))))
                {
                    if (keyvname != "") SetVariable(keyvname, ((HassiumKeyValuePair)needle).Key, forStmt);
                    SetVariable(valvname, keyvname != "" ? ((HassiumKeyValuePair)needle).Value : HassiumObject.ToHassiumObject(needle), forStmt);
                    forStmt.Body.Visit(this);
                    if (continueLoop) continueLoop = false;
                    if (breakLoop)
                    {
                        breakLoop = false;
                        break;
                    }
                }
                if (keyvname != "") FreeVariable(keyvname, forStmt);
                FreeVariable(valvname, forStmt);
                inLoop--;
            }
            else if (haystackstmt is HassiumArray || haystackstmt is HassiumString)
            {
                HassiumArray theArray = null;
                if (haystackstmt is HassiumString)
                {
                    theArray = new HassiumArray(haystackstmt.ToString().ToCharArray().Cast<object>());
                }
                theArray = ((HassiumArray)haystackstmt);

                var valvname = needlestmt.ToString();

                SetVariable(valvname, null, forStmt);
                foreach (var needle in theArray.Value)
                {
                    SetVariable(valvname, HassiumObject.ToHassiumObject(needle), forStmt);
                    forStmt.Body.Visit(this);
                    if (continueLoop) continueLoop = false;
                    if (breakLoop)
                    {
                        breakLoop = false;
                        break;
                    }
                }
                FreeVariable(valvname, forStmt);
                inLoop--;
            }
            else
            {
                inLoop--;
                throw new ParseException("Foreach can only be used with objects of type Array, Dictionary or String.", node);
            }
            return null;
        }

        public object Accept(ForNode node)
        {
            var forStmt = (ForNode)(node);
            inLoop++;
            forStmt.Left.Visit(this);
            while ((HassiumBool) (forStmt.Predicate.Visit(this)))
            {
                forStmt.Body.Visit(this);
                if (continueLoop) continueLoop = false;
                if (breakLoop)
                {
                    breakLoop = false;
                    break;
                }
                forStmt.Right.Visit(this);
            }
            inLoop--;
            return null;
        }

        public object Accept(FuncNode node)
        {
            var fnode = ((FuncNode)node);
            var stackFrame = new StackFrame(SymbolTable.ChildScopes[fnode.Name]);
            if (CallStack.Count > 0)
            {
                stackFrame.Scope.Symbols.AddRange(CallStack.Peek().Scope.Symbols);
                CallStack.Peek().Locals.All(x =>
                {
                    stackFrame.Locals.Add(x.Key, x.Value);
                    return true;
                });
            }
            var hfunc = new HassiumFunction(this, fnode, stackFrame);
            SetVariable(fnode.Name, hfunc, fnode);
            return hfunc;
        }

        public object Accept(FunctionCallNode node)
        {
            var call = (FunctionCallNode)node;

            IFunction target = null;


            bool dontEval = false;

            switch (call.Target.ToString())
            {
                case "free":
                    dontEval = true;
                    target = new InternalFunction(args =>
                    {
                        FreeVariable(args[0].ToString(), node);
                        return null;
                    });
                    break;
                default:
                    if ((!(call.Target is MemberAccessNode) && !HasVariable(call.Target.ToString())))
                    {
                        throw new ParseException("Attempt to run a non-valid function", node);
                    }
                    target = (HassiumObject) call.Target.Visit(this);
                    break;
            }


            if (target is InternalFunction && (target as InternalFunction).IsConstructor)
                throw new ParseException("Attempt to run a constructor without the 'new' operator", node);

            var arguments = new HassiumObject[call.Arguments.Children.Count];
            for (var x = 0; x < call.Arguments.Children.Count; x++)
            {
                arguments[x] = dontEval ? new HassiumString(call.Arguments.Children[x].ToString()) : (HassiumObject)call.Arguments.Children[x].Visit(this);
            }
            inFunc++;
            HassiumObject ret = target.Invoke(arguments);
            if (returnFunc)
                returnFunc = false;
            inFunc--;
            if (ret is HassiumArray) ret = ((Array)ret).Cast<HassiumObject>().Select((s, i) => new { s, i }).ToDictionary(x => HassiumObject.ToHassiumObject(x.i), x => HassiumObject.ToHassiumObject(x.s));
            return ret;
        }

        public object Accept(IdentifierNode node)
        {
            return GetVariable(node.Identifier, node);
        }

        public object Accept(IfNode node)
        {
            var ifStmt = (IfNode)(node);
            if ((HassiumBool)(ifStmt.Predicate.Visit(this)))
            {
                ifStmt.Body.Visit(this);
            }
            else
            {
                ifStmt.ElseBody.Visit(this);
            }
            return null;
        }

        public object Accept(InstanceNode node)
        {
            var inode = (InstanceNode)node;
            var fcall = (FunctionCallNode)inode.Target;
            var target = fcall.Target.ToString();

            var arguments = (HassiumObject[]) fcall.Arguments.Visit(this);
            if (HasVariable(target, true))
            {
                var theVar = Globals[target];

                var iFunc = theVar as InternalFunction;
                if (iFunc != null)
                {
                    if (iFunc.IsConstructor)
                    {
                        return iFunc.Invoke(arguments);
                    }
                }
            }
            throw new ParseException("No constructor found for " + target, node);
        }

        public object Accept(LambdaFuncNode node)
        {
            var funcNode = (LambdaFuncNode)(node);
            var stackFrame = new StackFrame(SymbolTable.ChildScopes["lambda_" + funcNode.GetHashCode()]);
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

        public object Accept(MemberAccessNode node)
        {
            var accessor = (MemberAccessNode)node;
            var target = (HassiumObject)accessor.Left.Visit(this);
            var attr = target.GetAttribute(accessor.Member);
            if (attr is InternalFunction && ((InternalFunction)attr).IsProperty)
            {
                return ((InternalFunction)attr).Invoke();
            }
            else
            {
                return attr;
            }
        }

        public object Accept(MentalNode node)
        {
            var mnode = ((MentalNode)node);
            if (!HasVariable(mnode.Name)) throw new ParseException("The operand of an increment or decrement operator must be a variable, property or indexer", mnode);
            var oldValue = GetVariable(mnode.Name, mnode);
            switch (mnode.OpType)
            {
                case "++":
                    SetVariable(mnode.Name, Convert.ToDouble((object)GetVariable(mnode.Name, mnode)) + 1, mnode);
                    break;
                case "--":
                    SetVariable(mnode.Name, Convert.ToDouble((object)GetVariable(mnode.Name, mnode)) - 1, mnode);
                    break;
                default:
                    throw new ParseException("Unknown operation " + mnode.OpType, mnode);
            }
            return mnode.IsBefore ? GetVariable(mnode.Name, mnode) : oldValue;
        }

        public object Accept(NumberNode node)
        {
            return new HassiumNumber(node.Value);
        }

        public object Accept(ReturnNode node)
        {
            if (inFunc == 0) throw new ParseException("'return' cannot be used outside a function", node);
            var returnStmt = (ReturnNode)(node);
            if (returnStmt.Value != null && !returnStmt.Value.ReturnsValue) throw new ParseException("This node type doesn't return a value.", returnStmt.Value);
            var ret = returnStmt.Value.Visit(this);
            returnFunc = true;
            CallStack.Peek().ReturnValue = (HassiumObject)ret;
            return ret;
        }

        public object Accept(StatementNode node)
        {
            return node.Visit(this);
        }

        public object Accept(StringNode node)
        {
            return new HassiumString(node.Value);
        }

        public object Accept(SwitchNode node)
        {
            var pred = node.Predicate.Visit(this);
            if (node.Body.Any(x => x.Values.Any(y => y.Visit(this).ToString() == pred.ToString())))
            {
                var cnode = node.Body.First(x => x.Values.Any(y => y.Visit(this).ToString() == pred.ToString()));
                cnode.Visit(this);
            }
            else
            {
                if (node.DefaultBody != null)
                {
                    node.DefaultBody.Visit(this);
                }
            }
            return null;
        }

        public object Accept(ThreadNode node)
        {
            var threadStmt = (ThreadNode)(node);
            Task.Factory.StartNew(() => threadStmt.Node.Visit(this));
            return null;
        }

        public object Accept(TryNode node)
        {
            var tryStmt = (TryNode)(node);
            try
            {
                tryStmt.Body.Visit(this);
            }
            catch
            {
                tryStmt.CatchBody.Visit(this);
            }
            finally
            {
                if(tryStmt.FinallyBody != null)
                    tryStmt.FinallyBody.Visit(this);
            }
            return null;
        }

        public object Accept(UnaryOpNode node)
        {
            return interpretUnaryOp((UnaryOpNode)node);
        }

        public object Accept(WhileNode node)
        {
            var whileStmt = (WhileNode)(node);
            inLoop++;
            if ((HassiumBool) whileStmt.Predicate.Visit(this))
                while ((HassiumBool) whileStmt.Predicate.Visit(this))
                {
                    whileStmt.Body.Visit(this);
                    if (continueLoop) continueLoop = false;
                    if (breakLoop)
                    {
                        breakLoop = false;
                        break;
                    }
                }
            else
            {
                if(whileStmt.ElseBody != null)
                    whileStmt.ElseBody.Visit(this);
            }
            inLoop--;
            return null;
        }

        private void VisitSubnodes(AstNode node)
        {
            foreach(var nd in node.Children)
            {
                nd.Visit(this);
                if (continueLoop || breakLoop || returnFunc) break;
            }
        }
    }
}

