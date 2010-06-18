namespace TextScanner.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class IntegrationTests
    {
        /// <summary>
        /// By default, a scanner uses white space to separate tokens. 
        /// (White space characters include blanks, tabs, and 
        /// line terminators.
        /// <br/>
        /// This reads the individual words in xanadu.txt and prints them 
        /// out, one per line.
        /// </summary>
        /// <remarks>
        /// Comes from http://java.sun.com/docs/books/tutorial/essential/io/scanning.html
        /// </remarks>
        [Test]
        public void CanBreakInputIntoTokens()
        {
            var expectedStrings = new[]
{
@"In",
@"Xanadu",
@"did",
@"Kubla",
@"Khan",
@"A",
@"stately",
@"pleasure-dome",
@"decree:",
@"Where",
@"Alph,",
@"the",
@"sacred",
@"river,",
@"ran",
@"Through",
@"caverns",
@"measureless",
@"to",
@"man",
@"Down",
@"to",
@"a",
@"sunless",
@"sea."
};
            using (var s = new TextScanner(new StreamReader("xanadu.txt")))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        [Test]
        public void CanUseRegularExpressionsOnInput()
        {
            var expectedStrings = new[]
{
@"In Xanadu did Kubla Khan
A stately pleasure-dome decree:
Where Alph",
@"the sacred river",
@"ran
Through caverns measureless to man
Down to a sunless sea.
"
};

            using (var s = 
                new TextScanner(new StreamReader("xanadu.txt"))
                    .UseDelimiter(",\\s"))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        [Test]
        public void CanConsumeExtraSpaces()
        {
            var expectedStrings = new[]
{
@"string",
@"with",
@"extra",
@"spaces",
};
            
            using (var s =
                new TextScanner("string with  extra spaces ")
                    .UseDelimiter("\\s+"))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        [Test]
        public void CanReturnEmptyStrings()
        {
            var expectedStrings = new[]
{
@"string",
@"with",
@"",
@"extra",
@"spaces",
};
            
            using (var s =
                new TextScanner("string with  extra spaces ")
                    .UseDelimiter("\\s"))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        [Test]
        public void CanUseRegularExpressionsWithWordsOnInputString()
        {
            var expectedStrings = new[]
{
@"1", 
@"2",
@"red",
@"blue"
};

            string input = "1 fish 2 fish red fish blue fish";
            using (var s = 
                new TextScanner(input)
                    .UseDelimiter("\\s*fish\\s*"))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        /// <summary>
        /// <see cref="TextScanner"/> also supports tokens for all of the 
        /// language's primitive types (except for char), as well as 
        /// <see cref="decimal"/>. Also, numeric values can use 
        /// thousands separators. Thus, in a US locale, Scanner 
        /// correctly reads the string "32,767" as representing an 
        /// integer value.
        /// <br/>
        /// This example reads a list of double values and adds them up.
        /// </summary>
        /// <remarks>
        /// Comes from http://java.sun.com/docs/books/tutorial/essential/io/scanning.html
        /// </remarks>
        [Test]
        public void CanTranslateIndividualTokens()
        {
            double sum = 0;

            using (var s = new TextScanner(new StreamReader("usnumbers.txt")))
            {
                s.UseCulture(new CultureInfo("en-US"));

                int count = 0;

                while (s.HasNext())
                {
                    if (s.HasNextDouble())
                    {
                        sum += s.NextDouble();
                    }
                    else
                    {
                        s.Next();
                    }

                    Assert.That(count++ < 100);
                }
            }

            Assert.That(sum, Is.EqualTo(1032778.74159));
        }

        [Test]
        public void CanIterate()
        {
            var expectedStrings = new[] { @"1", @"2", @"red", @"blue" };
            string input = "1 fish 2 fish red fish blue fish";
            var output = new List<string>();

            using (var s =
                new TextScanner(input)
                    .UseDelimiter("\\s*fish\\s*"))
            {
                foreach (var token in s)
                {
                    output.Add(token);
                }
            }

            Assert.That(output, Is.EquivalentTo(expectedStrings));
        }

        [Test]
        public void CanReadIntegers()
        {
            var input = "1   3 53\t-1\r\n0";
            var expected = new[] { 1, 3, 53, -1, 0 };

            using (var s = new TextScanner(input))
            {
                ScannerEquivalentTest(s, expected, s.HasNextInt, s.NextInt);
            }
        }

        [Test]
        public void DoesntConsumeSpacesAtBeginning()
        {
            var expectedStrings = new[]
{
@"",
@"string",
@"with",
@"extra",
@"spaces",
};

            using (var s =
                new TextScanner(" string with  extra spaces ")
                    .UseDelimiter("\\s+"))
            {
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        [Test]
        public void CanFindInLine()
        {
            var expectedStrings = new[]
{
@"1", 
@"2",
@"red",
@"blue"
};

            string input = "1 fish 2 fish red fish blue fish";
            using (var s = new TextScanner(input))
            {
                s.FindInLine("(\\d+) fish (\\d+) fish (\\w+) fish (\\w+)");
                Match match = s.Match;

                Assert.That(match.Groups.Cast<string>(), Is.EquivalentTo(expectedStrings));
                ScannerEquivalentTest(s, expectedStrings, s.HasNext, s.Next);
            }
        }

        private static void ScannerEquivalentTest<T>(
            TextScanner s, IEnumerable<T> expected, Func<bool> hasNextFunc, Func<T> nextFunc)
        {
            var output = new List<T>();

            int count = 0;
            while (hasNextFunc.Invoke())
            {
                output.Add(nextFunc.Invoke());

                Assert.That(count++ < 1000, "Took over 1000 cycles, probably means it's stuck.");
            }

            Assert.That(output, Is.EquivalentTo(expected));
        }
    }
}
