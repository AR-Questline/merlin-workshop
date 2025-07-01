# Plural Localization Formatter

Languages vary in how they handle plurals of nouns or unit expressions, for example, "hour" vs "hours". Some languages have two forms, like English; some languages have only a single form; and some languages have multiple forms. The [Plural Localization Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.PluralLocalizationFormatter) follows the Unicode [Common Locale Data Repository](https://cldr.unicode.org/) (CLDR) approach to handle [plurals](https://cldr.unicode.org/index/cldr-spec/plural-rules).

CLDR uses short, mnemonic tags for these plural categories:

- zero
- one (singular)
- two (dual)
- few (paucal)
- many (also used for fractions if they have a separate class)
- other (required—general plural form—also used if the language only has a single form)

The Plural Localization Formatter supports **cardinal** pluralization rules to choose different text. A full list of the [plural rules](https://cldr.unicode.org/index/cldr-spec/plural-rules) per locale can be found [here](https://www.unicode.org/cldr/cldr-aux/charts/29/supplemental/language_plural_rules.html).

![Diagram showing the breakdown of the Smart String and how each part is evaluated.](../images/SmartString-PluralFormatterSyntax.dot.svg)

To determine which plural rules to apply, the Plural Localization Formatter uses the locale of the String Table that the smart string is part of. You can override the locale with the Format Option, for example to force English, set (en) as the Format Option:
{0:plural(en):is 1 item|are {} items}

**Note**: The placeholder {} can be used as a shorthand to use the current value.

**Note**: You can use a plural formatter against an IEnumerable value. In this case, the Count of the IEnumerable count is used as the plural value.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>I have {0:plural:an apple\|{} apples}</td>
<td>English Locale:<br><br>10</td>
<td>I have 10 apples</td>
</tr>

<tr>
<td>{0} {0:банан\|{} банана\|{} бананов}</td>
<td>Russian Locale:<br><br>1</td>
<td>1 банана</td>
</tr>

<tr>
<td>{0:p:{} manzana\|{} manzanas}</td>
<td>Spanish Locale:<br><br>2</td>
<td>2 Manzanas</td>
</tr>

<tr>
<td>The following {0:plural:person is\|people are} impressed: {0:list:{}\|, \|, and}.</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-plural-formatter-1)]

</td>
<td>The following people are impressed: bob, and alice.</td>
</tr>

<tr>
<td></td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-plural-formatter-2)]

</td>
<td>The following person is impressed: Mohamed.</td>
</tr>

</table>
