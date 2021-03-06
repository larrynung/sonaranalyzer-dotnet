﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2016 SonarSource SA
 * mailto:contact@sonarsource.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Common.Sqale;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SqaleConstantRemediation("5min")]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.DataReliability)]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    [Tags(Tag.Bug, Tag.Performance)]
    public class OrderByRepeated : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3169";
        internal const string Title = "Multiple \"OrderBy\" calls should not be used";
        internal const string Description =
            "There's no point in chaining multiple \"OrderBy\" calls in a LINQ; only the last one will be reflected in the result " +
            "because each subsequent call completely reorders the list. Thus, calling \"OrderBy\" multiple times is a performance " +
            "issue as well, because all of the sorting will be executed, but only the result of the last sort will be kept.";
        internal const string MessageFormat = "Use \"ThenBy\" instead.";
        internal const string Category = SonarAnalyzer.Common.Category.Performance;
        internal const Severity RuleSeverity = Severity.Critical;
        internal const bool IsActivatedByDefault = true;

        internal static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var outerInvocation = (InvocationExpressionSyntax)c.Node;
                    if (!IsMethodOrderByExtension(outerInvocation, c.SemanticModel))
                    {
                        return;
                    }

                    var memberAccess = outerInvocation.Expression as MemberAccessExpressionSyntax;
                    if (memberAccess == null)
                    {
                        return;
                    }

                    var innerInvocation = memberAccess.Expression as InvocationExpressionSyntax;
                    if (!IsMethodOrderByExtension(innerInvocation, c.SemanticModel) &&
                        !IsMethodThenByExtension(innerInvocation, c.SemanticModel))
                    {
                        return;
                    }

                    c.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
                },
                SyntaxKind.InvocationExpression);
        }
        private static bool IsMethodOrderByExtension(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (invocation == null)
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            return methodSymbol != null &&
                   methodSymbol.Name == "OrderBy" &&
                   methodSymbol.MethodKind == MethodKind.ReducedExtension &&
                   methodSymbol.IsExtensionOn(KnownType.System_Collections_Generic_IEnumerable_T);
        }
        private static bool IsMethodThenByExtension(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (invocation == null)
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            return methodSymbol != null &&
                   methodSymbol.Name == "ThenBy" &&
                   methodSymbol.MethodKind == MethodKind.ReducedExtension &&
                   MethodIsOnIOrderedEnumerable(methodSymbol);
        }

        private static bool MethodIsOnIOrderedEnumerable(IMethodSymbol methodSymbol)
        {
            var receiverType = methodSymbol.ReceiverType as INamedTypeSymbol;

            return receiverType != null &&
                   receiverType.ConstructedFrom.ContainingNamespace.ToString() == "System.Linq" &&
                   receiverType.ConstructedFrom.MetadataName == "IOrderedEnumerable`1";
        }
    }
}
