namespace TextScanner
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// <p>
    /// A simple text scanner which can parse primitive types and strings using 
    /// regular expressions.
    /// </p><p>
    /// A <see cref="TextScanner"/> breaks its input into tokens using a delimiter pattern, which 
    /// by default matches whitespace. The resulting tokens may then be 
    /// converted into values of different types using the various next methods.
    /// </p>
    /// </summary>
    /// <remarks>
    /// <p>The default whitespace delimiter used by a scanner is as recognized 
    /// by <see cref="char.IsWhiteSpace(char)"/>. The <see cref="Reset()"/> method will reset the value of 
    /// the scanner's delimiter to the default whitespace delimiter regardless 
    /// of whether it was previously changed.</p>
    /// <p><b>Localized numbers</b></p>
    /// <p>An instance of this class is capable of scanning numbers in the
    /// standard formats as well as in the formats of the scanner's culture.
    /// A scanner's initial culture is the value returned by the
    /// <see cref="CultureInfo.CurrentUICulture"/> property; it may be changed
    /// via the <see cref="UseCulture"/> method.  The <see cref="Reset"/>
    /// method will reset the value of the scanner's culture to the initial
    /// culture reardleses of whether it was previously changed.</p>
    /// <p>Ported from http://java.sun.com/javase/6/docs/api/java/util/Scanner.html</p>
    /// </remarks>
    public partial class TextScanner
    {
        /// <summary>
        /// Peeks many characters ahead in the <see cref="textReader"/> stream,
        /// buffering the stream into the <see cref="unconsumedChars"/> queue.
        /// </summary>
        private class PeekStream : IEnumerable<char>
        {
            private readonly TextReader textReader;
            private readonly Queue<char> unconsumedChars;

            /// <summary>
            /// Initializes a new instance of the <see cref="PeekStream"/> class.
            /// </summary>
            /// <param name="textScanner">The text scanner.</param>
            public PeekStream(TextScanner textScanner)
            {
                this.textReader = textScanner.textReader;
                this.unconsumedChars = textScanner.unconsumedChars;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            public IEnumerator<char> GetEnumerator()
            {
                foreach (char unconsumedChar in this.unconsumedChars)
                {
                    yield return unconsumedChar;
                }

                while (true)
                {
                    int next = this.textReader.Read();
                    if (next < 0)
                    {
                        yield break;
                    }

                    this.unconsumedChars.Enqueue((char)next);
                    yield return (char)next;
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Reads the <see cref="unconsumedChars"/> queue if any characters are available,
        /// otherwise reads from the <see cref="textReader"/> stream.
        /// </summary>
        private class ReadStream : IEnumerable<char>
        {
            private readonly TextReader textReader;
            private readonly Queue<char> unconsumedChars;

            public ReadStream(TextScanner textScanner)
            {
                this.textReader = textScanner.textReader;
                this.unconsumedChars = textScanner.unconsumedChars;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            public IEnumerator<char> GetEnumerator()
            {
                while (this.unconsumedChars.Count > 0)
                {
                    yield return (char)this.unconsumedChars.Dequeue();
                }

                while (true)
                {
                    int next = this.textReader.Read();
                    if (next < 0)
                    {
                        yield break;
                    }

                    yield return (char)next;
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
