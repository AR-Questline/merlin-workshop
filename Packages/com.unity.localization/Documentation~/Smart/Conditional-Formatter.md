# Conditional Formatter

Conditional formatting occurs on any placeholder that contains a pipe character `|` after the colon `:`.
To invoke the [Conditional Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.ConditionalFormatter) explicitly use the name "conditional" or "cond". It can also be used implicitly when no name is provided.

| **Data Type** | **Syntax** | **Example** |
| ------------- | ---------- | ----------- |
| **Number (Integer, Double, etc...)** | {0:one\|default} | {0} {0:item\|items} |
| | {0} {0:item\|items} | {0:no items\|one item\|many items} |
| | {0:negative\|zero\|one\|default} | {0:negative items\|no items\|one item\|many items}
| **bool** | {0:true\|false} | Enabled? {0:Yes\|No} |
| **string** | {0:default\|null or empty} | Text: {0:{0}\|No text to display}
| **DateTime** | {0:before\|after} (as compared to Date.Now) | My birthday {0:was on\|will be on} {0:MMMM d} |
| **TimeSpan** | {0:negative\|positive} | My birthday {0:was {0} ago\|will be in {0} from now} |
| | {0:negative\|zero\|positive} | My birthday {0:was {0} ago\|is today!\|will be in {0} from now} |
| **object** | {0:default\|nothing} | Property: {0:{0}\|(Nothing)} |

Each parameter is separated by `|`. The comparison is followed by a `?` and then the text. The last (default) entry does not contain a comparison nor a `?`.

The following comparisons are supported:

- **&gt;=**
- **&gt;**
- **=**
- **&lt;**
- **&lt;=**
- **!=**

To combine comparisons, use `&` for AND or `/` for OR.

<table>
<tr>
<th><strong>Example Smart String</strong></th>
<th><strong>Arguments</strong></th>
<th><strong>Result</strong></th>
</tr>

<tr>
<td>{0:cond:>10?Greater Than 10\|=10?Equals to 10\|Less than 10}</td>
<td><code>5</code></td>
<td>Less than 10</td>

<tr>
<td>{Age:cond:&gt;=55?Senior Citizen|&gt;=30?Adult|&gt;=18?Young Adult|&gt;12?Teenager|&gt;2?Child|Baby}</td>
<td> 

[!code-cs[](../../DocCodeSamples.Tests/SmartStringSamples.cs#args-cond-1)]

</td>
<td>Adult</td>
</tr>

</table>
