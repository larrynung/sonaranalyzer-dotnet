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
using System.Linq;

namespace SonarAnalyzer.Rules
{
    public abstract class MetricsAnalyzerBase : UtilityAnalyzerBase<MetricsInfo>
    {
        protected const string DiagnosticId = "S9999-metrics";
        protected const string Title = "Metrics calculator";

        protected static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, string.Empty, string.Empty, DiagnosticSeverity.Warning,
                true, customTags: WellKnownDiagnosticTags.NotConfigurable);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        internal const string MetricsFileName = "metrics.pb";

        protected sealed override string FileName => MetricsFileName;

        protected abstract MetricsBase GetMetrics(SyntaxTree syntaxTree);

        protected sealed override MetricsInfo GetMessage(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            var metrics = GetMetrics(syntaxTree);
            return CalculateMetrics(metrics, syntaxTree.FilePath, IgnoreHeaderComments);
        }

        internal static MetricsInfo CalculateMetrics(MetricsBase metrics, string filePath, bool ignoreHeaderComments)
        {
            var complexity = metrics.Complexity;

            var metricsInfo = new MetricsInfo
            {
                FilePath = filePath,
                LineCount = metrics.LineCount,
                ClassCount = metrics.ClassCount,
                StatementCount = metrics.StatementCount,
                FunctionCount = metrics.FunctionCount,
                PublicApiCount = metrics.PublicApiCount,
                PublicUndocumentedApiCount = metrics.PublicUndocumentedApiCount,

                Complexity = complexity,
                ComplexityInClasses = metrics.ClassNodes.Sum(metrics.GetComplexity),
                ComplexityInFunctions = metrics.FunctionNodes.Sum(metrics.GetComplexity),

                FileComplexityDistribution = new Distribution(Distribution.FileComplexityRange).Add(complexity).ToString(),
                FunctionComplexityDistribution = metrics.FunctionComplexityDistribution.ToString()
            };

            var comments = metrics.GetComments(ignoreHeaderComments);
            metricsInfo.NoSonarComment.AddRange(comments.NoSonar);
            metricsInfo.NonBlankComment.AddRange(comments.NonBlank);

            metricsInfo.CodeLine.AddRange(metrics.CodeLines);
            return metricsInfo;
        }
    }
}
