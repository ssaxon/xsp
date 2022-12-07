# XSP Script Engine

This is an implementation of the XSP scripting engine, that deliberately avoids some of the poor design choices in the original which enables us to provide significantly better performance.

> #### Poor design choices?
> One example of a poor design choice in the original XSP script engine was the ability to assign global variables inside of a subroutine. What this meant was that you could have code generating an XSL transform, or even just performing a query, that depended on a global variable that could change at any time. By removing the ability to set global variables, we can cache content in a reliable way and avoid significant reprocessing.

## What is XSP?

XSP, or XML Server Pages, is a scripting language written entirely in XML, and designed to simplify the generation of web content from XML source documents.

The original XSP implementation provided a way to generate web content via a hierarchical output document, where different script functions were responsible for generating content for various parts of the page. That page's script could then be derived from on another page to override on specific area of the page.

The hierarchical output document (and its subsequent conversion to HTML) was adopted by Microsoft in the original ASP.NET 1.0 (aka. Web Controls), where ASP.NET would generate a hierarchy of controls, then perform a render pass over it. The ability to define object-orientation-like hierarchies of pages and override specific areas was subsequently borrowed by Microsoft in ASP.NET 2.0's Master Pages implementation.

## Anatomy of an XSP Script

We talked about how XSP scripts are written in XML. In programming tradition, here is Hello World implemented in XSP:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xsp:script xmlns:xsp="uri:xsp" xmlns:doc="uri:doc">
    <xsp:sub name="main">
        <doc:title>Hello World</doc:title>
    </xsp:sub>
</xsp:script>    
```

The entry point to the script is a subroutine (function) called `main`.

When run, the script above will generate output like this:

```xml
<doc:root xmlns:doc="uri:doc" executionMs="0.342">
    <doc:title>Hello World</doc:title>
</doc:root>
```

Subroutines can call each other and pass parameters using `xsp:call`, passing a URI to the target subroutine:

```xml
<xsp:sub name="main">
    <xsp:call href="#another?message=test" />
</xsp:sub>

<xsp:sub name="another">
    <doc:title file="${script.shortName}">${message} ${also? ?? 'missing'}</doc:title>
</xsp:sub>
```

Here we are accessing the `script` variable (which is a file-scoped variable), and also the `message` variable that was passed in from the caller. We are also looking for a variable called `also` which wasn't provided. By adding `?` after the name we are declaring it to be optional, with the `?? 'missing'` providing the content to use if `also` was `null`.

Now our output looks like this:

```xml
<doc:root xmlns:doc="uri:doc" executionMs="0.420">
    <doc:title file="scripts/default.xsp.xml">test missing</doc:title>
</doc:root>
```

If we changed the call in main to `<xsp:call href="#another?message=test&amp;also=present" />` our output would now say `test present`.

The `${...}` syntax is a newer way to define expressions. The original XSP used a different syntax that was harder to read. The new syntax will feel familiar if you've ever used Java Server Pages, or any modern expression language.

## XSP Language Reference

### &lt;xsp:script&gt;

This is the root-level identifier for an XSP script file. As such, you'll typically see a number of `xmlns` namespace declarations on this element. There are three you'll see consistently:

* `xmlns:xsp="uri:xsp"` defines the namespace of XSP itself
* `xmlns:doc="uri:doc"` defines the namespace of the XML intermediate output document (known as a "meta document")
* `xmlns:xsl="http://www.w3.org/1999/XSL/Transform"` defines the namespace for XSL used in queries. More on this later.

The other attribute you may see on the script is the `base` attribute used to indicate the base script this file was derived from.

```xml
<xsp:script base="base.xsp.xml"
        xmlns:xsp="uri:xsp"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
        xmlns:doc="uri:doc">
    :
</xsp:script>    
```

The `<xsp:script>` element may contain the following:

* `<xsp:sub>` to define a subroutine.
* `<xsp:assign>` to assign a variable with script-level scope.
* `<xsp:xml>` to assign an XML content variable with script-level scope.

### &lt;xsp:sub&gt;

This element defines a subroutine (i.e. a function) in this script. The script element has a single required attribute called `name`.

```xml
<xsp:sub name="main">
    :
