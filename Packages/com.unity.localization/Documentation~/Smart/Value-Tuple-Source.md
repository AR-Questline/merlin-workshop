# Value Tuple source

The [Value Tuple source](xref:UnityEngine.Localization.SmartFormat.Extensions.ValueTupleSource) can iterate through a [Tuple’s](https://docs.microsoft.com/en-us/dotnet/api/system.tuple-7) values, evaluating each item against the sources and formatters.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>It is {Name} {Key}</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-tuple-1)]

</td>
<td>It is Juan Value</td>
</tr>

</table>
