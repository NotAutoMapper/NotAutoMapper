using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace NotAutoMapper.Test
{
    [TestClass]
    public class NoMappingUnitTests : CodeFixVerifier
    {
        [TestMethod]
        public void NoMapEmpty()
        {
            VerifyCSharpDiagnostic("");
        }

        [TestMethod]
        public void NoMapPreMapped()
        {
            VerifyCSharpDiagnostic(@"
public class Human
{
    public Human(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; }
    public int Age { get; }
}

public class Monkey
{
    public Monkey(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; }
    public int Age { get; }
}

public class MyMapper
{
    public Monkey Map(Human dto)
    {
        return new Monkey
        (
            name: dto.Name,
            age: dto.Age
        );
    }
}
");
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
