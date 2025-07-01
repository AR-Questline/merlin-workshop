# Default Formatter

The Default Formatter performs the same logic as [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format). All cultural formatting uses the Locale from the table that the Smart String belongs to.

| **Example Smart String** | **Arguments**                  | **Result**               |
|--------------------------|--------------------------------|--------------------------|
| The cost is {0:C}        | `1234567.89` - Japanese Locale | The cost is ¥123,457     |
|                          | `1234567.89` - US Locale       | The cost is $123,456.78  |
|                          | `1234567.89` - French Locale   | The cost is 123 456,78 € |
| Today is {0:d}           | `DateTime` - Japanese Locale   | Today is 8/11/2021       |
|                          | `DateTime` - US Locale         | Today is 11/08/2021      |
|                          | `DateTime` - French Locale     | Today is 2021/08/11      |
