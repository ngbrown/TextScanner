namespace TextScanner
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
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
    /// <p>Ported from http://java.sun.com/javase/6/docs/api/java/util/Scanner.html</p>
    /// </remarks>
    public partial class TextScanner : IDisposable, IEnumerable<string>
    {
        /// <summary>
        /// Holds characters that we are attempting to evaluate, but we don't
        /// know if we can move the position on.  Supports the use of
        /// <see cref="NextLine"/> and <see cref="FindInLine(Regex)"/>.
        /// </summary>
        private readonly Queue<char> unconsumedChars = new Queue<char>();

        private static readonly Regex RegexSomething = new Regex(".+");

        private TextReader textReader;

        private CultureInfo culture;

        private Regex delimiter;

        private int position;

        private Match match;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class 
        /// that produces values scanned from the specified source.
        /// </summary>
        /// <param name="source">
        /// A character source implementing a <see cref="TextReader"/>.
        /// </param>
        public TextScanner(TextReader source)
        {
            this.textReader = source;

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

        private delegate TResult ParseDelegate<TResult>(string s);

        /// <summary>
        /// (string s, out TResult result)
        /// </summary>
        private delegate bool TryParseDelegate<TResult>(string s, out TResult result);

        /// <summary>
        /// Gets the scanner's culture.
        /// </summary>
        /// <remarks>
        /// A scanner's culture affects many elements of its default primitive 
        /// matching regular expressions; see localized numbers above.
        /// </remarks>
        /// <value>The scanner's culture.</value>
        public CultureInfo Culture 
        { 
            get { return this.culture; }
            private set { this.culture = value; }
        }

        /// <summary>
        /// Gets the <see cref="Regex"/> this <see cref="TextScanner"/>
        /// is currently using to match delimiters.
        /// </summary>
        /// <value>The scanners delimiting pattern.</value>
        public Regex Delimiter
        {
            get { return this.delimiter; }
            private set { this.delimiter = value; }
        }

        /// <summary>
        /// Gets the match result of the last scanning operation performed by 
        /// this scanner.
        /// </summary>
        /// <remarks>
        /// The various <c>next</c>methods of <see cref="TextScanner"/> make a 
        /// match result available if they complete without throwing an exception.
        /// for instance, after an invorcation of the <see cref="NextInt32"/> method
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
                    throw new InvalidOperationException("No match result available");
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
                   "[match valid=" + ( this.match != null ? this.match.Success : false) + "]" +
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
            // Valid white space characters are members of the SpaceSeparator 
            // category in UnicodeCategory, as well as these Unicode characters: 
            // hexadecimal 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x0085, 0x2028, and 0x2029.

            // Unicode SpaceSeparator matches any \s white space character plus the no-break space (U+00A0).
            // see http://msdn.microsoft.com/en-us/library/20bw873z.aspx#WhitespaceCharacter
            // also matches 
            return this.UseDelimiter(@"[\x09\x0A\x0B\x0C\x0D\x85\u2028\u2029\s\xA0]+")
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
            string value = this.ReadNextToken(true);

            if (value == null)
            {
                throw new InvalidOperationException();
            }

            this.match = RegexSomething.Match(value);
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
            return this.HasNext<double>(
                    delegate(string s, out double r) { return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, this.Culture, out r); });
        }

        /// <summary>
        /// Scans the next token of the input as a <see cref="double"/>.  This
        /// method will throw <see cref="FormatException"/> if the next
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
            return this.Next<double>(delegate(string s) { return double.Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, this.Culture); });
        }

        public bool HasNextSingle()
        {
            return this.HasNext<float>(delegate(string s, out float r) { return float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, this.Culture, out r); });
        }

        public float NextSingle()
        {
            return this.Next<float>(delegate(string s) { return float.Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, this.Culture); });
        }

        public bool HasNextDecimal()
        {
            return this.HasNext<decimal>(delegate(string s, out decimal r)
                    {
                        return decimal.TryParse(s, NumberStyles.Number, this.Culture, out r);
                    });
        }

        public decimal NextDecimal()
        {
            return this.Next<decimal>(
                delegate(string s) { return decimal.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextInt16()
        {
            return this.HasNext<short>(
                delegate(string s, out short r) { return short.TryParse(s, NumberStyles.Number, this.Culture, out r); });
        }

        public short NextInt16()
        {
            return this.Next<short>(
                delegate(string s) { return short.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextInt32()
        {
            return this.HasNext<int>(delegate(string s, out int r)
                {
                    return int.TryParse(s, NumberStyles.Number, this.Culture, out r);
                });
        }

        public int NextInt32()
        {
            return this.Next<int>(delegate(string s) { return int.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextInt64()
        {
            return this.HasNext<long>(delegate(string s, out long r) { return long.TryParse(s, NumberStyles.Number, this.Culture, out r); });
        }

        public long NextInt64()
        {
            return this.Next<long>(delegate(string s) { return long.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextByte()
        {
            return this.HasNext<byte>(delegate(string s, out byte r) { return byte.TryParse(s, NumberStyles.Integer, this.Culture, out r); });
        }

        public byte NextByte()
        {
            return this.Next<byte>(delegate(string s) { return byte.Parse(s, NumberStyles.Integer, this.Culture); });
        }

        public bool HasNextBoolean()
        {
            return this.HasNext<bool>(delegate(string s, out bool r) { return bool.TryParse(s, out r); });
        }

        public bool NextBoolean()
        {
            return this.Next<bool>(delegate(string s) { return bool.Parse(s); });
        }

        public bool HasNextUInt16()
        {
            return this.HasNext<ushort>(
                delegate(string s, out ushort r) { return ushort.TryParse(s, NumberStyles.Number, this.Culture, out r); });
        }

        public ushort NextUInt16()
        {
            return this.Next<ushort>(
                delegate(string s) { return ushort.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextUInt32()
        {
            return this.HasNext<uint>(delegate(string s, out uint r)
                    {
                        return uint.TryParse(s, NumberStyles.Number, this.Culture, out r);
                    });
        }

        public uint NextUInt32()
        {
            return this.Next<uint>(delegate(string s) { return uint.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextUInt64()
        {
            return this.HasNext<ulong>(delegate(string s, out ulong r) { return ulong.TryParse(s, NumberStyles.Number, this.Culture, out r); });
        }

        public ulong NextUInt64()
        {
            return this.Next<ulong>(delegate(string s) { return ulong.Parse(s, NumberStyles.Number, this.Culture); });
        }

        public bool HasNextSByte()
        {
            return this.HasNext<sbyte>(delegate(string s, out sbyte r) { return sbyte.TryParse(s, NumberStyles.Integer, this.Culture, out r); });
        }

        public sbyte NextSByte()
        {
            return this.Next<sbyte>(delegate(string s) { return sbyte.Parse(s, NumberStyles.Integer, this.Culture); });
        }

        public bool HasNextDateTime()
        {
            return this.HasNext<DateTime>(delegate(string s, out DateTime r)
                        {
                            return DateTime.TryParse(s, this.Culture, DateTimeStyles.None, out r);
                        });
        }

        public DateTime NextDateTime()
        {
            return this.Next<DateTime>(delegate(string s) { return DateTime.Parse(s, this.Culture, DateTimeStyles.None); });
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
        /// <c>true</c> if and only if this scanner has another line of input
        /// </returns>
        public bool HasNextLine()
        {
            // read in characters to our queue until we have 
            // matched our pattern or found a newline
            foreach (char peekChar in new PeekStream(this))
            {
                // if there's any text left, including a newline, then we can process the next line
                return true;
            }

            return false;
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
            bool consumeInput = true;

            StringBuilder sb = new StringBuilder();
            string completedLine = null;

            bool isEof = true;

            // read in characters to our queue until we have a newline
            foreach (char c in new PeekStream(this))
            {
                isEof = false;

                if (c == 0x000d || 
                    c == 0x000a)
                {
                    break;
                }

                sb.Append(c);
            }

            if (isEof)
            {
                // end of the input
                // we haven't found a newline yet, so throw an exception
                throw new InvalidOperationException("Line ending not found");
            }

            completedLine = sb.ToString();

            if (consumeInput)
            {
                this.ConsumeInput(completedLine.Length);
                sb.Remove(0, completedLine.Length);

                // scan for the end of the new line
                do
                {
                    int peek = this.PeekUnconsumedInput();
                    if (peek < 0)
                    {
                        // end of input
                        break;
                    }

                    char peekChar = (char)peek;
                    sb.Append(peekChar);

                    if (IsNewLine(sb.ToString()) == false)
                    {
                        // we passed the new line
                        break;
                    }

                    // consume the character
                    this.ConsumeInput(1);
                }
                while (true);
            }

            return completedLine;
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
        /// Since this method continues to search through the input looking for the specified
        /// pattern, it may buffer all of the input searching for the desired token if no
        /// line separators are present.
        /// </remarks>
        /// <param name="pattern">the regular expression to scan for</param>
        /// <returns>the text that matched the specified regular expression</returns>
        public string FindInLine(Regex pattern)
        {
            StringBuilder sb = new StringBuilder();
            Match localMatch = null;

            // read in characters to our queue until we have 
            // matched our pattern or found a newline
            foreach (char c in new PeekStream(this))
            {
                sb.Append(c);

                if (c == 0x000d ||
                    c == 0x000a)
                {
                    // we've reached a newline so terminate.
                    break;
                }

                // do we have a match yet?
                localMatch = pattern.Match(sb.ToString());
            }

            if (localMatch != null && localMatch.Success)
            {
                this.match = localMatch;
                this.ConsumeInput(localMatch.Index + localMatch.Length);
            }

            if (localMatch == null || localMatch.Success == false)
            {
                // no match was found
                return null;
            }

            // scan ahead for the end of the match or a newline

            int peek = this.PeekUnconsumedInput();
            if (peek < 0)
            {
                // end of input
                return this.match.Value;
            }

            char peekChar = (char)peek;

            if (peekChar == 0x000d ||
                peekChar == 0x000a)
            {
                // we've reached a newline with a match in progress, so consume the newline
                // and then return.
                StringBuilder sbnl = new StringBuilder(2);
                sbnl.Append(peekChar);
                do
                {
                    // consume the character
                    this.ReadUnconsumedInput();
                    this.position++;

                    // peek the next character
                    peek = this.PeekUnconsumedInput();
                    if (peek < 0)
                    {
                        // end of input
                        break;
                    }

                    peekChar = (char)peek;
                    sbnl.Append(peekChar);
                }
                while (IsNewLine(sbnl.ToString()));
            }

            return match.Value;
        }

        private static bool IsNewLine(string s)
        {
            return s == "\n" ||
                   s == "\r" ||
                   s == "\r\n" ||
                   s == Environment.NewLine;
        }

        /// <summary>
        /// Reads from the <see cref="unconsumedChars"/> queue or 
        /// the <see cref="textReader"/> stream.
        /// </summary>
        /// <returns>the next character</returns>
        private int ReadUnconsumedInput()
        {
            return this.unconsumedChars.Count > 0 ? this.unconsumedChars.Dequeue() : this.textReader.Read();
        }

        private void ConsumeInput(int count)
        {
            for (int i = 0; i < count; i++)
            {
                this.ReadUnconsumedInput();
                this.position++;
            }
        }

        /// <summary>
        /// Peeks into the <see cref="unconsumedChars"/> queue or 
        /// the <see cref="textReader"/> stream.
        /// </summary>
        /// <returns>the next character</returns>
        private int PeekUnconsumedInput()
        {
            return this.unconsumedChars.Count > 0 ? this.unconsumedChars.Peek() : this.textReader.Peek();
        }

        private string ReadNextToken(bool consumeInput)
        {
            StringBuilder sb = new StringBuilder();
            string completedToken = null;

            bool preMatchDone = false;
            int skipped = 0;

            // read in characters until we have a match on our delimiter
            foreach (char c in new PeekStream(this))
            {
                sb.Append(c);

                if (preMatchDone == false)
                {
                    // if we are at a delimiter at the beginning of our string, then skip it.
                    Match matchPre = this.Delimiter.Match(sb.ToString());
                    if (matchPre.Success && matchPre.Index == 0)
                    {
                        if (matchPre.Length == sb.Length)
                        {
                            // still matching
                            skipped = sb.Length;
                            continue;
                        }
                        else
                        {
                            preMatchDone = true;
                            skipped = sb.Length - 1;
                        }
                    }
                    else
                    {
                        ////preMatchDone = true;
                        skipped = 0;
                    }
                }

                // do we have a match yet?
                Match match = this.Delimiter.Match(sb.ToString(skipped, sb.Length - skipped));
                if (match.Success)
                {
                    // we have started matching a delimiter
                    // our token is just before the delimiter starts.
                    completedToken = sb.ToString(skipped, match.Index);

                    break;
                }
            }

            if (completedToken == null)
            {
                if (sb.Length - skipped > 0)
                {
                    // scan ahead was terminated by an end of stream
                    completedToken = sb.ToString(skipped, sb.Length - skipped);
                }
                else
                {
                    // we hadn't read anything and we are at the end of the stream
                    return null;
                }
            }

            if (consumeInput)
            {
                this.ConsumeInput(completedToken.Length + skipped);
            }

            return completedToken;
        }

        private string PeekNextToken()
        {
            return this.ReadNextToken(false);
        }

        /// <summary>
        /// Returns true if the next token in this scanner's input can be
        /// interpreted as a value of type T using the <see cref="Next{T}"/>
        /// method.  The scanner does not advance past any input.
        /// </summary>
        /// <typeparam name="T">The value type that is being parsed.  Must have a TryParse member.</typeparam>
        /// <param name="tryParse">The <c>TryParse</c> delegate.  For example <c>double.TryParse</c>.</param>
        /// <returns>
        /// true if and only if this scanner's next token is a valid double value
        /// </returns>
        private bool HasNext<T>(TryParseDelegate<T> tryParse)
        {
            string s = this.PeekNextToken();

            if (s == null)
            {
                return false;
            }

            T dummy;
            return tryParse(s, out dummy);
        }

        /// <summary>
        /// Scans the next token of the input as a <c>T</c>.  This
        /// method will throw <see cref="FormatException"/> if the next
        /// token cannot be translated into a valid double value.  If the
        /// translation is successful, the scanner advances past the
        /// input that matched.
        /// </summary>
        /// <typeparam name="T">The value type that is being parsed.  Must have a TryParse member.</typeparam>
        /// <param name="parse">The <c>Parse</c> delegate.  For example <c>double.Parse</c>.</param>
        /// <returns>
        /// the <see cref="double"/> scanned from the input
        /// </returns>
        /// <remarks>
        /// If the next token matches the Float regular expression defined above then the token is converted into a double value as if by removing all locale specific prefixes, group separators, and locale specific suffixes, then mapping non-ASCII digits into ASCII digits via Character.digit, prepending a negative sign (-) if the locale specific negative prefixes and suffixes were present, and passing the resulting string to Double.parseDouble. If the token matches the localized NaN or infinity strings, then either "Nan" or "Infinity" is passed to Double.parseDouble as appropriate.
        /// </remarks>
        private T Next<T>(ParseDelegate<T> parse)
        {
            string s = this.ReadNextToken(false);

            if (s == null)
            {
                throw new InvalidOperationException();
            }

            T value;
            try
            {
                value = parse(s);
                this.match = RegexSomething.Match(s);
            }
            catch (FormatException ex)
            {
                throw new FormatException(
                    string.Format("{0} [position={1}][token={2}]", ex.Message, this.position, s), ex);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException(
                    string.Format("{0} [position={1}][token={2}]", ex.Message, this.position, s), ex);
            }

            // if the parsing did not throw an exception, then advance the stream
            this.ReadNextToken(true);
            return value;
        }
    }
}
