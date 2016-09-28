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
                var tokenTypeInfo = new TokenTypeInfo
                {
                    FilePath = filePath
                };

                foreach (var classifiedSpan in classifiedSpans)
                {
                    var tokenType = ClassificationTypeMapping.ContainsKey(classifiedSpan.ClassificationType)
                        ? ClassificationTypeMapping[classifiedSpan.ClassificationType]
                        : TokenType.Unknown;

                    var tokenInfo = new TokenTypeInfo.Types.TokenInfo
                    {
                        TokenType = tokenType,
                        TextRange = GetTextRange(Location.Create(tree, classifiedSpan.TextSpan).GetLineSpan())
                    };
                    tokenTypeInfo.TokenInfo.Add(tokenInfo);
                }

                return tokenTypeInfo;
            }
        }

        public CopyPasteTokenInfo CopyPasteTokenInfo
        {
            get
            {
                return Rules.CopyPasteTokenAnalyzerBase.CalculateTokenInfo(tree, n => n.IsUsingDirective(), t => t.GetCpdValue());
            }
        }

        internal static TextRange GetTextRange(FileLinePositionSpan lineSpan)
        {
            return new TextRange
            {
                StartLine = lineSpan.StartLinePosition.GetLineNumberToReport(),
                EndLine = lineSpan.EndLinePosition.GetLineNumberToReport(),
                StartOffset = lineSpan.StartLinePosition.Character,
                EndOffset = lineSpan.EndLinePosition.Character
            };
        }

        private static bool IsIdentifier(SyntaxToken token)
        {
            return token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken) ||
                token.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierToken);
        }

        private static readonly IDictionary<string, TokenType> ClassificationTypeMapping = new Dictionary<string, TokenType>
        {
            { ClassificationTypeNames.ClassName, TokenType.TypeName },
            { ClassificationTypeNames.DelegateName, TokenType.TypeName },
            { ClassificationTypeNames.EnumName, TokenType.TypeName },
            { ClassificationTypeNames.InterfaceName, TokenType.TypeName },
            { ClassificationTypeNames.ModuleName, TokenType.TypeName },
            { ClassificationTypeNames.StructName, TokenType.TypeName },

            { ClassificationTypeNames.TypeParameterName, TokenType.TypeName },

            { ClassificationTypeNames.Comment, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentAttributeName, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentAttributeQuotes, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentAttributeValue, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentCDataSection, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentComment, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentDelimiter, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentEntityReference, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentName, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentProcessingInstruction, TokenType.Comment },
            { ClassificationTypeNames.XmlDocCommentText, TokenType.Comment },

            { ClassificationTypeNames.NumericLiteral, TokenType.NumericLiteral },

            { ClassificationTypeNames.StringLiteral, TokenType.StringLiteral },
            { ClassificationTypeNames.VerbatimStringLiteral, TokenType.StringLiteral },

            { ClassificationTypeNames.Keyword, TokenType.Keyword },
            { ClassificationTypeNames.PreprocessorKeyword, TokenType.Keyword }
        }.ToImmutableDictionary();
    }
}
