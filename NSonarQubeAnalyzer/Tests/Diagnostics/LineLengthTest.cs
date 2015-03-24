﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSonarQubeAnalyzer.Diagnostics;

namespace Tests.Diagnostics
{
    [TestClass]
    public class LineLengthTest
    {
        [TestMethod]
        public void LineLength()
        {
            var diagnostic = new LineLength {Maximum = 47};
            Verifier.Verify(@"TestCases\LineLength.cs", diagnostic);
        }
    }
}
