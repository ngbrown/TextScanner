namespace TextScanner.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;

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
            StringBuilder output = new StringBuilder();

            TextScanner s = null;

            try
            {
                s = new TextScanner(new StreamReader("xanadu.txt"));

                int count = 0;
                while (s.HasNext())
                {
                    output.AppendLine(s.Next());

                    Assert.That(count++ < 100);
                }
            }
            finally
            {
                if (s != null)
                {
                    s.Dispose();
                }
            }

            const string ExpectedString =
@"In
Xanadu
did
Kubla
Khan
A
stately
pleasure-dome
decree:
Where
Alph,
the
sacred
river,
ran
Through
caverns
measureless
to
man
Down
to
a
sunless
sea.
";
            Assert.That(output.ToString(), Is.EqualTo(ExpectedString));
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
            TextScanner s = null;
            double sum = 0;

            try
            {
                s = new TextScanner(new StreamReader("usnumbers.txt"));
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
            finally
            {
                if (s != null)
                {
                    s.Dispose();
                }
            }

            Assert.That(sum, Is.EqualTo(1032778.74159));
        }
    }
}
