# Creating a custom source

You can create custom sources using the [ISource](xref:UnityEngine.Localization.SmartFormat.Core.Extensions.ISource) interface.

To use a custom Source, add it to the [Sources](../LocalizationSettings.md#sources) list in the **LocalizationSettings**.

## Random Value Source Example

The following example shows how to create a source that generates a random number. This could then be combined with a [choose Formatter](Choose-Formatter.md) to produce different random responses.

[!code-cs[](../../DocCodeSamples.Tests/RandomValueSource.cs)]

| **Example Smart String**                                | **Result** |
|---------------------------------------------------------|------------|
| {random:choose(1\|2\|3}:Hello\|Greetings\|Welcome\|Hi}! | Welcome!   |

## Literal Text Source Example

The following example shows how to create a source that will convert the source value into a string, this could then be further processed.

[!code-cs[](../../DocCodeSamples.Tests/LiteralTextSource.cs)]

> [!NOTE]
> To use `"` in the selectors it will need to be added to the **Allowed Selector Chars**. (menu: **Edit > Project Settings > Localization > String Database > Smart Format > Parser**)

| **Example Smart String**                                | **Result** |
|---------------------------------------------------------|------------|
| {"This example allows you to use spaces".ToUpper}       | THIS EXAMPLE ALLOWS YOU TO USE SPACES |
| Hello {"Karl:t(highlight)}                              | Hello \<color=red>Karl\</color><br><br>This is using a [template](Template-Formatter.md) in the form `<color=red>{}</color>`.
