<meta name="wikd:title" content="Extract">
<meta name="wikd:name" content="actions-extract">
<meta name="wikd:order" content="3">
<meta name="wikd:icon" content="fas fa-plug">

# Extract actions

Actions to extract data from a source

## Parse csv

Parse a csv file based on a table model. The resulting table will be saving in the `PipelineStore`.

* Type name: `parse_csv`
* Introduced in: `v1`
* Current status: `supported`

### Properties

* `source` - The data source's name.
* `table` - The table model details of the csv

### Example

```json
{
  "source": "[data source name]",
  "table": {
    "name": "[table name]",
    "version": "[table version]"
  }
}
```

### Notes

This uses `Freql.Csv` which (as of version `0.9.0`) does not support multiline delimited csv strings.
See this [issue](https://github.com/mc738/Freql/issues/13) for more details.

If the csv is quite large it can be better to chunk the file first and use `parse_csv_collection` instead.

Currently there is not way to specify if a source contains a header.

The table import handler for a column can be used to specify the format. For example for `DataType` values.
An example of a handler settings to parse `DateTimes` in the format `M/D/YYYY` is:

```json
{
    "handler": "parse_date",
    "format": "M/d/yyyy"
}
```

When a row failed to parse an entry will be added in the `__import_errors` table with details and a row number.
If you notice the action taking a long time to complete it is possible most/all the rows are failing parse.
This can happen if the wrong table model is used for example or if data varies a lot row by row.

If a value can be blank, use an `Option` base type to avoid issues.

If possible it can help to have test run with a smaple of data to make sure each column type is correct, 
formats are right and blank values are handled.