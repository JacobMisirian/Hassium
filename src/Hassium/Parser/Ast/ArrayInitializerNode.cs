﻿// Copyright (c) 2015, HassiumTeam (JacobMisirian, zdimension) All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;
using Hassium.Interpreter;

namespace Hassium.Parser.Ast
{
    [Serializable]
    public class ArrayInitializerNode : AstNode
    {
        private readonly Dictionary<object, object> _value;

        public Dictionary<object, object> Value
        {
            get { return _value; }
        }

        public bool IsDictionary { get; set; }

        public ArrayInitializerNode(int position, Dictionary<object, object> items) : base(position)
        {
            _value = items;
            _value.All(x =>
            {
                Children.Add((AstNode) x.Key);
                Children.Add((AstNode) x.Value);
                return true;
            });
        }

        public override string ToString()
        {
            return "[ArrayInitializer { " + Children.Select(x => x.ToString() + " ") + "} ]";
        }

        public ArrayInitializerNode(int position) : this(position, new Dictionary<object, object>())
        {
        }

        public void AddItem(object item)
        {
            AddItem(_value.Count, item);
        }

        public void AddItem(object key, object item)
        {
            _value.Add(key, item);
            Children.Add(key is int ? new NumberNode(-1, (int) key, true) : (AstNode) key);
            Children.Add((AstNode) item);
        }

        public override object Visit(IVisitor visitor)
        {
            return visitor.Accept(this);
        }
    }
}