using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace NotAutoMapper.Test
{
    [TestClass]
    public class NoMappingUnitTests : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void NoMapEmpty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
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
