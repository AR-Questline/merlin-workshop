# Creating a custom formatter

You can create a custom Formatter by inheriting from the [FormatterBase](xref:UnityEngine.Localization.SmartFormat.Core.Extensions.FormatterBase) class.

To use a custom Formatter, add it to the [Formatters](LocalizationSettings.md#formatters) list in the **LocalizationSettings**.

## Byte formatter example

This example shows how to create a formatter to format an integer that represents bytes.

[!code-cs[](../../DocCodeSamples.Tests/ByteFormatter.cs)]

| **Example Smart String**    | **Arguments** | **Result**               |
| --------------------------- | ------------- | ------------------------ |
| The file size is {0:byte()} | `100`         | The file size is 100 B   |
|                             | `1000`        | The file size is 0.98 KB |
|                             | `1234`        | The file size is 1.21 KB |
|                             | `10000000`    | The file size is 9.5 MB  |
|                             | `2000000000`  | The file size is 1.9 GB  |

## Color formatter example

This example creates a formatter to turn a [Color](https://docs.unity3d.com/ScriptReference/Color.html) into an HTML string for use in rich text.

[!code-cs[](../../DocCodeSamples.Tests/ColorFormatterExample.cs)]

| **Example Smart String**    | **Arguments** | **Result**               |
| --------------------------- | ------------- | ------------------------ |
| This is \<color=#{0:color()}\>red\</color\> | `Color.red` | This is <span style="color:red">red</span> |
