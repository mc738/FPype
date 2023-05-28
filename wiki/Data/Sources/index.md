<meta name="wikd:title" content="Data sources">
<meta name="wikd:name" content="data-sources">
<meta name="wikd:order" content="0">
<meta name="wikd:icon" content="fas fa-plug">

# Data sources

Data sources are used to specific where data can be pulled from.

For example, when a file is imported with the `import_file` action, a data source is added.

Actions like `parse_csv` and `grok` operator on a data source.

## Collections

Data sources can also be part of a collection. 
The action `chunk_file` for example will break a file down into a series of smaller files,
add a data source for each one and set them to all have the same collection.

Actions like `parse_csv_collection` can operator on a collect rather than individual data sources.

## Storage

Data sources are stored in the `__data_sources` table in the pipeline store.

To manually query them you can use the following `SQL`:

```sql
SELECT `name`, `type`, `uri`, `collection_name`
FROM `__data_sources`;
```

The `PipelineStoreProxy` (using in scripting) has methods to add and get data sources.
