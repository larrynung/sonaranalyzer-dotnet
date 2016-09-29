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
using SonarAnalyzer.Helpers;
using System.Linq;
using System.Collections.Generic;
using Google.Protobuf;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Xml.Linq;
using SonarAnalyzer.Protobuf;
using Microsoft.CodeAnalysis.Text;

namespace SonarAnalyzer.Rules
{
    public abstract class UtilityAnalyzerBase : SonarDiagnosticAnalyzer
    {
        protected static readonly object parameterReadLock = new object();
        private static volatile bool parametersAlreadyRead = false;

        protected static bool IsAnalyzerEnabled { get; set; } = false;

        protected static string WorkDirectoryBasePath { get; set; }

        protected static bool IgnoreHeaderComments { get; set; }

        protected static void ReadParameters(AnalyzerOptions options)
        {
            if (parametersAlreadyRead)
            {
                return;
            }

            var additionalFile = options.AdditionalFiles
                .FirstOrDefault(f => ParameterLoader.ConfigurationFilePathMatchesExpected(f.Path));

            if (additionalFile == null)
            {
                return;
            }

            lock (parameterReadLock)
            {
                if (parametersAlreadyRead)
                {
                    return;
                }

                var xml = XDocument.Load(additionalFile.Path);
                var settings = xml.Descendants("Setting");
                ReadBoolProperty(settings, "sonar.cs.ignoreHeaderComments");
                // todo read sonar.vbnet.ignoreHeaderComments
                WorkDirectoryBasePath = GetPropertyStringValue(settings, "sonarqube.out.protobuf");

                if (!string.IsNullOrEmpty(WorkDirectoryBasePath))
                {
                    IsAnalyzerEnabled = true;
                }

                parametersAlreadyRead = true;
            }
        }

        private static void ReadBoolProperty(IEnumerable<XElement> settings, string propName)
        {
            string propertyStringValue = GetPropertyStringValue(settings, propName);
            bool propertyValue;
            if (propertyStringValue != null &&
                bool.TryParse(propertyStringValue, out propertyValue))
            {
                IgnoreHeaderComments = propertyValue;
            }
        }

        private static string GetPropertyStringValue(IEnumerable<XElement> settings, string propName)
        {
            return settings
                .FirstOrDefault(s => s.Element("Key")?.Value == propName)
                ?.Element("Value").Value;
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
    }

    public abstract class UtilityAnalyzerBase<TMessage> : UtilityAnalyzerBase
        where TMessage : IMessage
    {
        private static readonly object fileWriteLock = new object();

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterCompilationAction(
                c =>
                {
                    ReadParameters(c.Options);

                    if (!IsAnalyzerEnabled)
                    {
                        return;
                    }

                    var messages = new List<TMessage>();

                    foreach (var tree in c.Compilation.SyntaxTrees)
                    {
                        if (!GeneratedCodeRecognizer.IsGenerated(tree))
                        {
                            messages.Add(GetMessage(tree, c.Compilation.GetSemanticModel(tree)));
                        }
                    }

                    if (!messages.Any())
                    {
                        return;
                    }

                    var pathToWrite = Path.Combine(WorkDirectoryBasePath, FileName);
                    lock (fileWriteLock)
                    {
                        // Make sure the folder exists
                        Directory.CreateDirectory(WorkDirectoryBasePath);

                        if (!File.Exists(pathToWrite))
                        {
                            using (File.Create(pathToWrite)) { }
                        }

                        using (var metricsStream = new FileStream(pathToWrite, FileMode.Append, FileAccess.Write))
                        {
                            foreach (var message in messages)
                            {
                                message.WriteDelimitedTo(metricsStream);
                            }
                        }
                    }
                });
        }

        protected abstract TMessage GetMessage(SyntaxTree syntaxTree, SemanticModel semanticModel);

        protected abstract GeneratedCodeRecognizer GeneratedCodeRecognizer { get; }

        protected abstract string FileName { get; }
    }
}
