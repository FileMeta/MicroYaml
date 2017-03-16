# MicroYaml
A simple parser for the MicroYaml dialect of the [YAML](http://www.yaml.org/) file format.

MicroYaml is part of the [FileMeta](http://www.filemeta.org) project because it offers a
simple and convenient way to embed structured metadata in convenient locations such as
comments in source code or to add custom metadata fields in the "comments" metadata field
of formats like .mp3.

## The MicroYaml Dialect
MicroYaml is a proper subset of YAML. MicroYaml documents consist of one "mapping" or set
of key-value pairs. Keys and values may be in "Simple" or "Block" format with "Plain",
"Double-Quoted", and "Single-Quoted" styles.

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
AYAML is a simple and intuitive format where newlines and indentation are significant to
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

Here's a sample to get you started:
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
