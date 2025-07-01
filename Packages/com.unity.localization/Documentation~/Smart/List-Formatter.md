# List Formatter

The [List Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.ListFormatter) is a Source and a Formatter. You can use it as a Formatter to repeat the contents of any [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable), such as an Array or List.

![Diagram showing the breakdown of the Smart String and how each part is evaluated.](../images/SmartString-ListFormatterSyntax.dot.svg)

The spacers can also include character literals, for example, instead of a comma you could also use **\n** to list each item on a new line.
Use the placeholder index to access the current index of the list being iterated. You can use this to access multiple lists.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>{0:list:{}|, |, and }</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-1)]

</td>
<td>one, two, and three</td>

<tr>
<td>{theKey:ismatch(^.+123.+$):Fixed content if match|No match content}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-2)]

</td>
<td>Fixed content if match</td>

<tr>
<td>{0:list:{:ismatch(^100|200|999$):{:0.00}|'no match'}|, | and }</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-3)]

</td>
<td>100.00, 200.00 and 'no match'</td>

<tr>
<td>{0:list:{:ismatch(^100|200|999$):'match'|'no match'}|, | and }</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-3)]

</td>
<td>'match', 'match' and 'no match'</td>

<tr>
<td>The index placeholder can be used to sync multiple lists such as:

{0:{} = {1.index}|, }</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-4)]

</td>
<td>The index placeholder can be used to sync multiple lists such as:<br><br>1 = one, 2 = two, 3 = three, 4 = four
</td>

</table>

## Localized String Example

This example shows how to pass a list as an argument to a LocalizedString.

> [!NOTE]
> The list must be one of the arguments, in this example it is argument 0.

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-formatter-loc-string-example)]
