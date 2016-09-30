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
using System.Collections.Generic;
using SonarAnalyzer.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenTypeAnalyzer : TokenTypeAnalyzerBase
    {
        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer => Helpers.CSharp.GeneratedCodeRecognizer.Instance;

        protected override TokenClassifierBase GetTokenClassifier(SyntaxToken token, SemanticModel semanticModel)
            => new TokenClassifier(token, semanticModel);

        private class TokenClassifier : TokenClassifierBase
        {
            public TokenClassifier(SyntaxToken token, SemanticModel semanticModel)
                : base(token, semanticModel)
            {
            }

            protected override SyntaxNode GetBindableParent(SyntaxToken token)
            {
                return SymbolReferenceAnalyzer.GetBindableParentNode(token);
            }

            protected override bool IsIdentifier(SyntaxToken token)
            {
                return token.IsKind(SyntaxKind.IdentifierToken);
            }

            protected override bool IsKeyword(SyntaxToken token)
            {
                return SyntaxFacts.IsKeywordKind(token.Kind());
            }

            protected override bool IsContextualKeyword(SyntaxToken token)
            {
                return SyntaxFacts.IsContextualKeyword(token.Kind());
            }

            protected override bool IsRegularComment(SyntaxTrivia trivia)
            {
                return CommentKinds.Contains(trivia.Kind());
            }

            protected override bool IsNumericLiteral(SyntaxToken token)
            {
                return token.IsKind(SyntaxKind.NumericLiteralToken);
            }

            protected override bool IsStringLiteral(SyntaxToken token)
            {
                return StringKinds.Contains(token.Kind());
            }

            protected override bool IsDocComment(SyntaxTrivia trivia)
            {
                return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
            }

            private static readonly ISet<SyntaxKind> StringKinds = ImmutableHashSet.Create(
                SyntaxKind.StringLiteralToken,
                SyntaxKind.CharacterLiteralToken,
                SyntaxKind.InterpolatedStringStartToken,
                SyntaxKind.InterpolatedVerbatimStringStartToken,
                SyntaxKind.InterpolatedStringTextToken,
                SyntaxKind.InterpolatedStringEndToken);

            private static readonly ISet<SyntaxKind> CommentKinds = ImmutableHashSet.Create(
                SyntaxKind.SingleLineCommentTrivia,
                SyntaxKind.MultiLineCommentTrivia);
        }
    }
}
