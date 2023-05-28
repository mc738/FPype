<meta name="wikd:title" content="Data">
<meta name="wikd:name" content="data">
<meta name="wikd:order" content="0">
<meta name="wikd:icon" content="fas fa-plug">

# Data

There are 3 main data structures in `FPype`:

* Unstructured/unprocessed - Data in a unprocessed form, such as a raw file.
* Tabular - This is data in the form of tables, such as a database table.
* Objects - Data made up collection of key value properties (including child objects), such as a `JSON` file.

The goal of a pipeline is to transform data from various sources, gain insights from it and load it into other system. 

At it's core a pipeline can be a ELT process, however the goal is to offer more flexibility.
For example a pipeline might extract and transform data multiple times in any order.

## Expanding data

One of the uses of data pipelines is to 'expand' data. 
For example a table of sales data could by transformed to create a table of sales by month,
sales by category, sales by month and category and so on.
