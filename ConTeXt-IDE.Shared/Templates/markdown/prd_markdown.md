---
Title: Markdown to ConTeXt
Date: 2022/04/17
Author: wdev
---

# Install Pandoc

- Go to https://github.com/jgm/pandoc/releases/latest to install Pandoc.
- Restart the IDE
- In the IDE, Navigate to ConTeXt --> Manage Modules
- Install the filter module

# Compile Markdown -> TeX -> PDF

You can now write your texts in Markdown using the internal Markdown previewer!

The content of the .md file gets parsed by Pandoc to a .tex file, which gets included to your document and compiled by \ConTeXt.

You can use many \ConTeXt\ commands \cite{hagen}, Pandoc will just pass them through:

$$ y(x) = m \cdot x + c $$

## Text

> Markdown font *attributes* will _show_ in the **preview** and in the ***compiled*** ~~pdf~~.

Superscripts and subscript don't seem to work with Pandoc:

H<sub>3</sub>O^+ 

## Itemize

The markdown live previewer seems to have some issues displaying nested items:

- Item 1
- Item 2
	* Bullet 1 in list item 2
	* Bullet 2 in list item 2
- Item 3

## Images

In order to see Images in the preview and in the compiled output, please use a path relative to the current .md file like so:

![This is the figure caption](pictures/NyquistPlot1.png){width=50%}

Some stuff like link attributes are Pandoc-specific, that is why "{width=50%}" has no effect in the live preview.

## Code

``` csharp
public static void Main(string[] args)
{
  Console.WriteLine("Hello world!");
}
```

The compile time (as always) scales with the document size, so you might want to split your .md files (= individual sections) when they exceed ~1000 lines.

## Subsections

### Subsubsection

#### Subsubsubsection

If you want to include every section depth, just remove the \type{\setupcombinedlist[content][]} command.

##### Subsubsubsubsection

## Tables

Table: This is the table caption

| Right | Left | Default | Center |
|------:|:-----|---------|:------:|
|   12  |  12  |    12   |    12  |
|  123  |  123 |   123   |   123  |
|    1  |    1 |     1   |     1  |
