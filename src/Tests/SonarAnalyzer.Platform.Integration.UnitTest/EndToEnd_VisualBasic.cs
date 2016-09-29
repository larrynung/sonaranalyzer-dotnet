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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Runner;
using System.IO;
using System.Linq;
using SonarAnalyzer.Protobuf;
using Google.Protobuf;
using System.Collections.Generic;
using System;
using FluentAssertions;

namespace SonarAnalyzer.Integration.UnitTest
{
    [TestClass]
    public class EndToEnd_VisualBasic
    {
        public TestContext TestContext { get; set; }

        private const string extension = ".vb";

        [TestInitialize]
        public void Initialize()
        {
            var tempInputFilePath = Path.Combine(TestContext.DeploymentDirectory, ParameterLoader.ParameterConfigurationFileName);
            File.Copy(Path.Combine(EndToEnd_CSharp.TestResourcesFolderName, "SonarLint.Vb.xml"), tempInputFilePath, true);

            Program.Main(new[] {
                tempInputFilePath,
                EndToEnd_CSharp.OutputFolderName,
                AnalyzerLanguage.VisualBasic.ToString()});
        }

        [TestMethod]
        public void Token_Types_Computed_VisualBasic()
        {
            var testFileContent = File.ReadAllLines(EndToEnd_CSharp.TestInputPath + extension);
            EndToEnd_CSharp.CheckTokenInfoFile(testFileContent, extension, 32, new[]
                {
                    new EndToEnd_CSharp.ExpectedTokenInfo { Index = 7, Kind = TokenType.Comment, Text = "' FIXME: fix this issue" },
                    new EndToEnd_CSharp.ExpectedTokenInfo { Index = 6, Kind = TokenType.TypeName, Text = "TTTestClass" },
                    new EndToEnd_CSharp.ExpectedTokenInfo { Index = 17, Kind = TokenType.TypeName, Text = "TTTestClass" },
                    new EndToEnd_CSharp.ExpectedTokenInfo { Index = 16, Kind = TokenType.Keyword, Text = "New" }
                });
        }

        [TestMethod]
        public void Cpd_Tokens_Computed_VisualBasic()
        {
            EndToEnd_CSharp.CheckCpdTokens(
                @"Public Class TTTestClass Public Function MyMethod ( ) As Object Dim x = $num Dim y = New TTTestClass " +
                "If $num = $num Then Return x + $str End If Return $char End Function End Class");
        }

        [TestMethod]
        public void Symbol_Reference_Computed_VisualBasic()
        {
            var testFileContent = File.ReadAllLines(EndToEnd_CSharp.TestInputPath + extension);
            EndToEnd_CSharp.CheckTokenReferenceFile(testFileContent, extension, 4, new[]
                {
                    new EndToEnd_CSharp.ExpectedReferenceInfo { Index = 0, NumberOfReferences = 2 },
                    new EndToEnd_CSharp.ExpectedReferenceInfo { Index = 1, NumberOfReferences = 0 },
                    new EndToEnd_CSharp.ExpectedReferenceInfo { Index = 2, NumberOfReferences = 1 },
                    new EndToEnd_CSharp.ExpectedReferenceInfo { Index = 3, NumberOfReferences = 0 }
                });
        }
    }
}
