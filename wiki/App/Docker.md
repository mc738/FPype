<meta name="wikd:title" content="Docker">
<meta name="wikd:name" content="app-docker">
<meta name="wikd:order" content="0">
<meta name="wikd:icon" content="fas fa-plug">

# Docker

The `FPype.App` can be run via Docker.

To do this first you need to set up Docker on the machine
you wish to run the pipeline one.

You can either build `FPype.App` from scratch or use a prebuilt version.

Create a `Dockerfile`. For example:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM base AS final
WORKDIR /app
COPY app /app
ENTRYPOINT ["dotnet", "FPype.App.dll"]
```

For this example the app is in a `app` directory.

The structure looks like

```text
root
|
|-- app
|   |
|   |-- [FPype app files]
|
|-- Dockerfile
```

You can then build the image with the following command:

```shell
sudo docker build -t fpype -f Dockerfile .
```

The app can be run with the following command:

```shell
docker run \
    --name fpype -v [data path on disk]:[data path in container] \
    fpype run -c [config path] -p [output path] -n [pipeline name] -v [pipeline version (optional)]
```

The second line are Docker args, the third line are fpype args.


