using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using NotAutoMapper;

namespace NotAutoMapper.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public Person(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
    public class Human
    {
        public Human(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }

    public static class Mappings
    {
        static Human Map(Person person)
        {
            return null;
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = NotAutoMapperAnalyzer.DiagnosticId,
                Message = "Map method from 'Person' to 'Human' can be completed",
                Severity = DiagnosticSeverity.Info,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 27, 22)
                }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public Person(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
    public class Human
    {
        public Human(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }

    public static class Mappings
    {
        static Human Map(Person person)
        {
            return new Human
            (
                name: person.Name
            );
        }
    }
}";

            VerifyCSharpFix(test, fixtest);
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
