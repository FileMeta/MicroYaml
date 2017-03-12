using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace Yaml
{
    /// <summary>
    /// <para>Parses MicroYaml documents.
    /// </para>
    /// <para>MicroYaml is subset of the full YAML syntax. It consists of set of
    /// name-value pairs or one "mapping" im YAML parlance. "Flow" syntax, which
    /// emulates JSON, is not included. Names and values are in "Simple" or "Block"
    /// format with Plain, Double-Quoted, and Single-Quoted styles.
    /// </para>
    /// <para>Presently there are no plans to add lists, nested mappings, or other
    /// advanced YAML features. There are other, more capable, YAML parsers available
    /// for those purposes. Besides, JSON may be a better choice when that complexity is
    /// needed.
    /// </para>
    /// <para>For samples of MicroYaml documents, see https://
    /// </para>
    /// <para>For details of the YAML syntax including samples, see "http://yaml.org"
    /// </para>
    /// <para>Fore experimenting with yaml, you may try "http://yaml-online-parser.appspot.com/".</para>
    /// </summary>
    static class MicroYaml
    {
        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="filename">Filename of a MicroYaml document.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int LoadFile(String filename, ICollection<KeyValuePair<string, string>> map)
        {
            using (var reader = new StreamReader(filename, Encoding.UTF8, true))
            {
                return Load(reader, map);
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="doc">MicroYaml document.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int LoadYaml(String doc, ICollection<KeyValuePair<string, string>> map)
        {
            using (var reader = new StringReader(doc))
            {
                return Load(reader, map);
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> loaded with a MicroYaml document.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int Load(Stream stream, ICollection<KeyValuePair<string, string>> map)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return Load(reader, map);
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> loaded with a MicroYaml document.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int Load(TextReader reader, ICollection<KeyValuePair<string, string> > map)
        {
            using (var r = new MicroYamlReader(reader))
            {
                return r.CopyTo(map);
            }
        }
    }

    /// <summary>
    /// <para><see cref="MicroYaml"/> reader. Implements the IEnumerator&lt;string, string&gt; interface
    /// for conveniently reading MicroYaml documents into collections. For details about the document
    /// format, see the <see cref="MicroYaml"/> class.
    /// </para>
    /// </summary>
    /// <seealso cref="MicroYaml"/>
    class MicroYamlReader : IEnumerator<KeyValuePair<string, string>>
    {
        private enum TokenType
        {
            Null,          // No token (yet)
            Scalar,        // A string (typically a key or a value)
            KeyPrefix,     // The '? ' sequence indicating a subsequent key (optional)
            ValuePrefix,   // The ': ' sequence indicating a subsequent value (optional)
            BeginDoc,      // A line containing exclusively '---'
            EndDoc,        // A line containing exclusively '...' 
            EOF            // End of the file 
        }

        TextReader m_reader;
        bool m_closeInput;
        KeyValuePair<string, string> m_current;

        // Current Token
        TokenType m_tokenType;
        string m_token;
        int m_indent;

        public MicroYamlReader(TextReader reader, bool closeInput = false)
        {
            m_reader = reader;
            m_closeInput = closeInput;
            m_tokenType = TokenType.Null;
            m_token = null;
            m_indent = 0;
            InInit();
        }

        public KeyValuePair<string, string> Current
        {
            get
            {
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return m_current;
            }
        }

        public void Dispose()
        {
            if (m_reader != null && m_closeInput)
            {
                m_reader.Dispose();
            }
            m_reader = null;
            m_current = new KeyValuePair<string, string>(null, null);
        }

        public void Reset()
        {
            throw new InvalidOperationException("MicroYamlReader is read-once. It cannot be reset.");
        }

        /// <summary>
        /// Read the next key/value pair from the inbound stream.
        /// </summary>
        /// <returns>True if successful. False if end of document.</returns>
        public bool MoveNext()
        {
            // If beginning of stream, skip a begin document indicator.
            if (m_tokenType == TokenType.Null)
            {
                ReadToken();
                if (m_tokenType == TokenType.BeginDoc)
                {
                    ReadToken();
                }
            }

            // Upon entry, the current token is the NEXT one to be processed and we're looking for a key.

            // === Keep trying until we have obtained a key.
            string key = null;
            do
            {
                switch (m_tokenType)
                {
                    case TokenType.Scalar:
                        key = m_token;
                        ReadToken();
                        break;

                    case TokenType.KeyPrefix:
                        ReadToken();
                        if (m_tokenType != TokenType.Scalar)
                        {
                            ReportError("Expected scalar value.");
                        }
                        // Loop back to process the next token
                        break;

                    case TokenType.ValuePrefix:
                        key = string.Empty;
                        break;

                    case TokenType.BeginDoc:
                    case TokenType.EndDoc:
                    case TokenType.EOF:
                        m_current = new KeyValuePair<string, string>(); // Clear the current value
                        return false;
                }
            }
            while (key == null);

            // === Keep trying until we have obtained a value
            string value = null;
            do
            {
                switch (m_tokenType)
                {
                    case TokenType.Scalar:
                        value = m_token;
                        ReadToken();
                        break;

                    case TokenType.KeyPrefix:
                        value = string.Empty;
                        break;

                    case TokenType.ValuePrefix:
                        ReadToken();
                        if (m_tokenType != TokenType.Scalar)
                        {
                            ReportError("Expected scalar value.");
                        }
                        // Loop back to process the next token
                        break;

                    case TokenType.BeginDoc:
                    case TokenType.EndDoc:
                    case TokenType.EOF:
                        value = string.Empty;
                        break;
                }
            }
            while (value == null);

            m_current = new KeyValuePair<string, string>(key, value);
            return true;
        }

        public bool NextDocument()
        {
            // Clear the current value
            m_current = new KeyValuePair<string, string>();

            // Read the balance of the current document
            while (m_tokenType != TokenType.EOF && m_tokenType != TokenType.BeginDoc && m_tokenType != TokenType.EndDoc)
            {
                ReadToken();
            }

            // If end of document, read the next token
            if (m_tokenType == TokenType.EndDoc)
            {
                ReadToken();
            }

            // If not begin document there are no more documents
            if (m_tokenType != TokenType.BeginDoc)
            {
                return false;
            }

            // Move to the next token
            ReadToken();
            return true;
        }

        public int CopyTo(ICollection<KeyValuePair<string, string> > map)
        {
            int i = 0;
            while(MoveNext())
            {
                map.Add(Current);
                ++i;
            }
            return i;
        }

        #region Parser / Scanner

        private void ReadToken()
        {
            // Keep trying until we successfully read a token
            m_tokenType = TokenType.Null;
            m_token = null;
            for (;;)
            {
                SkipWhitespace();
                char ch = InPeek();

                if (ch == 0)
                {
                    m_tokenType = TokenType.EOF;
                    return;
                }

                else if (ch == '\n')
                {
                    // Skip the newline and find out how far the next line is indented
                    InRead();
                    m_indent = 0;
                    if (ReadMatch("---\n"))
                    {
                        m_tokenType = TokenType.BeginDoc;
                        return;
                    }
                    if (ReadMatch("...\n"))
                    {
                        m_tokenType = TokenType.EndDoc;
                        return;
                    }
                    m_indent = SkipWhitespace();
                    continue;
                }

                else if (ch == '#')
                {
                    // Comment
                    InRead();
                    SkipBalanceOfLine();
                    continue;
                }

                else if (ch == '\'' || ch == '"')
                {
                    ReadQuoteScalar();
                }

                else if (ch == '|' || ch == '>')
                {
                    ReadBlockScalar();
                }

                else if (ReadMatch("? ")) // Key Prefix
                {
                    m_tokenType = TokenType.KeyPrefix;
                    return;
                }

                else if (ReadMatch(": ")) // value prefix
                {
                    m_tokenType = TokenType.ValuePrefix;
                    return;
                }

                else
                {
                    ReadSimpleScalar();
                }

                if (m_tokenType != TokenType.Null) return;
            }
        }

        private void SkipBalanceOfLine()
        {
            // Simply read to the end of the line
            char ch;
            do
            {
                ch = InRead();
            } while (ch != '\0' && ch != '\n');
        }

        private void ReadQuoteScalar()
        {
            // In quote scalars, line breaks are converted to spaces.
            // Leading and trailing spaces on line breaks are stripped.
            // Double-quote scalars use backslash escaping while single-quote scalars allow the quote to be doubled
            char quoteChar = InRead();
            Debug.Assert(quoteChar == '"' || quoteChar == '\'');
            bool doubleQuote = (quoteChar == '"');
            int spaceCount = 0;
            StringBuilder sb = new StringBuilder();
            for (;;)
            {
                char ch = InRead();
                if (ch == '\0') break; // End of file
                if (doubleQuote && ch == '\"') break; // End quote

                if (doubleQuote && ch == '\\')
                {
                    sb.Append(ReadEscape());
                    spaceCount = 0;
                }

                else if (!doubleQuote && ch == '\'')
                {
                    // Doubled single-quotes converted to one single-quote.
                    if (InPeek() == '\'')
                    {
                        InRead();
                        sb.Append('\'');
                        spaceCount = 0;
                    }

                    // Otherwise, the whole scalar has been read
                    else
                    {
                        break;
                    }
                }

                else if (ch == '\n')
                {
                    // Strip leading and trailing spaces
                    if (spaceCount > 0) sb.Remove(sb.Length - spaceCount, spaceCount);
                    for (;;)
                    {
                        ch = InPeek();
                        if (ch != ' ' && ch != '\t') break;
                        InRead();
                    }

                    // Insert one space
                    sb.Append(' ');
                    spaceCount = 1;
                }

                // Add the character and count trailing spaces.
                else
                {
                    sb.Append((char)ch);
                    if (ch == ' ' || ch == '\t')
                        ++spaceCount;
                    else
                        spaceCount = 0;
                }
            }

            // Return the result
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();
        }

        private void ReadBlockScalar()
        {
            char blockChar = InRead();
            Debug.Assert(blockChar == '|' || blockChar == '>');
            bool fold = (blockChar == '>');

            // Read indent value if any
            int indent = 0;
            if (char.IsDigit(InPeek()))
            {
                indent = InRead() - '0';
            }

            // Read chomp type if any
            char chomp = '\0';
            char ch = InPeek();
            if (ch == '-' || ch == '+')
            {
                chomp = InRead();
            }

            // Skip to the end of the line. Only whitespace and comment should appear.
            SkipWhitespace();
            ch = InPeek();
            if (ch != '#' && ch != '\n')
            {
                ReportError("Expected comment or newline.");
            }
            SkipBalanceOfLine();

            // If not specified, determine the indent level by the indentation of the first line
            if (indent == 0)
            {
                indent = SkipWhitespace();
                if (indent == 0)
                {
                    ReportError("YAML: '|' should be followed by one or more indented lines.");
                    m_tokenType = TokenType.Scalar;
                    m_token = string.Empty;
                }
            }

            // Body of value is composed of all lines indented at least as much as the first line.
            // Indent characters are stripped. All other characters are preserved including the concluding \n
            // Embedded comments are not permitted.
            int spaceCount = 0;
            bool justFolded = false;
            StringBuilder sb = new StringBuilder();
            for (;;)
            {
                ch = InRead();
                if (ch == '\0') break;
                if (ch == '\n')
                {
                    // Strip trailing spaces
                    if (spaceCount > 0) sb.Remove(sb.Length - spaceCount, spaceCount);
                    spaceCount = 0;

                    // Read up to indent spaces in the following line
                    int nextIndent;
                    for (nextIndent=0; nextIndent< indent; ++nextIndent)
                    {
                        ch = InPeek();
                        if (ch != ' ' && ch != '\t') break;
                        InRead();
                    }

                    // Scalar ends with a non-empty line that is indented less than "indent" value.
                    if (ch != '\n' && nextIndent < indent)
                    {
                        sb.Append('\n');
                        break; // End of scalar
                    }

                    // Handle line folding (only if didn't just fold a line)
                    if (fold && !justFolded)
                    {
                        sb.Append(' ');
                        ++spaceCount;
                        Debug.Assert(spaceCount == 1);
                        justFolded = true;
                    }
                    else
                    {
                        sb.Append('\n');
                    }
                    continue;
                }
                justFolded = false;

                // Add the character and count trailing white space
                sb.Append(ch);
                if (ch == ' ' || ch == '\t')
                    ++spaceCount;
                else
                    spaceCount = 0;
            }

            // Handle "chomp" options.
            //  chomp == '-': Strip all trailing newlines.
            //  chomp == '\0': Default, strip all but one trailing newline
            //  chomp == '+': Keep all trailing newlines
            if (chomp == '-' || chomp == '\0')
            {
                // Find the end of the text (before the first trailing newline
                int end;
                for (end = sb.Length; end>0; --end)
                {
                    if (sb[end - 1] != '\n') break;
                }

                if (chomp == '\0') ++end;

                if (end < sb.Length) sb.Remove(end, sb.Length - end);
            }

            // Return the result
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();          
        }

        private void ReadSimpleScalar()
        {
            char ch;
            var sb = new StringBuilder();
            for (;;)
            {
                ch = InRead();
                if (ch == '\0' || ch == '\n') break; // EOF or Newline
                if ((ch == ' ' || ch == '\t') && InPeek() == '#') break; // Comment
                if ((ch == ':' || ch == '?') && InPeek() == ' ') break; // Key or value indicator
                sb.Append(ch);
            }

            // Strip trailing whitespace
            int end;
            for (end = sb.Length; end > 0; --end)
            {
                if (!char.IsWhiteSpace(sb[end - 1])) break;
            }
            sb.Remove(end, sb.Length - end);

            // Return the scalar
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();
        }

        private char ReadEscape()
        {
            // The backslash has already been read, handle the rest.
            char ch = InRead();

            switch (ch)
            {
                case '0':
                    return '\0';

                case '\n': // You can literally escape a line-end.
                    return '\n';

                case 'a':
                    return '\a';

                case 'b':
                    return '\b';

                case 't':
                    return '\t';

                case 'n':
                    return '\n';

                case 'v':
                    return '\v';

                case 'f':
                    return '\f';

                case 'r':
                    return '\r';

                case 'e':
                    return '\x1B';

                case 'N':
                    return '\x85'; // Unicode next line

                case '_':
                    return '\xA0'; // Unicode non-breaking space

                case 'L':
                    return '\u2028'; // Unicode Line separator

                case 'P':
                    return '\u2029'; // Unicode Paragraph separator

                case 'x':
                    return ReadHex(2);

                case 'u':
                    return ReadHex(4);

                default:
                    return ch;
            }
        }

        private char ReadHex(int charcount)
        {
            int result = 0;
            while (charcount > 0)
            {
                result *= 16;
                int ch = InPeek();
                if (ch >= '0' && ch <= '9')
                {
                    result += ch - '0';
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    result += (ch - 'A') + 10;
                }
                else if (ch >= 'a' && ch <= 'f')
                {
                    result += (ch - 'a') + 10;
                }
                else
                {
                    break;
                }

                InRead();
                --charcount;
            }

            return (char)result;
        }

        // In YAML, whitespace doesn't include newlines.
        // Depending on context, the number of characters may be significant.
        private int SkipWhitespace()
        {
            int count = 0;
            for (;;)
            {
                char ch = InPeek();
                if (ch != ' ' && ch != '\t') break;
                InRead();
                ++count;
            }
            return count;
        }

        #endregion

        #region Character Reader

        /* The character reader functions return one character at a time
           from the input. If end of file, the functions return '\0' and
           CharEof returns true. A '\0' in the input stream is converted
           to '\uFFFD'. All newline combinations of CR, LF, or CRLF are
           converted to LF ('\n'). This is per HTML5 specs which seem
           to be a reasonable option for YAML as well.

           An unlimited number of characters can be "ungotten" and will be
           returned by future InReads. This makes parsing convenient because
           you can look ahead and then back off if something doesn't match.
        */

        private Stack<char> m_readBuf = new Stack<char>();

        void InInit()
        {
            m_readBuf.Clear();
            m_readBuf.Push('\n');
        }

        char InPeek()
        {
            if (m_readBuf.Count <= 0)
            {
                char ch = InRead();
                if (ch == '\0') return '\0';
                m_readBuf.Push(ch);
                return ch;
            }
            return m_readBuf.Peek();
        }

        char InRead()
        {
            if (m_readBuf.Count > 0)
            {
                return m_readBuf.Pop();
            }

            int ch = m_reader.Read();

            // Normalize newlines according to HTML5 standards
            if (ch == '\r')
            {
                if (m_reader.Peek() == (int)'\n')
                {
                    // Suppress the CR in CRLF
                    ch = (char)m_reader.Read();
                }
                else
                {
                    // Replace CR with LF
                    ch = '\n';
                }
            }

            // Return the value
            if (ch > 0)
            {
                return (char)ch;
            }

            // Per HTML5 convert '\0'
            if (ch == 0)
            {
                return '\xFFFD';
            }

            // EOF
            return '\0';
        }

        void InUnread(char ch)
        {
            Debug.Assert(ch != '\0');
            m_readBuf.Push(ch);
        }

        /// <summary>
        /// Look ahead and see if the text matches. If so, consume the text.
        /// </summary>
        /// <param name="value">The text to match.</param>
        /// <returns>True if there's a match and the text was consumed. Otherwise false.</returns>
        /// <remarks>
        /// A special case is that a '\n' at the end of the value will match end-of-file.
        /// </remarks>
        bool ReadMatch(string value)
        {
            int i;
            for (i=0; i<value.Length; ++i)
            {
                if (value[i] != InPeek()) break;
                ++i;
                InRead();
            }

            // Special case: newline matches EOF
            if (i == value.Length - 1 && value[i] == '\n' && InPeek() == '\0') ++i;

            if (i >= value.Length) return true;

            // Undo the character reads
            while (i > 0)
            {
                --i;
                InUnread(value[i]);
            }
            return false;
        }

        #endregion

        // TODO: Set up a way to collect syntax errors for later reporting.
        // For now, syntax errors are ignored and parsing continues.
        [Conditional("DEBUG")]
        private static void ReportError(string msg, params object[] args)
        {
            Debug.Fail(string.Format(msg, args));
        }
    }

}
