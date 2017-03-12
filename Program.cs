/*
---
Title: MicroYaml Unit Tests
Filename: Program.cs
...
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Yaml
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                bool result = CoreTest(m_cTest1, m_cTest1Expected);
                Console.WriteLine("Tests {0}.", result ? "passed" : "failed");
            }
            catch (Exception err)
            {
                Console.WriteLine();
                Console.Write(err.ToString());
            }

            if (Win32Interop.ConsoleHelper.IsSoleConsoleOwner)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit.");
                Console.ReadKey();
            }
        }

        static bool CoreTest(string yaml, IEnumerable<KeyValuePair<string, string>> comp)
        {
            Yaml.MicroYamlReader reader = null;
            IEnumerator<KeyValuePair<string, string>> standard = null;
            bool success = true;
            try
            {
                reader = new MicroYamlReader(new StringReader(yaml));
                standard = comp.GetEnumerator();

                bool eofStandard = false;
                while (reader.MoveNext())
                {
                    if (!eofStandard)
                    {
                        eofStandard = !standard.MoveNext();
                        if (eofStandard)
                        {
                            Console.WriteLine("   Standard at EOF but still input from YAML.");
                            Console.WriteLine();
                            success = false;
                        }
                    }

                    Console.WriteLine("(\"{0}\", \"{1}\")", EscapeString(reader.Current.Key), EscapeString(reader.Current.Value));

                    if (!eofStandard)
                    {
                        if (!string.Equals(reader.Current.Key, standard.Current.Key, StringComparison.Ordinal))
                        {
                            Console.WriteLine("   Keys don't match:\r\n      \"{0}\"\r\n      \"{1}\"", EscapeString(reader.Current.Key), EscapeString(standard.Current.Key));
                            success = false;
                        }
                        if (!string.Equals(reader.Current.Value, standard.Current.Value))
                        {
                            Console.WriteLine("   Values don't match:\r\n      \"{0}\"\r\n      \"{1}\"", EscapeString(reader.Current.Value), EscapeString(standard.Current.Value));
                            success = false;
                        }
                    }
                }

                if (standard.MoveNext())
                {
                    Console.WriteLine("   YAML at EOF but still values in standard.");
                }

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                success = false;
            }
            finally
            {
                if (standard != null)
                {
                    standard.Dispose();
                    standard = null;
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
            }

            return success;
        }

        static string EscapeString(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\n", "\\n");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "\\t");
            return str;
        }

        const string m_cTest0 = @"
Key1: Val1
Key2: Val2
";

        static KeyValuePair<string, string>[] m_cTest0Expected = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("Key1", "Val1"),
            new KeyValuePair<string, string>("Key2", "Val2")
        };


        const string m_cTest1 = @"
# This is a comment followed by a key-value pair.
key: value

# In simple format, the ""scalar"" is terminated with a delimiter or a new line.

simple1: simple-value
simple2: simple value with ' quotes "" that are preserved literally
simple3: All alphanumeric and many other @-!$*:?${};'""[]~()_+=~`|<> literal characters are acceptable
simple4: A comment # May follow the value.
simple5: A colon followed by a space delimits the value. When embedded in a simple value, the :colon must not be followed by a space.

# In the following entries, the name is in simple format while the value is in single-quote format.
single-quote1: 'This is the value. ''Embedded single-quotes'' are doubled up.'
single-quote2: 'In single-quoted format you   
may have line breaks. Line breaks in this format use line-folding    
meaning that they are converted to a single space character and  
not preserved literally.'

# The following entries have values in double-quote format.
double-quote1: ""This a double-quote value.\nIt uses c-style escaping.""
double-quote2: ""Like single-quote values, double-quote   
values may have embedded line breaks. Also like single-quote   
values the newlines are converted into spaces  
and trailing spaces are stripped. To embed
a literal newline, use the \n escape. To embed quotes, use the \""quote\"" escape.""
double-quote3: ""You may escape the \
newline itself thereby supporting trailing spaces and\
embedded newlines.""
double-quote4: ""Hex \x7E and Unicode \u007B escapes are also supported.""

# Literal block format is indicated by the | character
literal1: |
 Values in literal block format must be indented.


 Newlines are significant and preserved.    
 Trailing spaces are trimmed.     

 Indentation beyond the amount of the first line
  is also preserved.
literal2: |-
 A dash after the block indicator means to ""chomp"" the   
 terminating newline in literal block format.
literal3: |+
 A plus after the block indicator means to include the

 terminating newline and any subsequent blank lines.

literal4: |1-
   A numeral after the block indicator indicates the
number of indentation characters thereby allowing the
first of the value to have leading whitespace.

# Folded block format is indicated by the > character
folded1: >
 In folded format, newlines are converted to spaces
 and trailing spaces are trimmed.
folded2: >1-
  Folded format also supports the indentation indicator
 and the chomping indicator.
folded3: >
 In folded format, a newline followed by a blank line

 results in ONE embedded newline.

# All of the preceding examples have used simple keys and
# complex values. But keys may use all of the same formats
# that values can.
simple key: value
'single quote key': value
""double quote
 key"": value
|-
 Literal block key
: value


# A few more edge cases for testing purposes
edge1 : Trailing spaces on a simple key.
""edge2"" : Trailing spaces on a double-quote key.
""edge3 "": Preserved trailing spaces on a double-quote key.
edge4: |
 Literal block followed by empty lines




edge5: |
 Literal block without any trailing empty lines.
edge6: |-
 Literal block with chomping followed by empty lines



edge7: |+
 Literal block with keeping followed by empty lines



edge8: >
 Folded block with trailing empty lines.



Edge9: >-
 Folded block with chomping followed by empty lines




Edge10: >+
 Folded block with keeping followed by empty lines



Edge11: >-
 Folded block with embedded and trailing empty lines


 and chomping.



Edge12: End of file.
";

        static KeyValuePair<string, string>[] m_cTest1Expected = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("key", "value"),
            new KeyValuePair<string, string>("simple1", "simple-value"),
            new KeyValuePair<string, string>("simple2", "simple value with ' quotes \" that are preserved literally"),
            new KeyValuePair<string, string>("simple3", "All alphanumeric and many other @-!$*:?${};'\"[]~()_+=~`|<> literal characters are acceptable"),
            new KeyValuePair<string, string>("simple4", "A comment"),
            new KeyValuePair<string, string>("simple5", "A colon followed by a space delimits the value. When embedded in a simple value, the :colon must not be followed by a space."),
            new KeyValuePair<string, string>("single-quote1", "This is the value. 'Embedded single-quotes' are doubled up."),
            new KeyValuePair<string, string>("single-quote2", "In single-quoted format you may have line breaks. Line breaks in this format use line-folding meaning that they are converted to a single space character and not preserved literally."),
            new KeyValuePair<string, string>("double-quote1", "This a double-quote value.\nIt uses c-style escaping."),
            new KeyValuePair<string, string>("double-quote2", "Like single-quote values, double-quote values may have embedded line breaks. Also like single-quote values the newlines are converted into spaces and trailing spaces are stripped. To embed a literal newline, use the \n escape. To embed quotes, use the \"quote\" escape."),
            new KeyValuePair<string, string>("double-quote3", "You may escape the \nnewline itself thereby supporting trailing spaces and\nembedded newlines."),
            new KeyValuePair<string, string>("double-quote4", "Hex ~ and Unicode { escapes are also supported."),
            new KeyValuePair<string, string>("literal1", "Values in literal block format must be indented.\n\n\nNewlines are significant and preserved.\nTrailing spaces are trimmed.\n\nIndentation beyond the amount of the first line\n is also preserved.\n"),
            new KeyValuePair<string, string>("literal2", "A dash after the block indicator means to \"chomp\" the\nterminating newline in literal block format."),
            new KeyValuePair<string, string>("literal3", "A plus after the block indicator means to include the\n\nterminating newline and any subsequent blank lines.\n\n"),
            new KeyValuePair<string, string>("literal4", "   A numeral after the block indicator indicates the"),
            new KeyValuePair<string, string>("number of indentation characters thereby allowing the", "first of the value to have leading whitespace."),
            new KeyValuePair<string, string>("folded1", "In folded format, newlines are converted to spaces and trailing spaces are trimmed.\n"),
            new KeyValuePair<string, string>("folded2", "  Folded format also supports the indentation indicator and the chomping indicator."),
            new KeyValuePair<string, string>("folded3", "In folded format, a newline followed by a blank line\nresults in ONE embedded newline.\n"),
            new KeyValuePair<string, string>("simple key", "value"),
            new KeyValuePair<string, string>("single quote key", "value"),
            new KeyValuePair<string, string>("double quote key", "value"),
            new KeyValuePair<string, string>("Literal block key", "value"),
            new KeyValuePair<string, string>("edge1", "Trailing spaces on a simple key."),
            new KeyValuePair<string, string>("edge2", "Trailing spaces on a double-quote key."),
            new KeyValuePair<string, string>("edge3 ", "Preserved trailing spaces on a double-quote key."),
            new KeyValuePair<string, string>("edge4", "Literal block followed by empty lines\n"),
            new KeyValuePair<string, string>("edge5", "Literal block without any trailing empty lines.\n"),
            new KeyValuePair<string, string>("edge6", "Literal block with chomping followed by empty lines"),
            new KeyValuePair<string, string>("edge7", "Literal block with keeping followed by empty lines\n\n\n\n"),
            new KeyValuePair<string, string>("edge8", "Folded block with trailing empty lines.\n"),
            new KeyValuePair<string, string>("Edge9", "Folded block with chomping followed by empty lines"),
            new KeyValuePair<string, string>("Edge10", "Folded block with keeping followed by empty lines\n\n\n"),
            new KeyValuePair<string, string>("Edge11", "Folded block with embedded and trailing empty lines\n\nand chomping."),
            new KeyValuePair<string, string>("Edge12", "End of file.")
        };

    }
}
