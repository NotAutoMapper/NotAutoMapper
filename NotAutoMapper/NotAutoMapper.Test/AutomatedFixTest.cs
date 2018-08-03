using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        private class CodeRegion
        {
            public static IImmutableList<CodeRegion> FromSource(IImmutableList<string> source)
            {
                var regions = ImmutableList<CodeRegion>.Empty;

                while (source.Count > 0)
                {
                    source = source.SkipWhile(x => !Regex.IsMatch(x, "^ *#region")).ToImmutableList();
                    var region = source.TakeUntil(x => !Regex.IsMatch(x, "^ *#endregion")).ToImmutableList();
                    source = source.Skip(region.Count).ToImmutableList();

                    if (region.Count > 0)
                        regions = regions.Add(new CodeRegion
                        (
                            name: Regex.Match(region[0], "^ *#region *(?<name>[^ ]|[^ ].*[^ ]) *$").Groups["name"].Value,
                            lines: region.Skip(1).Take(region.Count - 2).ToImmutableList()
                        ));
                }

                return regions;
            }

            public CodeRegion(string name, IImmutableList<string> lines)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            }

            public string Name { get; }
            public IImmutableList<string> Lines { get; }
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
                var regions = CodeRegion.FromSource(content.ToImmutableList());

                var models = regions.Single(x => x.Name.Equals("Models", StringComparison.OrdinalIgnoreCase));
                var expected = regions.Single(x => x.Name.Equals("Expected", StringComparison.OrdinalIgnoreCase));
                var inputs = regions.Where(x => x.Name.Equals("Input", StringComparison.OrdinalIgnoreCase));

                foreach (var input in inputs)
                {
                }
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
