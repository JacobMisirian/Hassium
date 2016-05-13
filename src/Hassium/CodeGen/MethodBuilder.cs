using System;
using System.Collections.Generic;

using Hassium.Parser;
using Hassium.Runtime;
using Hassium.Runtime.StandardLibrary;
using Hassium.Runtime.StandardLibrary.Types;

namespace Hassium.CodeGen
{
    public class MethodBuilder: HassiumObject
    {
        public HassiumClass Parent { get; set; }
        public string Name { get { return name; } set { name = value; Types.Add("func"); } }
        private string name = "";

        public Dictionary<FuncNode.Parameter, int> Parameters = new Dictionary<FuncNode.Parameter, int>();
        public List<Instruction> Instructions = new List<Instruction>();

        public Dictionary<double, int> Labels = new Dictionary<double, int>();
        public Stack<double> BreakLabels = new Stack<double>();
        public Stack<double> ContinueLabels = new Stack<double>();

        public string SourceRepresentation { get; set; }
        public bool IsConstructor { get { return Name == "new"; } }


        public override HassiumObject Invoke(VirtualMachine vm, HassiumObject[] args)
        {
            if (name != "__lambda__" && name != "__catch__")
            vm.StackFrame.EnterFrame();

            vm.CallStack.Push(SourceRepresentation != null ? SourceRepresentation : Name);
            int counter = 0;
            foreach (KeyValuePair<FuncNode.Parameter, int> param in Parameters)
            {
                HassiumObject argument = args[counter++];
                if (param.Key.IsEnforced)
                {
                    if (!argument.Types.Contains(param.Key.Type))
                        throw new InternalException(string.Format("Expected type {0} to {1}, instead got {2}!", param.Key.Type, SourceRepresentation, argument.Type()));
                }
                
                vm.StackFrame.Add(param.Value, argument);
            }
            HassiumObject returnValue = vm.ExecuteMethod(this);
            if (name != "__lambda__" && name != "__catch__")
            vm.StackFrame.PopFrame();

            if (IsConstructor)
            {
                HassiumClass ret = new HassiumClass();
                ret.Attributes = CloneDictionary(Parent.Attributes);
                ret.Types.Add(Parent.Name);
                foreach (HassiumObject obj in ret.Attributes.Values)
                    if (obj is MethodBuilder)
                        ((MethodBuilder)obj).Parent = ret;
                return ret;
            }
            vm.CallStack.Pop();
            return returnValue;
        }

        public static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>
            (Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                                        original.Comparer);
            foreach (KeyValuePair<TKey, TValue> entry in original)
            {
                ret.Add(entry.Key, (TValue) entry.Value.Clone());
            }
            return ret;
        }


        public void Emit(SourceLocation location, InstructionType instructionType, double value = 0)
        {
            Instructions.Add(new Instruction(instructionType, value, location));
        }
    }
}