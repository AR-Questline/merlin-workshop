# Template Formatter

The [Template Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.TemplateFormatter) lets you register reusable templates, and use them by name.
Templates are useful when you want to format text in a particular way across multiple smart strings. When the string needs to be modified, you only need to do it in the template.

![Diagram showing the breakdown of the Smart String and how each part is evaluated.](../images/SmartString-TemplateInspector.png)

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Template</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>The initials are {0:template(initials)}</td>
<td>initials = <code>{name:substr(0,1)} {surname:substr(0,1)}</code></td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-template-formatter-1)]
</td>
<td>The initials are L C</td>
</tr>

<tr>
<td>Hello {0.template(fullName)}</td>
<td>fullName = <code>{name} {surname}</code></td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-template-formatter-1)]

</td>
<td>Hello Lara Croft</td>
</tr>

<tr>
<td>The name is {name:t(highlight)}.</td>
<td>highlight* = <code>&lt;color=red&gt;{}&lt;/color&gt;</code></td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-template-formatter-1)]

</td>
<td>The name is &lt;color=red&gt;Lara&lt;/color&gt;.</td>
</tr>

<tr>
<td>The name is {surname:t(highlight)}.</td>
<td>highlight* =<code>&lt;color=red&gt;{}&lt;/color&gt;</code></td>
<td>

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-template-formatter-1)]

</td>
<td>The name is &lt;color=red&gt;Croft&lt;/color&gt;.</td>
</tr>

</table>

> [!NOTE]
> \* To use the current value in scope, use an empty placeholder **{}**.
