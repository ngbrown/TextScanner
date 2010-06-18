namespace TextScanner
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.AccessControl;
    using System.Text;
    using System.Text.RegularExpressions;

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
    /// <p>Ported from http://java.sun.com/javase/7/docs/api/java/util/Scanner.html</p>
    /// </remarks>
    public class TextScanner : IDisposable, IEnumerable<string>
    {
        private TextReader textReader;

        private CultureInfo culture;

        private Regex delimiter;

        private string nextToken;

        private int position;

        private Match match;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class 
        /// that produces values scanned from the specified source.
        /// </summary>
        /// <param name="textReader">
        /// A character source implementing a <see cref="TextReader"/>.
        /// </param>
        public TextScanner(TextReader textReader)
        {
            this.textReader = textReader;

            this.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class
        /// that produces values scanned from the specified file.
        /// The default encoding is used.
        /// </summary>
        /// <param name="source">A file to be scanned.</param>
        public TextScanner(FileInfo source)
            : this(new StreamReader(source.FullName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class
        /// that produces values scanned from the specified file, with the
        /// specified character encoding.
        /// </summary>
        /// <param name="source">A file to be scanned.</param>
        /// <param name="encoding">The character encoding to use.</param>
        public TextScanner(FileInfo source, Encoding encoding)
            : this(new StreamReader(source.FullName, encoding))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class
        /// that produces values scanned from the specified string.
        /// </summary>
        /// <param name="source">A string to scan</param>
        public TextScanner(string source)
            : this(new StringReader(source))
        {
        }

        private delegate TResult ParseDelegate<TResult>(string s, NumberStyles style, IFormatProvider provider);

        private delegate bool TryParseDelegate<TResult>(string s, NumberStyles style, IFormatProvider provider, out TResult result);

        /// <summary>
        /// Gets or sets the scanner's culture.
        /// </summary>
        /// <remarks>
        /// A scanner's culture affects many elements of its default primitive 
        /// matching regular expressions; see localized numbers above.
        /// </remarks>
        /// <value>The scanner's culture.</value>
        public CultureInfo Culture 
        { 
            get { return this.culture; }
            protected set { this.culture = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Regex"/> this <see cref="TextScanner"/>
        /// is currently using to match delimiters.
        /// </summary>
        /// <value>The scanners delimiting pattern.</value>
        public Regex Delimiter
        {
            get { return this.delimiter; }
            protected set { this.delimiter = value; }
        }

        /// <summary>
        /// Gets the match result of the last scanning operation performed by 
        /// this scanner.
        /// </summary>
        /// <remarks>
        /// The various <c>next</c>methods of <see cref="TextScanner"/> make a 
        /// match result available if they complete without throwing an exception.
        /// for instance, after an invorcation of the <see cref="NextInt"/> method
        /// that returned an int, this method returns a <see cref="Match"/> for
        /// the search of the <see cref="int"/> regular expression defined above.
        /// Similarly the <see cref="FindInLine"/>, <see cref="FindWithinHorizon"/>,
        /// and <see cref="Skip"/> methods will make a match available if they succeed.
        /// </remarks>
        /// <value>a match result for the last match operation.</value>
        public Match Match
        {
            get
            {
                if (this.match == null)
                {
                    throw new InvalidOperationException();
                }

                return this.match;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            while (this.HasNext())
            {
                yield return this.Next();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() +
                   "[delimiters=" + this.Delimiter + "]" +
                   "[position=" + this.position + "]" +
                   "[match valid=" + (this.nextToken != null) + "]" +
                   "[need input=" + "]" +
                   "[source closed=" + (this.textReader == null) + "]" +
                   "[skipped=" + "]" +
                   "[group separator=" + "]" +
                   "[decimal separator=" + "]" +
                   "[positive prefix=" + "]" +
                   "[negative prefix=" + "]" +
                   "[positive suffix=" + "]" +
                   "[negative suffix=" + "]" +
                   "[NaN string=" + "]" +
                   "[infinity string=" + "]";
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }

        /// <summary>
        /// Closes this scanner.
        /// </summary>
        /// <remarks>
        /// If this scanner has not yet been closed then if its underlying 
        /// <see cref="TextReader"/> also implements the <see cref="IDisposable"/>
        /// interface, then the TextReader's Dispose method will be invoked.
        /// If this scanner is already disposed then invoking this method will
        /// have no effect.
        /// </remarks>
        void IDisposable.Dispose()
        {
            if (this.textReader != null)
            {
                this.textReader.Dispose();
            }

            this.textReader = null;

            this.Delimiter = null;
            this.culture = null;
            this.nextToken = null;
        }

        /// <summary>
        /// Closes this scanner.
        /// </summary>
        /// <remarks>
        /// If this scanner has not yet been closed then if its underlying 
        /// <see cref="TextReader"/> also implements the <see cref="IDisposable"/>
        /// interface, then the TextReader's Dispose method will be invoked.
        /// If this scanner is already disposed then invoking this method will
        /// have no effect.
        /// <p/>
        /// Attempting to perform search operations after a scanner has been
        /// closed will result in an Exception.
        /// </remarks>
        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        /// <summary>
        /// Resets this scanner.
        /// </summary>
        /// <remarks>
        /// Resetting a scanner discards all of its explicit state information which
        /// may have been changed by invocation of <see cref="UseDelimiter"/>,
        /// <see cref="UseCulture"/>, or <see cref="UseRadix"/>.
        /// </remarks>
        /// <returns>this scanner</returns>
        public TextScanner Reset()
        {
            // matches any \s white space character plus the no-break space (U+00A0).
            // see http://msdn.microsoft.com/en-us/library/20bw873z.aspx#WhitespaceCharacter
            return this.UseDelimiter(@"[\s\xA0]+")
                       .UseCulture(CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns true if this scanner has another token in its input.
        /// This method may block while waiting for input to scan.
        /// The scanner does not advance past any input.
        /// </summary>
        /// <returns>true if and only if this scanner has another token</returns>
        public bool HasNext()
        {
            string s = this.PeekNextToken();

            return s != null;
        }

        /// <summary>
        /// Finds and returns the next complete token from this scanner.
        /// A complete token is preceded and followed by input that matches
        /// the delimiter pattern.  This method may block while waiting for
        /// input to scan, even if a previous invocation of <see cref="HasNext"/>
        /// returned true.
        /// </summary>
        /// <returns>The next token</returns>
        public string Next()
        {
            string value;
            if (this.nextToken != null)
            {
                value = this.nextToken;
                this.nextToken = null;
                return value;
            }

            this.ReadNextToken();

            if (this.nextToken == null)
            {
                throw new InvalidOperationException();
            }

            value = this.nextToken;
            this.nextToken = null;
            return value;
        }

        /// <summary>
        /// Returns true if the next token in this scanner's input can be
        /// interpreted as a double value using the <see cref="NextDouble"/>
        /// method.  The scanner does not advance past any input.
        /// </summary>
        /// <returns>true if and only if this scanner's next token is a valid double value</returns>
        public bool HasNextDouble()
        {
            return this.HasNext<double>(double.TryParse);
        }

        /// <summary>
        /// Scans the next token of the input as a <see cref="double"/>.  This
        /// method will throw <see cref="EndOfStreamException"/> if the next
        /// token cannot be translated into a valid double value.  If the 
        /// translation is successfull, the scanner advances past the
        /// input that matched.
        /// </summary>
        /// <remarks>
        /// If the next token matches the Float regular expression defined above then the token is converted into a double value as if by removing all locale specific prefixes, group separators, and locale specific suffixes, then mapping non-ASCII digits into ASCII digits via Character.digit, prepending a negative sign (-) if the locale specific negative prefixes and suffixes were present, and passing the resulting string to Double.parseDouble. If the token matches the localized NaN or infinity strings, then either "Nan" or "Infinity" is passed to Double.parseDouble as appropriate.
        /// </remarks>
        /// <returns>the <see cref="double"/> scanned from the input</returns>
        public double NextDouble()
        {
            return this.Next<double>(double.Parse);
        }

        public bool HasNextInt()
        {
            return this.HasNext<int>(int.TryParse);
        }

        public int NextInt()
        {
            return this.Next<int>(int.Parse);
        }

        /// <summary>
        /// Sets this scanner's culture to the specified culture.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        public TextScanner UseCulture(CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException("cultureInfo");
            }

            this.Culture = cultureInfo;

            return this;
        }

        /// <summary>
        /// Sets this scanner's delimiting regular expression to a <see cref="Regex"/> constructed from the specified <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// An invocation of this method of the form <c>UseDelimiter(pattern)</c> behaves in exactly
        /// the same way as the invocation <c>UseDelimiter(new Regex(pattern))</c>
        /// <p/>
        /// Invaking the <see cref="Reset"/> method will set the scanner's delimiter to the default.
        /// </remarks>
        /// <param name="pattern">A string specifing a delimiting regular expression</param>
        /// <returns>this scanner</returns>
        public TextScanner UseDelimiter(string pattern)
        {
            return this.UseDelimiter(new Regex(pattern));
        }

        /// <summary>
        /// Sets the scanner's delimiting regular expression to the specified regular expression.
        /// </summary>
        /// <param name="pattern">A delimiting regular expression</param>
        /// <returns>this scanner</returns>
        public TextScanner UseDelimiter(Regex pattern)
        {
            this.Delimiter = pattern;
            return this;
        }

        /// <summary>
        /// Returns true if there is another line in the input of this scanner.
        /// This input may block while waiting for input.  The scanner does not 
        /// advance past any input.
        /// </summary>
        /// <returns>
        /// <c>true</c> if and only iff this scanner has another line of input
        /// </returns>
        public bool HasNextLine()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Advances this scanner past the current line and returns the input
        /// that was skipped.  This method returns the rest of the current line,
        /// excluding any line separator at the end.  The position is set to the
        /// beginning of the next line.
        /// </summary>
        /// <remarks>
        /// Since this method continues to seach through the input looking for a 
        /// line separator, it may buffer all of the input searching for the
        /// line to skip if no line separators are present.
        /// </remarks>
        /// <returns>the line that was skipped.</returns>
        public string NextLine()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to find the next occurrence of a regular expression constructed from
        /// the specified string, ignoring delimiters.
        /// </summary>
        /// <remarks>
        /// An invocation of this method of the form <c>FindInLine(pattern)</c>
        /// behaves in exactly the same way as the invocation <c>FindInLine(new Regex(pattern))</c>.
        /// </remarks>
        /// <param name="pattern">a string specifying the regular expression to search for.</param>
        /// <returns>the text that matched the specified regular expression</returns>
        public string FindInLine(string pattern)
        {
            return this.FindInLine(new Regex(pattern));
        }

        /// <summary>
        /// Attempts to find the next occurrence of the specified regular expression,
        /// ignoring delimiters.  If the pattern is found before the next line separator,
        /// the scanner advances past the input that matched and returns the string that
        /// matched the pattern.  If no such pattern is detected in the input up to the
        /// next line separator, then <c>null</c> is returned and the scanner's position
        /// is unchanged.  This method may block waiting for input that matches the pattern.
        /// </summary>
        /// <remarks>
        /// Since this method continues to seach through the input looking for the specified
        /// pattern, it may buffer all of the input searching for the desired token if no
        /// line separators are present.
        /// </remarks>
        /// <param name="pattern">the regular expression to scan for</param>
        /// <returns>the text that matched the specified regular expression</returns>
        public string FindInLine(Regex pattern)
        {
            throw new NotImplementedException();
        }

        private void ReadNextToken()
        {
            StringBuilder sb = new StringBuilder();
            string completedToken;

            // read in characters until we have a match on our delimiter
            do
            {
                int next = this.textReader.Read();
                this.position++;
                if (next < 0)
                {
                    // end of the input
                    // return a null if we hadn't scanned anything yet
                    completedToken = sb.Length > 0 ? sb.ToString() : null;
                    break;
                }

                char nextChar = (char)next;
                sb.Append(nextChar);

                // do we have a match yet?
                Match match = this.Delimiter.Match(sb.ToString());
                if (match.Success)
                {
                    // we have started matching a delimiter
                    // our token is just before the delimiter starts.
                    completedToken = sb.ToString(0, match.Index);
                    break;
                }
            } 
            while (true);

            if (completedToken == null)
            {
                this.nextToken = null;
                return;
            }

            // Strip the token from our StringBuilder
            sb.Remove(0, completedToken.Length);

            // scan ahead through the delimiter until it's consumed
            do
            {
                int peek = this.textReader.Peek();
                if (peek < 0)
                {
                    // end of input
                    break;
                }

                char nextChar = (char)peek;
                sb.Append(nextChar);

                // have we stopped matching yet?
                Match match = this.Delimiter.Match(sb.ToString());
                if (match.Length != sb.Length)
                {
                    // end of capture
                    break;
                }

                // consume the character
                this.textReader.Read();
                this.position++;
            }
            while (true);

            this.nextToken = completedToken;
        }

        private string PeekNextToken()
        {
            if (this.nextToken != null)
            {
                return this.nextToken;
            }

            this.ReadNextToken();

            return this.nextToken;
        }

        /// <summary>
        /// Returns true if the next token in this scanner's input can be
        /// interpreted as a value of type T using the <see cref="Next{T}"/>
        /// method.  The scanner does not advance past any input.
        /// </summary>
        /// <typeparam name="T">The value type that is being parsed.  Must have a TryParse member.</typeparam>
        /// <param name="tryParse">The <c>TryParse</c> delegate.</param>
        /// <returns>
        /// true if and only if this scanner's next token is a valid double value
        /// </returns>
        private bool HasNext<T>(TryParseDelegate<T> tryParse)
            where T : struct
        {
            string s = this.PeekNextToken();

            if (s == null)
            {
                return false;
            }

            T dummy;
            return tryParse(s, NumberStyles.Number, this.Culture, out dummy);
        }

        /// <summary>
        /// Scans the next token of the input as a <c>T</c>.  This
        /// method will throw <see cref="EndOfStreamException"/> if the next
        /// token cannot be translated into a valid double value.  If the
        /// translation is successful, the scanner advances past the
        /// input that matched.
        /// </summary>
        /// <typeparam name="T">The value type that is being parsed.  Must have a TryParse member.</typeparam>
        /// <param name="parse">The <c>Parse</c> delegate.</param>
        /// <returns>
        /// the <see cref="double"/> scanned from the input
        /// </returns>
        /// <remarks>
        /// If the next token matches the Float regular expression defined above then the token is converted into a double value as if by removing all locale specific prefixes, group separators, and locale specific suffixes, then mapping non-ASCII digits into ASCII digits via Character.digit, prepending a negative sign (-) if the locale specific negative prefixes and suffixes were present, and passing the resulting string to Double.parseDouble. If the token matches the localized NaN or infinity strings, then either "Nan" or "Infinity" is passed to Double.parseDouble as appropriate.
        /// </remarks>
        private T Next<T>(ParseDelegate<T> parse)
            where T : struct
        {
            string s = this.PeekNextToken();

            if (s == null)
            {
                throw new EndOfStreamException();
            }

            T value = parse(this.nextToken, NumberStyles.Number, this.Culture);

            // if the parsing throws an exception, don't advance.
            this.nextToken = null;
            return value;
        }
    }
}
