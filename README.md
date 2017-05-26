# MicroYaml
A simple parser for the MicroYaml dialect of the [YAML](http://www.yaml.org/) file format. Distributed as a CodeBit.

MicroYaml is part of the [FileMeta](http://www.filemeta.org) project because it offers a
simple and convenient way to embed structured metadata in convenient locations such as
comments in source code or to add custom metadata fields in the "comments" metadata field
of formats like .mp3.

## About CodeBit
A CodeBit is a way to share common code that's lighter weight than NuGet. CodeBits are contained in one source code file. A structured comment at the beginning of the file indicates where to find the master copy so that automated tools can retrieve and update CodeBits to the latest version. For more information see http://FileMeta.org/CodeBit.html.

This project is the official distribution vehicle for the **MicroYamlReader.cs** CodeBit.

## The MicroYaml Dialect
MicroYaml is a proper subset of YAML. MicroYaml documents consist of one "mapping" or set
of key-value pairs. Keys and values may be in "Simple" or "Block" format with Plain,
"Double-Quoted", and 'Single-Quoted' styles.

Here's a sample:
```yml
# This MicroYaml document expresses the five elements from the Dublin Core
Title: The Hitchhiker's Guide to the Galaxy
Creator: Douglas Adams
Subject: "Fiction"
Description: >
   The misadventures of Arthur Dent, the last surviving man following
   demolition of Planet Earth by a Vogon constructor fleet to make way
M   for a hyperspace bypass.
Date: 1979-07-15
```

MicroYaml does not support lists, nested mappings, complex mapping keys, flow syntax,
strong typing, or other advanced. YAML features. There is presently no plan to add
these features. Those needing more structure than a simple mapping should consider
[XML](http://www.w3.org/XML/), [JSON](http://www.json.org/) or a full YAML parser.

## Why YAML?
YAML is a simple and intuitive format where newlines and indentation are significant to
the parser just as they are to the writer. Most people encountering YAML can successfully
add or edit information without needing to learn the syntax and without creating syntax
errors.

## Why MicroYaml?
While YAML starts out simple, some of the constructs, like Complex Mapping Keys, Compound
Values, and embedded JSON can make it more challenging. Humans may not understand what's
going on and parsers have to produce a complicated DOM to represent the document. In
contrast, MicroYaml keeps things simple and parses to a flat set of key-value pairs.

## Using this MicroYaml parser
This project consists of two source code files:
* **MicroYamlReader.cs** is a self-contained parser suitable for incorporation into a
C# project. Documentation is embedded in the file using triple-slash format.
* **Program.cs** is a set of regression tests for the parser.

Here is sample source code to get you started:
```cs
Dictionary<string, string> yamlDoc = new Dictionary<string, string>();
var options = new Yaml.YamlReaderOptions();
options.MergeDocuments = true;
using (var reader = new StreamReader("MyYamlFile.yml"))
{
   using (var yamlReader = new Yaml.MicroYamlReader(reader, options))
   {
      yamlReader.CopyTo(yamlDoc);
   }
}
```

## Extended MicroYaml Sample
This sample demonstrates most MicroYaml features.

```yml
# YAML comments start with a # sign

# A Key is delimited from a value with a colon and a space. The space is mandatory.
key: value

# In simple format, the value is terminated with a new line.
simple1: simple value
simple2: simple value with ' quotes "" that are preserved literally
simple3: All alphanumeric and many other @-!$*:?${};'""[]~()_+=~`|<> literal characters are acceptable
simple4: simple value # A comment may follow a value. To be a comment, the # must be preceded by a space.
simple5: A colon followed by a space delimits the value from the key.
simple6: When embedded in a simple value, the :colon must not be followed by a space.

# In the following entries, the key is in simple format while the value is in single-quote format.
single-quote1: 'This is the value. ''Embedded single-quotes'' are doubled.'
single-quote2: 'In single-quoted format you   
may have line breaks. Line breaks in this format use line-folding    
meaning that they are converted to a single space character and  
not preserved literally.'

# The following entries, the value is in double-quote format.
double-quote1: "This a double-quote value."
double-quote2: "Unlike single-quote values, double-quote values use \"c-style\" escaping."
double-quote2: "Like single-quote values, double-quote   
values may have embedded line breaks. Also like single-quote   
values the newlines are converted into spaces
and trailing spaces are stripped. To embed
a literal newline, use the \n escape. To embed quotes, use the \"quote\" escape."
double-quote3: "You may escape the \
newline itself thereby supporting trailing spaces and\
embedded newlines."
double-quote4: "Hex \x7E and Unicode \u007B escapes are also supported."

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
 first line of the value to have leading whitespace.

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
 While a single newline
 is replaced with a space.

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
>-
 Folded block key
: value
```