</xsp:sub>
```

Subroutines will in turn contain more statements.

The important thing to understand about XSP is that when a function is run, XSP will execute any instructions in the XSP namespace (`<xsp:...>`), and treat everything else as content — whether that be text, comments, or other XML. We saw this behavior earlier when we introduced the `main` function:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xsp:script xmlns:xsp="uri:xsp" xmlns:doc="uri:doc">
    <xsp:sub name="main">
        <doc:title>Hello World</doc:title>
    </xsp:sub>
</xsp:script>    
```

In this example, the `<doc:title>` and its contents were treated as content and written to the ouput meta document:

```xml
<doc:root xmlns:doc="uri:doc" executionMs="0.342">
    <doc:title>Hello World</doc:title>
</doc:root>
```

A second thing to understand is that when you call another subroutine, the output of that function is written into the context we were in when the subroutine was called. For example, the code below would write out the exact same output as the example above:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xsp:script xmlns:xsp="uri:xsp" xmlns:doc="uri:doc">
    <xsp:sub name="main">
        <doc:title><xsp:call href="#title" /></doc:title>
    </xsp:sub>

    <xsp:sub name="title">Hello World</xsp:sub>
</xsp:script>
```

### &lt;xsp:assign&gt;

The assign statement assigns a locally scoped variable. If defined as a child of `<xsp:script>` the variable will be accessible anywhere in that script. Within a subroutine it is only visible in the scope in which it is defined.

`<xsp:assign>` accepts two arguments, both of which are required:

* `name` is the name to give to the variable.
* `value` is the value to assign to the variable (which can be an expression).

```xml
<xsp:script ...>
    <!-- script-scoped variable -->
    <xsp:assign name="userName" value="${query.user}" />

    <xsp:sub name="main">
        <!-- variable visible inside this subroutine -->
        <xsp:assign name="greeting" value="Hello ${userName}" />
    </xsp:sub>
</xsp:script>
```

In addition to variable you define, there are a few variables that are defined globally within the current page request:

* `query` provides access to query string parameters of the requested page
* `dateTime` provides the current UTC date/time in ISO 8601 format, e.g. `2022-12-07T22:53:43.5134970+00:00`
* `locale` provides information about the current locale. The `locale` object exposes the following properties:
    * `locale.l` the language in which to render this page. In this implementation the locale is picked up from the browser path, e.g. `/en/default` will render the `default` script in English, while `/fr/default` will render in French.
* `script` provides details about the running script for debugging purposes. The `script` object exposes the following properties:
    * `script.name` the fully qualified name and path of the current script file, e.g. `/Users/xmlguy/Projects/xsp/xsproot/scripts/base.xsp.xml`
    * `script.path` the fully quality path containing the current script file, e.g. `/Users/xmlguy/Projects/xsp/xsproot/scripts`
    * `script.shortName` the name and path of the current script file relative to the scripts folder, e.g. `scripts/base.xsp.xml`
    * `script.shortPath` the path containing the current script file relative to the scripts folder, e.g. `scripts`

### &lt;xsp:xml&gt;

The `xml` statement assigns a block of XML content to an XML variable. XML variables can be queried like documents. One interesting feature of XML variables is that they may contain `xsp` statements, where those statements then output into the XML variable.

The `xml` statement requires a `name`, and optional `cacheKey` and `cacheDuration` attributes that define the basis for caching the contents of the variable so we don't need to reevaluate it each time. If a `cacheKey` is specified without a duration, we default to caching for 60 seconds.

```xml
<!-- cache products by language for 10 minutes -->
<xsp:xml name="products" cacheKey="product${locale.l}" cacheDuration="00:10:00">
    <xsp:if test="${locale.l != 'en'}">
        <localized><xsp:query src="/data/files/content/${locale.l}/*.xml" /></localized>
    </xsp:if>
    <default><xsp:query src="/data/files/content/en/*.xml" /></default>
    <xsp:query src="/data/files/content/facts.xml" />
