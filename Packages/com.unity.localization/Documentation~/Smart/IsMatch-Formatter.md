# Is Match Formatter

Use the [Is Match Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.IsMatchFormatter) to evaluate [regular expressions](https://en.wikipedia.org/wiki/Regular_expression) to control the output.
The Is Match Formatter is not included by default. You can add it to the Formatters in the [Smart Format Settings](../LocalizationSettings.md#formatters).

> [!NOTE]
> In the options part of the formatter, not all regular expression characters are valid. For example, curly braces and parentheses aren’t supported because the same parser is used for all other extensions.

![Diagram showing the breakdown of the Smart String and how each part is evaluated when using IsMatch.](../images/SmartString-IsMatchSyntax.dot.svg)

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>{theKey:ismatch(^.+123.+$):Okay - {}\|No match content}</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-ismatch-1)]

</td>
<td>Okay - Some123Content</td>
</tr>

<tr>
<td>{theKey:ismatch(^.+123.+$):Fixed content if match\|No match content}</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-ismatch-1)]

</td>
<td>Fixed content if match </td>
</tr>

<tr>
<td>{0:list:{:ismatch(^100\|200\|999$):{:0.00}\|'no match'}\|, \| and }</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-ismatch-2)]

</td>
<td>100.00, 200.00 and 'no match'</td>
</tr>

<tr>
<td>{0:list:{:ismatch(^100\|200\|999$):'match'\|'no match'}\|, \| and }</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-ismatch-2)]

</td>
<td>'match', 'match' and 'no match'</td>
</tr>

</table>
