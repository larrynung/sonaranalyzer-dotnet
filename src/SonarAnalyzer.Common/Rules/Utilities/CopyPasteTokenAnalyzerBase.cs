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
using SonarAnalyzer.Common;
using SonarAnalyzer.Protobuf;
using System;

namespace SonarAnalyzer.Rules
{
    public abstract class CopyPasteTokenAnalyzerBase : UtilityAnalyzerBase<CopyPasteTokenInfo>
    {
        protected const string DiagnosticId = "S9999-cpd";
        protected const string Title = "Copy-paste token calculator";

        protected static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, string.Empty, string.Empty, DiagnosticSeverity.Warning,
                true, customTags: WellKnownDiagnosticTags.NotConfigurable);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        internal const string CopyPasteTokenFileName = "token-cpd.pb";

        protected sealed override string FileName => CopyPasteTokenFileName;

        protected sealed override CopyPasteTokenInfo GetMessage(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            return CalculateTokenInfo(syntaxTree, IsUsingDirective, GetCpdValue);
        }

        internal static CopyPasteTokenInfo CalculateTokenInfo(SyntaxTree syntaxTree, Func<SyntaxNode, bool> isUsingDirective,
            Func<SyntaxToken, string> getCpdValue)
        {
            var cpdTokenInfo = new CopyPasteTokenInfo
            {
                FilePath = syntaxTree.FilePath
            };

            var tokens = syntaxTree.GetRoot().DescendantTokens(n => !isUsingDirective(n), false);

            foreach (var token in tokens)
            {
                var tokenInfo = new CopyPasteTokenInfo.Types.TokenInfo
                {
                    TokenValue = getCpdValue(token),
                    TextRange = GetTextRange(Location.Create(syntaxTree, token.Span).GetLineSpan())
                };

                if (!string.IsNullOrWhiteSpace(tokenInfo.TokenValue))
                {
                    cpdTokenInfo.TokenInfo.Add(tokenInfo);
                }
            }

            return cpdTokenInfo;
        }

        internal abstract string GetCpdValue(SyntaxToken token);

        internal abstract bool IsUsingDirective(SyntaxNode node);
    }
}
