/*
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

using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using SonarAnalyzer.Protobuf;
using System.Collections.Immutable;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Runner
{
    public class TokenCollector
    {
        private static readonly ISet<SymbolKind> DeclarationKinds = ImmutableHashSet.Create(
            SymbolKind.Event,
            SymbolKind.Field,
            SymbolKind.Local,
            SymbolKind.Method,
            SymbolKind.NamedType,
            SymbolKind.Parameter,
            SymbolKind.Property,
            SymbolKind.TypeParameter);

        private readonly SyntaxTree tree;
        private readonly SemanticModel semanticModel;
        private readonly IEnumerable<ClassifiedSpan> classifiedSpans;

        private readonly string filePath;

        public TokenCollector(string filePath, Document document, Workspace workspace)
        {
            this.filePath = filePath;
            this.tree = document.GetSyntaxTreeAsync().Result;
            this.semanticModel = document.GetSemanticModelAsync().Result;
            this.classifiedSpans = Classifier.GetClassifiedSpans(semanticModel, tree.GetRoot().FullSpan, workspace);
        }


        public SymbolReferenceInfo SymbolReferenceInfo
        {
            get
            {
                return Rules.SymbolReferenceAnalyzerBase.CalculateSymbolReferenceInfo(tree, semanticModel, t => IsIdentifier(t), t => t.GetBindableParent());
            }
        }

        public TokenTypeInfo TokenTypeInfo
        {
            get
            {
                return tree.GetRoot().Language == LanguageNames.CSharp
                    ? new Rules.CSharp.TokenTypeAnalyzer().GetTokenTypeInfo(tree, semanticModel)
                    : new Rules.VisualBasic.TokenTypeAnalyzer().GetTokenTypeInfo(tree, semanticModel);
            }
        }

        public CopyPasteTokenInfo CopyPasteTokenInfo
        {
            get
            {
                return Rules.CopyPasteTokenAnalyzerBase.CalculateTokenInfo(tree, n => n.IsUsingDirective(), t => t.GetCpdValue());
            }
        }


        private static bool IsIdentifier(SyntaxToken token)
        {
            return token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken) ||
                token.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierToken);
        }
    }
}
