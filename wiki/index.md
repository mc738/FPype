<meta name="wikd:title" content="FPype">
<meta name="wikd:name" content="index">
<meta name="wikd:icon" content="fas fa-home">

# FPype

FPype is a tool for building and running data processing pipelines.
Pipelines are build up from a set of actions that are run sequentially.
The goal is to create repeatable pipelines that can be run in a number of places.

Pipelines do not support conditional logic, this is to keep things simpler.
If conditional logic is needed pipelines can be chained together with workflows.

## Store

When a pipeline is running a store is created.
This can hold data at various staging of processing, artifacts, resources and more.
The results can then be exported or loaded in to other systems or databases.

This also means a record is kept of results from intermediary stages when required.

The store is a `SQLite` database, meaning it can easily be queried via `SQL` and view with various tools.

## Actions

Actions are grouped into categories based on their general purpose:

* `Utils` - General utility actions.
* `Import` - Import actions, used to import data into a pipeline.
* `Extract` - Extraction actions, used to extract data from various data sources.
* `Transform` - Transformation actions, used to transform or aggregate data.
* `Export` - Export actions, used to export the data from a pipeline.
* `ML` - Machine learning actions, used to train models and make predictions.
* `Load` - Load actions, used to load data from pipelines into other systems or databases.
* `Visualizations` - Visualization actions, used to generate visualizations from data in the pipeline.

## Data sources

Data sources all you to specific where data will be coming from

## Connectors

## Data modelling
