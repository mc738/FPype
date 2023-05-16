namespace FPype.Scripting

open FPype.Data
open FsToolbox.Core

[<RequireQualifiedAccess>]
module FSharp =

    open System
    open FPype.Data.Store
    open FPype.Scripting.Core

    /// <summary>
    /// The script context.
    /// To be used in scripts as a way to request data and run actions on a Pipeline store.
    /// </summary>
    type ScriptContext(pipeName) =
        let clientCtx = Client.start pipeName

        interface IDisposable with

            member sc.Dispose() =
                printfn "closing client..."
                // On dispose (i.e. when script has ended) send close message and close the stream.
                Client.close clientCtx

        member _.SendRequest(request: IPC.RequestMessage) = Client.sendRequest clientCtx request


    type PipelineStoreProxy(pipeName) =
        let ctx = Client.start pipeName

        interface IDisposable with

            member sc.Dispose() =
                printfn "closing client..."
                // On dispose (i.e. when script has ended) send close message and close the stream.
                Client.close ctx

        member _.SendRawRequest(request: IPC.RequestMessage) = Client.sendRequest clientCtx request

        member _.AddStateValue(key, value) =
            ({ Key = key; Value = value }: IPC.AddStateValueRequest)
            |> IPC.RequestMessage.AddStateValue
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddStateValue(key, value) =
            ({ Key = key; Value = value }: IPC.UpdateStateValueRequest)
            |> IPC.RequestMessage.UpdateStateValue
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetState() =
            IPC.RequestMessage.GetState
            |> Client.sendRequest ctx
            |> Result.bind (function
                // TODO return type?
                | IPC.ResponseMessage.String s -> Models.State.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetStateValue(key) =
            ({ Key = key }: IPC.GetStateValueRequest)
            |> IPC.RequestMessage.GetStateValue
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.StateValueExists(key) =
            ({ Key = key }: IPC.StateValueExistsRequest)
            |> IPC.RequestMessage.StateValueExists
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Bool b -> Ok b
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetId() =
            IPC.RequestMessage.GetId
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetComputerName() =
            IPC.RequestMessage.GetComputerName
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetUserName() =
            IPC.RequestMessage.GetUserName
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetBasePath() =
            IPC.RequestMessage.GetBasePath
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetImportsPath() =
            IPC.RequestMessage.GetImportsPath
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetExportsPath() =
            IPC.RequestMessage.GetExportsPath
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetTmpPath() =
            IPC.RequestMessage.GetTmpPath
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetStorePath() =
            IPC.RequestMessage.GetTmpPath
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddDataSource(name, dataSourceType: DataSourceType, uri, collectionName) =
            ({ Name = name
               DataSourceType = dataSourceType.Serialize()
               Uri = uri
               CollectionName = collectionName }
            : IPC.AddDataSourceRequest)
            |> IPC.RequestMessage.AddDataSource
            |> Client.sendRequest ctx
            |> Result.bind (function
                // TODO return type?
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetDataSource(name) =
            ({ Name = name }: IPC.GetDataSourceRequest)
            |> IPC.RequestMessage.GetDataSource
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Models.DataSource.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddArtifact(name, bucket, artifactType, data) =
            ({ Name = name
               Bucket = bucket
               Type = artifactType
               Base64Data = Conversions.toBase64 data }
            : IPC.AddArtifactRequest)
            |> IPC.RequestMessage.AddArtifact
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok ()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetArtifact(name)
           ({  }: IPC.GetArtifactRequest)
    


    let executeScript (store: PipelineStore) () =

        // Start pipe server

        // Run script (on background thread?)

        // On main handle requests etc

        // Once script is complete a close signal should be sent

        // Main thread handles close.


        ()
