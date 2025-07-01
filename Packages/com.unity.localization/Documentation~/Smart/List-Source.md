# List Formatter

The [List Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.ListFormatter) is both a Source and a Formatter. You can use it as a Source to extract specific values from an [IList](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ilist) by using an index.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>The value of the item at index 1 is {0.1}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-1)]

</td>
<td>The value of the item at index 1 is 2</td>

<tr>
<td>{0.0} {0.1} {0.2}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-2)]

</td>
<td>1 Hello World</td>

<tr>
<td>The index placeholder can be used to sync multiple lists such as {0:{} = {1.index}\|, }</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-list-3)]

</td>
<td>The index placeholder can be used to sync multiple lists such as 1 = one, 2 = two, 3 = three, 4 = four</td>

</table>
