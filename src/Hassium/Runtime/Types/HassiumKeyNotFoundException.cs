﻿using Hassium.Compiler;

using System.Collections.Generic;
using System.Text;

namespace Hassium.Runtime.Types
{
    public class HassiumKeyNotFoundException : HassiumObject
    {
        public static new HassiumTypeDefinition TypeDefinition = new KeyNotFoundExceptionTypeDef();

        public HassiumObject Key { get; private set; }
        public HassiumObject Object { get; private set; }

        public HassiumKeyNotFoundException()
        {
            AddType(TypeDefinition);
        }

        public class KeyNotFoundExceptionTypeDef : HassiumTypeDefinition
        {
            public KeyNotFoundExceptionTypeDef() : base("KeyNotFoundException")
            {
                BoundAttributes = new Dictionary<string, HassiumObject>()
                {
                    { INVOKE, new HassiumFunction(_new, 2) },
                    { "key", new HassiumProperty(get_key) },
                    { "message", new HassiumProperty(get_message) },
                    { TOSTRING, new HassiumFunction(tostring, 0) }
                };
            }
            [FunctionAttribute("func new (obj : object, key : object) : KeyNotFoundException")]
            public static HassiumKeyNotFoundException _new(VirtualMachine vm, HassiumObject self, SourceLocation location, params HassiumObject[] args)
            {
                HassiumKeyNotFoundException exception = new HassiumKeyNotFoundException();

                exception.Object = args[0];
                exception.Key = args[1];

                return exception;
            }

            [FunctionAttribute("key { get; }")]
            public static HassiumObject get_key(VirtualMachine vm, HassiumObject self, SourceLocation location, params HassiumObject[] args)
            {
                return (self as HassiumKeyNotFoundException).Key;
            }

            [FunctionAttribute("message { get; }")]
            public static HassiumString get_message(VirtualMachine vm, HassiumObject self, SourceLocation location, params HassiumObject[] args)
            {
                var exception = (self as HassiumKeyNotFoundException);
                return new HassiumString(string.Format("Key Not Found Error: Could not find key '{0}' in object of type '{1}'", exception.Key.ToString(vm, exception.Key, location).String, exception.Object.Type()));
            }

            [FunctionAttribute("object { get; }")]
            public static HassiumObject get_object(VirtualMachine vm, HassiumObject self, SourceLocation location, params HassiumObject[] args)
            {
                return (self as HassiumKeyNotFoundException).Object;
            }

            [FunctionAttribute("func tostring () : string")]
            public static HassiumString tostring(VirtualMachine vm, HassiumObject self, SourceLocation location, params HassiumObject[] args)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(get_message(vm, self, location).String);
                sb.Append(vm.UnwindCallStack());

                return new HassiumString(sb.ToString());
            }
        }

        public override bool ContainsAttribute(string attrib)
        {
            return BoundAttributes.ContainsKey(attrib) || TypeDefinition.BoundAttributes.ContainsKey(attrib);
        }

        public override HassiumObject GetAttribute(VirtualMachine vm, string attrib)
        {
            if (BoundAttributes.ContainsKey(attrib))
                return BoundAttributes[attrib];
            else
                return (TypeDefinition.BoundAttributes[attrib].Clone() as HassiumObject).SetSelfReference(this);
        }

        public override Dictionary<string, HassiumObject> GetAttributes()
        {
            foreach (var pair in TypeDefinition.BoundAttributes)
                if (!BoundAttributes.ContainsKey(pair.Key))
                    BoundAttributes.Add(pair.Key, (pair.Value.Clone() as HassiumObject).SetSelfReference(this));
            return BoundAttributes;
        }
    }
}
