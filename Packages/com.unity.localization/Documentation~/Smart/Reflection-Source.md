# Reflection source

The [Reflection source](xref:UnityEngine.Localization.SmartFormat.Extensions.ReflectionSource) is used to select using [.Net Reflection](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection). Named placeholders are evaluated against the current object.
The Reflection Source can be used to extract an object from a property, field or method.

> [!NOTE]
> When using a method, the method must be parameterless and not return void.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>The value is {gameObject.name}</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-1)]

</td>
<td>The value is Camera</td>
</tr>

<tr>
<td>You can call methods to modify values such as {0.ToUpper}</td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-2)]

</td>
<td>You can call methods to modify values such as HELLO WORLD</td>
</tr>

<tr>
<td>My name is {Name} and I am {Age} years old.</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-3a)]
[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-3b)]

</td>
<td>My name is Juan Pérez and I am 39 years old.</td>
</tr>

<tr>
<td>Mr {name.ToCharArray.0} {surname}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-4)]

</td>
<td>Mr Y Taro</td>
</tr>
</table>

The following example shows how the `Person` instance could be provided as an argument.

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-reflection-5)]