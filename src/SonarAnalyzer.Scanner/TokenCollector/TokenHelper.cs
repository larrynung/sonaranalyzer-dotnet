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

using Microsoft.CodeAnalysis;

namespace SonarAnalyzer.Runner
{
    public static class TokenHelper
    {
        public static SyntaxNode GetBindableParent(this SyntaxToken token)
        {
            return token.Language == LanguageNames.CSharp
                ? new Rules.CSharp.SymbolReferenceAnalyzer().GetBindableParent(token)
                : new Rules.VisualBasic.SymbolReferenceAnalyzer().GetBindableParent(token);
        }

        public static bool IsUsingDirective(this SyntaxNode node)
        {
            return node.Language == LanguageNames.CSharp
                ? new Rules.CSharp.CopyPasteTokenAnalyzer().IsUsingDirective(node)
                : new Rules.VisualBasic.CopyPasteTokenAnalyzer().IsUsingDirective(node);
        }

        public static string GetCpdValue(this SyntaxToken token)
        {
            return token.Language == LanguageNames.CSharp
                ? new Rules.CSharp.CopyPasteTokenAnalyzer().GetCpdValue(token)
                : new Rules.VisualBasic.CopyPasteTokenAnalyzer().GetCpdValue(token);
        }
    }
}
