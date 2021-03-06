﻿// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using Microsoft.Python.Analysis.Analyzer.Evaluation;
using Microsoft.Python.Analysis.Modules;
using Microsoft.Python.Analysis.Types;
using Microsoft.Python.Analysis.Values;
using Microsoft.Python.Core.Logging;
using Microsoft.Python.Parsing.Ast;

namespace Microsoft.Python.Analysis.Analyzer.Handlers {
    internal abstract class StatementHandler {
        protected AnalysisWalker Walker { get; }
        protected ExpressionEval Eval => Walker.Eval;
        protected IPythonModule Module => Eval.Module;
        protected ILogger Log => Eval.Log;
        protected IPythonInterpreter Interpreter => Eval.Interpreter;
        protected PythonAst Ast => Eval.Ast;

        protected IModuleResolution ModuleResolution
            => Module.ModuleType == ModuleType.Stub
                ? Module.Interpreter.TypeshedResolution
                : Module.Interpreter.ModuleResolution;

        protected StatementHandler(AnalysisWalker walker) {
            Walker = walker;
        }

        protected void AssignVariable(NameExpression ne, IMember value) {
            IScope scope;
            if (Eval.CurrentScope.NonLocals[ne.Name] != null) {
                Eval.LookupNameInScopes(ne.Name, out scope, LookupOptions.Nonlocal);
                scope?.Variables[ne.Name].Assign(value, Eval.GetLocationOfName(ne));
                return;
            }

            if (Eval.CurrentScope.Globals[ne.Name] != null) {
                Eval.LookupNameInScopes(ne.Name, out scope, LookupOptions.Global);
                scope?.Variables[ne.Name].Assign(value, Eval.GetLocationOfName(ne));
                return;
            }

            var source = value.IsGeneric() ? VariableSource.Generic : VariableSource.Declaration;
            var location = Eval.GetLocationOfName(ne);
            if (IsValidAssignment(ne.Name, location)) {
                Eval.DeclareVariable(ne.Name, value ?? Module.Interpreter.UnknownType, source, location);
            }
        }
        private bool IsValidAssignment(string name, Location loc) => !Eval.GetInScope(name).IsDeclaredAfter(loc);
    }
}
