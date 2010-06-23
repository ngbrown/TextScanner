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
        public void CanTranslateAllTokenTypes()
        {
            string input = "text 9999999999999999999999999999 -32768 2,147,483,647 -9,223,372,036,854,775,808 129 NaN -3.402823e38 true 65,535 4,294,967,295 18,446,744,073,709,551,615 -128";

            using (var s = new TextScanner(input)
                            .UseCulture(new CultureInfo("en-US")))
            {
                Assert.That(s.HasNext(), Is.True);
                Assert.That(s.Next(), Is.EqualTo("text"));
                
                Assert.That(s.HasNextDecimal(), Is.True);
                Assert.That(s.NextDecimal(), Is.EqualTo(9999999999999999999999999999m));
                
                Assert.That(s.HasNextInt16(), Is.True);
                Assert.That(s.NextInt16(), Is.EqualTo((short)-32768));
                
                Assert.That(s.HasNextInt32(), Is.True);
                Assert.That(s.NextInt32(), Is.EqualTo(2147483647));
                
                Assert.That(s.HasNextInt64(), Is.True);
                Assert.That(s.NextInt64(), Is.EqualTo(-9223372036854775808L));

                Assert.That(s.HasNextByte(), Is.True);
                Assert.That(s.NextByte(), Is.EqualTo((byte)129));
                
                Assert.That(s.HasNextDouble(), Is.True);
                Assert.That(s.NextDouble(), Is.EqualTo(double.NaN));
                
                Assert.That(s.HasNextSingle(), Is.True);
                Assert.That(s.NextSingle(), Is.EqualTo(-3.402823e38f));

                Assert.That(s.HasNextBoolean(), Is.True);
                Assert.That(s.NextBoolean(), Is.EqualTo(true));

                Assert.That(s.HasNextUInt16(), Is.True);
                Assert.That(s.NextUInt16(), Is.EqualTo((ushort)65535));

                Assert.That(s.HasNextUInt32(), Is.True);
                Assert.That(s.NextUInt32(), Is.EqualTo((uint)4294967295U));

                Assert.That(s.HasNextUInt64(), Is.True);
                Assert.That(s.NextUInt64(), Is.EqualTo((ulong)18446744073709551615U));

                Assert.That(s.HasNextSByte(), Is.True);
                Assert.That(s.NextSByte(), Is.EqualTo((sbyte)-128));
            }

            using (var s = new TextScanner("5/1/2008 8:30:52 AM").UseDelimiter(",\\s*"))
            {
                Assert.That(s.HasNextDateTime(), Is.True);
                Assert.That(s.NextDateTime(), Is.EqualTo(new DateTime(2008, 5, 1, 8, 30, 52)));
            }
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
                ScannerEquivalentTest(s, expected, s.HasNextInt32, s.NextInt32);
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
            var expectedStrings = new[] { "1", "2", "red", "blue" };

            string input = "1 fish 2 fish red fish blue fish";
            using (var s = new TextScanner(input))
            {
                var matchingText = s.FindInLine("(\\d+) fish (\\d+) fish (\\w+) fish (\\w+)");

                // skip the overall match and get the captured group values
                var groups = from @group in s.Match.Groups.Cast<Group>().Skip(1)
                             select @group.Value;

                Assert.That(groups, Is.EquivalentTo(expectedStrings));
                Assert.That(matchingText, Is.EqualTo("1 fish 2 fish red fish blue"));
                Assert.That(s.Next(), Is.EqualTo(string.Empty));
                Assert.That(s.Next(), Is.EqualTo("fish"));
            }
        }

        [Test]
        public void CanSkipLines()
        {
            string input =
@"First Line, second statement,
Second Line, fourth statement
";

            using (var s = new TextScanner(input)
                .UseDelimiter(@",\s*"))
            {
                Assert.That(s.Next(), Is.EqualTo("First Line"));
                Assert.That(s.NextLine(), Is.EqualTo("second statement,"));
                Assert.That(s.Next(), Is.EqualTo("Second Line"));

                Assert.That(s.NextLine(), Is.EqualTo("fourth statement"));

                // there are no more line endings, so the following should throw
                Assert.Catch<InvalidOperationException>(() => s.NextLine());
            }
        }

        [Test]
        public void AttemptingToSkipLineWithNoLinebreakDoesntAdvancePosition()
        {
            string input = @"First line, second statement, third statement";

            using (var s = new TextScanner(input)
                            .UseDelimiter(@",\s*"))
            {
                Assert.That(s.Next(), Is.EqualTo("First line"));
                Assert.Catch<InvalidOperationException>(() => s.NextLine());
                Assert.That(s.Next(), Is.EqualTo("second statement"));
                Assert.That(s.HasNext(), Is.True);
            }
        }

        [Test]
        public void MatchIsFilledOnNext()
        {
            string input = "4234; a string; +3,234.32";
            
            using (var s = new TextScanner(input)
                            .UseCulture(new CultureInfo("en-US"))
                            .UseDelimiter(";\\s*"))
            {
                Assert.That(s.NextInt64(), Is.EqualTo(4234));
                Assert.That(s.Match.Value, Is.EqualTo("4234"));

                Assert.That(s.Next(), Is.EqualTo("a string"));
                Assert.That(s.Match.Value, Is.EqualTo("a string"));

                Assert.That(s.NextDouble(), Is.EqualTo(3234.32));
                Assert.That(s.Match.Value, Is.EqualTo("+3,234.32"));
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