</xsp:xml>
```

Here the `cacheKey` is based on the name of the variable and the current language. We are then creating a block of XML which will include a `<localized>` element when the language isn't English, and a `<default>` element based on English content, followed by the entire contents of the `facts.xml` document.

Because this example is querying file contents, all of the files are also assigned as cache dependencies, so if any of the files change, the cache is invalidated, and statements would be reexecuted on the next page request.

### &lt;xsp:call&gt;

The `call` statement is used to call a function, as we saw in some of our earliest examples. `call` requires a `href` attribute, which is a URI containing the name of the subroutine to run, along with any arguments. Because it is a URI, the arguments are specified like query string parameters.

Examples:

* `<xsp:call href="foo.xsp.xml" />` call the `main` subroutine in the `foo.xsp.xml` script
* `<xsp:call href="foo.xsp.xml#named" />` call the `named` subroutine in the `foo.xsp.xml` script
* `<xsp:call href="#named" />` call the `named` subroutine in the **current** script
* `<xsp:call href="#named?greeting=Hello" />` call the `named` subroutine in the **current** script, assigning the variable `greeting` the value `"Hello"`.
* `<xsp:call href="#named?greeting=Hello&amp;arg=2" />` call the `named` subroutine in the **current** script, assigning the variable `greeting` the value `"Hello"` and `arg` the value `2`. The `&amp;` separator is the XML way to express the `&` separator you would see in a query string.

### &lt;xsp:if&gt;

The `if` statement is very similar in behavior and syntax to the same named element in XSL. As such `<xsp:if>` expects a `test` attribute that is some boolean expression that evaluates to `true` or `false`. If `true` the contents of `<xsp:if>` are then executed.

### &lt;xsp:choose&gt;

The `choose` statement was also borrowed from XSL, and is similar to the `switch` statement in C/C++/Java/JavaScript, or the `when` statement in Kotlin.

`<xsp:choose>` contains `<xsp:when>` statements, each of which has a `test` attribute. These statements evaluated in order, when we find the first test that evaluates to `true`, the contents of that `<xsp:when>` are executed.

If none of the tests return `true`, we will execute the contents of the `<xsp:otherwise>` element (similar to the `default` case in a `switch` statement).

```xml
<xsp:choose>
    <xsp:when test="${last == 'Smith'}">the name is Smith</xsp:when>
    <xsp:when test="${last == 'Saxon'}">the name is Saxon</xsp:when>
    <xsp:otherwise>the name is actually ${last}</xsp:otherwise>
</xsp:choose>
```

### &lt;xsp:query&gt;

The `query` statement is used to query an XML data source, which can be a document, a single file path, or a wildcard path referencing multiple files. The `src` attribute identifies the XML data source.

```xml
<!-- contents of an XML variable -->
<xsp:query src="#someVariable" />

<!-- explicit single file -->
<xsp:query src="/data/files/content/facts.xml" />

<!-- wildcard path -->
<xsp:query src="/data/files/content/en/*.xml" />
```

The `select` argument defines an XPath expression that is used to filter the data source, so only the element(s) matching that expression are output from the query. For example, the syntax below would output the single `<fact>` that matches the XPath expression:

```xml
<xsp:query src="/data/files/content/facts.xml" select="//fact[@id='pencil']" />
```

The `query` statement may optionally contain an XSL transformation, which is itself run through the XSP processor, which could allow you to dynamically generate the XSL transform (though this is not recommended!). You can also pass variables into the transformation as parameters, which the XSL transform can then leverage. If you pass in `query` arguments, only the specific query argument's name is used.

In the example below, we enable paging through a dataset using variables from the query string ...

```xml
<xsp:query src="#testData" args="query.page query.count">
    <!-- these parameters can be overriden from the query string -->
    <xsl:param name="page" select="'1'" />
    <xsl:param name="count" select="'10'" />
    
    <xsl:param name="firstIndex" select="1 + ($count * ($page - 1))" />
    <xsl:param name="lastIndex" select="$firstIndex + $count - 1" />
    <xsl:template match="/">
        [<xsl:value-of select="$page" />, <xsl:value-of select="$firstIndex" />-<xsl:value-of select="$lastIndex" />]
        <xsl:apply-templates select="*/foo[(position() &gt;= $firstIndex) and (position() &lt;= $lastIndex)]" />
    </xsl:template>
    <xsl:template match="foo">
        <p><xsl:value-of select="." /></p>
    </xsl:template>
</xsp:query>
```