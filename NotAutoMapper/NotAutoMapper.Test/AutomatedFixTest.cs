using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TestHelper;

namespace NotAutoMapper.Test
{
    [TestClass]
    public class AutomatedFixTest : CodeFixVerifier
    {
        [TestMethod]
        public void TestTestCases()
        {
            var files = Directory.EnumerateFiles("TestCases", "*.cs");

            foreach (var f in files)
            {
                var content = File.ReadAllLines(f);
                var topComments = content.TakeWhile(x => Regex.IsMatch(x, "^//.*$")).ToImmutableList();
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new NotAutoMapperCodeFixProvider();
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NotAutoMapperAnalyzer();
        }
    }
}
