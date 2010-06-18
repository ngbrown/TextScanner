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
    /// <p>Ported from http://java.sun.com/javase/7/docs/api/java/util/Scanner.html</p>
    /// </remarks>
    public class TextScanner : IDisposable, IEnumerator<string>
    {
        private readonly TextReader textReader;

        private CultureInfo culture;

        private Regex delimiter;

        private string nextToken;

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
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        string IEnumerator<string>.Current
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.
        /// </exception>
        object IEnumerator.Current
        {
            get { return (this as IEnumerator<string>).Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. 
        /// </exception>
        bool IEnumerator.MoveNext()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. 
        /// </exception>
        void IEnumerator.Reset()
        {
            throw new InvalidOperationException("Unable to Reset.");
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
            this.culture = CultureInfo.CurrentUICulture;

            // matches any \s white space character plus the no-break space (U+00A0).
            // see http://msdn.microsoft.com/en-us/library/20bw873z.aspx#WhitespaceCharacter
            this.delimiter = new Regex(@"[\s\xA0]");

            return this;
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
                throw new EndOfStreamException();
            }

            value = this.nextToken;
            this.nextToken = null;
            return value;
        }

        /// <summary>
        /// Sets this scanner's culture to the specified culture.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        public void UseCulture(CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException("cultureInfo");
            }

            this.Culture = cultureInfo;
        }

        /// <summary>
        /// Returns true if the next token in this scanner's input can be
        /// interpreted as a double value using the <see cref="NextDouble"/>
        /// method.  The scanner does not advance past any input.
        /// </summary>
        /// <returns>true if and only if this scanner's next token is a valid double value</returns>
        public bool HasNextDouble()
        {
            string s = this.PeekNextToken();

            if (s == null)
            {
                return false;
            }

            double dummy;
            return double.TryParse(s, NumberStyles.Number, this.Culture, out dummy);
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
            string s = this.PeekNextToken();

            if (s == null)
            {
                throw new EndOfStreamException();
            }

            double value = double.Parse(this.nextToken, NumberStyles.Number, this.Culture);

            // if the parsing throws an exception, don't advance.
            this.nextToken = null;
            return value;
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
        public void Dispose()
        {
            this.textReader.Dispose();
        }

        private void ReadNextToken()
        {
            StringBuilder sb = new StringBuilder();

            do
            {
                int next = this.textReader.Read();
                if (next < 0)
                {
                    break;
                }

                char nextChar = (char)next;
                if (this.Delimiter.IsMatch(nextChar.ToString()))
                {
                    break;
                }

                sb.Append(nextChar);
            } 
            while (true);

            while ((this.textReader.Peek() >= 0) &&
                   this.Delimiter.IsMatch(((char)this.textReader.Peek()).ToString()))
            {
                this.textReader.Read();
            }

            if (sb.Length > 0)
            {
                this.nextToken = sb.ToString();
            }
            else
            {
                this.nextToken = null;
            }
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
            this.Delimiter = new Regex(pattern);
            return this;
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
    }
}
