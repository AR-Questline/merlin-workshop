# Choose Formatter

Use the [Choose Formatter](xref:UnityEngine.Localization.SmartFormat.Extensions.ChooseFormatter) to add logic to a Smart String by selecting an output, using a provided set of choices. To invoke the Choose Formatter explicitly, use the name "choose" or "c".

![Diagram showing the breakdown of the Smart String when using a Choose formatter.](../images/SmartString-ChooseSyntax.dot.svg)

| **Example Smart String**                              | **Argument**  | **Result**  |
|-------------------------------------------------------|---------------|-------------|
| {0:choose(1\|2\|3):one\|two\|three\|other}            | 2             | two         |
| {0:choose(1\|2\|3):one\|two\|three\|other}            | 1             | one         |
| {0:choose(1\|2\|3):one\|two\|three\|other}            | 5             | other       |
| {0:choose(True\|False):yes\|no}                       | true          | yes         |
| {0:choose(null): NULL \| {} }                         | `null`        | NULL        |
| How {0:choose(Male\|Female):is he\|is she\|are they}? | "Male"        | How is he?  |
| It is {day:choose(1\|2\|3\|4\|5):Mon\|Tue\|Wed\|Thur\|Fri\|the weekend}.                    | 7 | It is the weekend.   |
| Es {day:choose(1\|2\|3\|4\|5):lunes\|martes\|miércoles\|jueves\|viernes\|el fin de semana}. | 7 | Es el fin de semana. |

![Example of a Smart String to show the days of the week. A numeric value is converted into a day string representation using the choose formatter.](../images/SmartString-ChooseDays.gif)


- The value can be anything. It will be converted to string (using [ToString](https://docs.microsoft.com/en-us/dotnet/api/system.object.tostring)), and then matched against the choices. This works great for numbers, booleans, strings, enums, and most simple types.
- Choices are case-sensitive by default.
- `null` is supported too: `{0:choose(null): NULL | NOT NULL }`
