# Default source

The [default source](xref:UnityEngine.Localization.SmartFormat.Extensions.DefaultSource) replicates the [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format) behavior and extracts an argument by the index.

The default source examines the current selector. If the selector can be parsed as an integer and is a valid index (within the number of provided arguments), the default source extracts the argument at the selected index and no further selectors are considered.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>{0} {1} {2}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-one-two-three)]

</td>
<td>1 2 3</td>

<tr>
<td>{1} {1} {2} {0}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-one-two-three)]

</td>
<td>2 2 3 1</td>

<tr>
<td>Player {0} scored {1} points</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-string-int)]

</td>
<td>Player Potato scored 155 points</td>

<tr>
<td>Player {0.Name} score {0.Points}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-anon-class)]

</td>
<td>Player One Scored 100 points</td>

</table>
