// Copyright (c) 2015, HassiumTeam (JacobMisirian, zdimension) All rights reserved.
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//  * Redistributions of source code must retain the above copyright notice, this list
//    of conditions and the following disclaimer.
// 
//  * Redistributions in binary form must reproduce the above copyright notice, this
//    list of conditions and the following disclaimer in the documentation and/or
//    other materials provided with the distribution.
// Neither the name of the copyright holder nor the names of its contributors may be
// used to endorse or promote products derived from this software without specific
// prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
// DAMAGE.

using System.Collections.Generic;
using Hassium.Interpreter;

namespace Hassium.Parser.Ast
{
    public class FuncNode : AstNode
    {
        public string Name { get; private set; }
        public List<string> Parameters { get; private set; }
        public FunctionCallNode CallConstructor { get; private set; }
        public bool InfParams { get; private set; }

        public AstNode Body
        {
            get { return Children[0]; }
        }

        public FuncNode(int position, string name, List<string> parameters, AstNode body,
            FunctionCallNode constructor = null, bool parms = false) : base(position)
        {
            Parameters = parameters;
            Name = name;
            CallConstructor = constructor;
            Children.Add(body);
            InfParams = parms;
        }

        public static explicit operator LambdaFuncNode(FuncNode funcNode)
        {
            return new LambdaFuncNode(funcNode.Position, funcNode.Parameters, funcNode.Body);
        }

        public override object Visit(IVisitor visitor)
        {
            return visitor.Accept(this);
        }
    }
}