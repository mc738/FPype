<meta name="wikd:title" content="Import">
<meta name="wikd:name" content="actions-import">
<meta name="wikd:order" content="2">
<meta name="wikd:icon" content="fas fa-plug">

# Import actions

Import actions handle importing data in to a pipeline.

## Import file

Import a file.

* Type name: `import_file`
* Introduced in: `v1`
* Current status: `supported`

### Properties

* `path` - The path to the file to be imported.
* `name` - The name of the data source to import to.

### Example

```json
{
  "path": "[file path]",
  "name": "[data source name]"
}
```

### Notes

The imported file will be saved in `$root/runs/[run id]/imports` directory.

A data source of type `file` will be created.

Currently the data source collection for these imports is set to `imports`,
however in the future it is possible this will be added as an optional parameters which will default to `imports`.

## Chunk file

Chunk a file by splitting into smaller files with a specific number of lines.

* Type name: `chunk_file`
* Introduced in: `v1`
* Current status: `experimental`

### Properties

* `path` - The path to the file to be imported.
* `name` - The name of the data source collection to import to.
* `size` - The size of chunks to split the file into.

### Example

```json
{
  "path": "[file path]",
  "name": "[data source collection name]",
  "size": "[chunk size]"
}
```

### Notes

The imported files will be saved in `$root/runs/[run id]/imports` directory.

The files will have a name `[file name]___[chunk index].[file extension`,
where `file name` is the name of the existing file.
So `test.csv` will be create files called `test___0.csv`, `test___1.csv` etc.

A data source of type `file` will be created for each file and will have the collection name from the parameters.
Currently it is best to use these files in actions that operated on a collection of data sources.

It is possible in the future you a name format parameter might be added to allow specific naming of the files.

## Http get

Make a `GET` request to a http end point and save the result.

* Type name: `http_get`
* Introduced in: `v1`
* Current status: `experimental`

### Properties

* `url` - The http end point's url.
* `additionHeaders` (optional) - An array of key/value pairs representing additional headers (see below for properties).
* `name` - The name of the data source for the saved response.
* `responseType` (optional) - The response type. This is used to set the artifact type. Defaults to `txt`.
* `collection` (optional) - The name of the data source collection for the saved response. Defaults to `imports`.

The objects in the `additionalHeaders` array's properties are:

* `key` - The header key.
* `value` - The header value.

### Example

```json
{
  "url": "[file path]",
  "additionHeaders": [
    {
      "key": "[header key]",
      "value": "[header value]"
    }
  ],
  "name": "[data source name]",
  "responseType": "[response type]",
  "collection": "[data source collection name]"
}
```

### Notes

Currently there is no pooling of requests or reuse of the underlying `HttpClient`.