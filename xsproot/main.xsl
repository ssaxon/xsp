<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet
        version="2.0"
        exclude-result-prefixes="doc"
        xmlns:doc="uri:doc"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" doctype-public="" version="4.0" encoding="UTF-8" indent="no" />

    <xsl:template match="/">
        <xsl:apply-templates select="doc:root" />
    </xsl:template>

    <xsl:template match="doc:root">
        <html>
            <head>
            <title><xsl:value-of select="doc:title" /></title>
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0-beta1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-0evHe/X+R7YkIZDRvuzKMRqM+OrBnVFBL6DOitfPri4tjfHxaWutUpFmBp4vmVor" crossorigin="anonymous" />
            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0-beta1/dist/js/bootstrap.bundle.min.js" integrity="sha384-pprn3073KE6tl6bjs2QrFaJGz5/SUsLqktiwsUTF55Jfv3qYSDhgCecCxMW52nD2" crossorigin="anonymous"></script>
            </head>
            <body>
                <xsl:apply-templates select="doc:header" />
                <xsl:apply-templates select="doc:body" />
                <xsl:apply-templates select="doc:footer" />
                <code>Execution time: <xsl:value-of select="/*/@executionMs" />ms</code>
            </body>
        </html>
    </xsl:template>

    <xsl:template match="doc:header">
        <nav class="navbar navbar-expand-lg bg-light">
            <div class="container">
                <a class="navbar-brand" href="#">Navbar</a>
            </div>
        </nav>
    </xsl:template>

    <xsl:template match="doc:body">
        <xsl:apply-templates select="doc:content" />
    </xsl:template>

    <xsl:template match="doc:content">
        <div class="container">
            <h1>Products</h1>
            <xsl:copy-of select="p" />

            <xsl:for-each select="products/product">
                <h2><xsl:value-of select="@title" /></h2>
                <code><xsl:value-of select="@sourceLastModified" /></code>
                <p><xsl:copy-of select="description/node()" /></p>
                <xsl:apply-templates select="facts" />
            </xsl:for-each>
        </div>
    </xsl:template>

    <xsl:template match="facts">
        <p>
            <b>Width</b>: <xsl:value-of select="@width" />cm;
            <b>Length</b>: <xsl:value-of select="@length" />cm;
            <b>Height</b>: <xsl:value-of select="@height" />cm;
            <b>Weight</b>: <xsl:value-of select="@weight" />g
        </p>
    </xsl:template>
</xsl:stylesheet>
