<meta name="wikd:title" content="App">
<meta name="wikd:name" content="app">
<meta name="wikd:order" content="0">
<meta name="wikd:icon" content="fas fa-plug">

# App

The `FPype.App` allows you to run pipelines in a stand alone context.

The following actions are support:

* `import` - Import a configuration into a configuration store, or create a new store if it does not exist.
* `run` - Run a pipeline. 


## Import

The `import` action accepts the following arguments:

* `-c` or `--config` - The configuration store path.
* `-p` or `--path` - The configuration file path.

#### Example

```
fpype import -c "~/fpype/config.db" -p "~/fpype/config.path"
```

## Run

The `run` action accepts the following arguments:

* `-c` or `--config` - The configuration store path.
* `-n` or `--name` - The pipeline's name.
* `-p` or `--path` - The root path for the pipeline's output.
* `-v` or `--version` (optional) - The pipeline version. If not included the latest version will be run.

#### Example

```
fpype import -c "~/fpype/config.db" -n "my_pipeline" -p "~/fpype/runs" -v 1
```


