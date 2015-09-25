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

using System.Linq;
using Hassium.HassiumObjects.Types;

namespace Hassium.HassiumObjects.Interpreter
{
    public class HassiumDebug : HassiumObject
    {
        public HassiumDebug()
        {
            Attributes.Add("localVariables",
                new HassiumProperty("localVariables", x => localVariables(), null, true));
            Attributes.Add("globalVariables",
                new HassiumProperty("globalVariables", x => globalVariables(), null, true));
            Attributes.Add("fileName",
                new HassiumProperty("fileName", x => Program.options.FilePath, null, true));
            Attributes.Add("secureMode",
                new HassiumProperty("secureMode", x => Program.options.Secure, null, true));
            Attributes.Add("sourceCode",
                new HassiumProperty("sourceCode", x => Program.options.Code, null, true));
        }

        public HassiumObject localVariables()
        {
            return
                new HassiumDictionary(Program.CurrentInterpreter.CallStack.GetLocals(true));
        }

        public HassiumObject globalVariables()
        {
            var result = Program.CurrentInterpreter.Globals;
            Program.CurrentInterpreter.Constants.All(x =>
            {
                result.Add(x.Key, x.Value);
                return true;
            });
            return new HassiumDictionary(result.ToDictionary(x => new HassiumString(x.Key), x => x.Value));
        }
    }
}