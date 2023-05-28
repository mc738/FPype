<meta name="wikd:title" content="Connectors">
<meta name="wikd:name" content="data-connectors">
<meta name="wikd:order" content="0">
<meta name="wikd:icon" content="fas fa-plug">

## Data connectors

Data connectors allow a pipeline to connect to various external sources.
These can be used to import or export data.

These will normally be wrapped up in actions like `query_sqlite_database` 
and `save-query-result-to-sqlite-database`. A pipeline will not have to call then directly.

However, they are useful when developing scripts or plugins.

