# Pipeline action examples

## General

These properties can be found in various actions.

### Tables

***TODO***

### Queries

***TODO***

### Data groups

## Import

### Import file

* Name: `import_file`
* Description: Import a file.

#### Properties

* `path` - The path to the file to be imported.
* `name` - The name of the data source to import to.

#### Example

```json
{
  "path": "[file path]",
  "name": "[data source name]"
}
```

#### Notes

The imported file will be saved in `$root/runs/[run id]/imports` directory.

A data source of type `file` will be created.

## Extract

### Parse csv

* Name: `parse_csv`
* Description: Parse a csv file into a table

#### Properties

* `source`: The name of the data source 
* `table`: The table to create from the parsed data (see above)

#### Example

```json
{
    "source": "[data source name]",
    "table": {
        "name": "[table name]",
        "version": 1
    }
}
```

#### Notes

The data source must be file in csv format (either local or remote).

This will create a new table based on the table property.

### Grok

***TODO***

## Transform

### Aggregate

* Name: `aggregate`
* Description: Select values from a table model via a query and save them to a new table

#### Properties

* `query`: The query to gather results
* `table`: The table to create from the parsed data (see above)

#### Example

```json
{
  "query": {
    "name": "by_sub_category",
    "version": 1
  },
  "table": {
    "name": "by_sub_category",
    "version": 1
  }
}
```

#### Notes

This action basically runs a query to select results and save them to a new table.

### Aggregate by date

* Name: `aggregate_by_date`
* Description: Select fields from table model, aggregate then by date groups and save them to a new table.

#### Properties

* `dataGroups`: The date groups to aggregate results by (see `dataGroups` above)
* `query`: The basic query to select fields (see `queries` above)
* `table`: The output table model for the results (see `tables` above)
#### Example

```json
{
  "dateGroups": {
    "type": "months",
    "start": "2014-01-01",
    "length": 48,
    "fieldName": "order_date",
    "label": "month"
  },
  "query": {
    "name": "by_date",
    "version": 1
  },
  "table": {
    "name": "by_date",
    "version": 1
  }
}
```

#### Notes

N/A

### Aggregate by date and category

* Name: `aggregate_by_date_and_category`
* Description: Aggregate values from a query but date and a category (based on a table field)

#### Properties

* `dataGroups`: The date groups to aggregate results by (see `dataGroups` above)
* `query`: The basic query to select fields (see `queries` above)
* `categoryField`: The field that will be used as the category to aggregate on
* `table`: The output table model for the results (see `tables` above)

#### Example

```json
{
  "dateGroups": {
    "type": "months",
    "start": "2014-01-01",
    "length": 48,
    "fieldName": "order_date",
    "label": "month"
  },
  "query": {
    "name": "by_category_and_date",
    "version": 1
  },
  "categoryField": "category",
  "table": {
    "name": "by_category_and_date",
    "version": 1
  }
}
```

#### Notes

N/A

### Map to object

* Name: `map_to_object`
* Description: Map a query (and related nested queries) to a `json` object.

#### Properties

* `mapper`: The name of the data source
* `verison` (optional): The mapper version to use. If left out the latest version will be used.

#### Example

```json
{
  "mapper": "[mapper name]",
  "version": 1
}
```

#### Notes

N/A

## Load

***Coming soon***

### Export

***Coming soon***