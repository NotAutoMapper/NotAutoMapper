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
        private struct Location
        {
            public static Location FromComments(IImmutableList<string> comments)
            {
                return comments
                    .Select(x => Regex.Match(x, "^(?://)? *FixLocation *: *(?:ln|line):? *(?<line>\\d+),? *(?:col|column):? *(?<column>\\d+) *$", RegexOptions.IgnoreCase))
                    .Where(m => m.Success)
                    .Select(x => new Location
                    (
                        line: int.Parse(x.Groups["line"].Value),
                        column: int.Parse(x.Groups["column"].Value)
                    ))
                    .Single();
            }

            public Location(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public int Line { get; }
            public int Column { get; }
        }

        private static string MessageFromComments(IImmutableList<string> comments)
        {
            return comments
                .Select(x => Regex.Match(x, "^(?://)? *MESSAGE *: *(?<message>[^ ]|[^ ].*[^ ]) *$", RegexOptions.IgnoreCase))
                .Where(m => m.Success)
                .Select(x => x.Groups["message"].Value)
                .Single();
        }

        [TestMethod]
        public void TestTestCases()
        {
            var files = Directory.EnumerateFiles("TestCases", "*.cs");

            foreach (var f in files)
            {
                var content = File.ReadAllLines(f);
                var topComments = content.TakeWhile(x => Regex.IsMatch(x, "^//.*$")).ToImmutableList();

                var location = Location.FromComments(topComments);
                var message = MessageFromComments(topComments);
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
