# Dictionary source

The [Dictionary source](xref:UnityEngine.Localization.SmartFormat.Extensions.DictionarySource) extracts objects with a matching Key from an [IDictionary](https://docs.microsoft.com/en-us/dotnet/api/system.collections.idictionary) or [IDictionary<string, object>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.idictionary-).

When evaluating an IDictionary, if the Key is not a string, the Dictionary source first converts it to a string using the object’s ToString method. The case sensitivity of the comparison is determined by the Smart Form [Case Sensitivity](../LocalizationSettings.md#smart-format-settings) option.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>{SomeKey}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-dict-1)]

</td>
<td>999</td>

<tr>
<td>Hello {Name} {Surname}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-dict-2)]

</td>
<td>Hello Gordon Freeman</td>

<tr>
<td>{Name} {Surname} scored {1:Score} points</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-dict-3)]

</td>
<td>Gordon Freeman scored 153 points</td>

</table>

The following example shows how a dictionary could be provided as an argument.

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-dict-example)]
