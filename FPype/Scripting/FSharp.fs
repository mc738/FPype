namespace FPype.Scripting

open FPype.Data
open FPype.Scripting.Core
open FsToolbox.Core

[<RequireQualifiedAccess>]
module FSharp =

    open System
    open FPype.Data.Store
    open FPype.Scripting.Core

    [<AutoOpen>]
    module private Helpers =

        let deserializeOption<'T> (fn: string -> Result<'T, string>) (str: string) =
            match String.IsNullOrWhiteSpace(str) with
            | true -> Ok None
            | false -> fn str |> Result.map Some

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

        member _.SendRawRequest(request: IPC.RequestMessage) = Client.sendRequest ctx request

        member _.AddStateValue(key, value) =
            ({ Key = key; Value = value }: IPC.AddStateValueRequest)
            |> IPC.RequestMessage.AddStateValue
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.UpdateStateValue(key, value) =
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
                | IPC.ResponseMessage.String s ->
                    match String.IsNullOrWhiteSpace s with
                    | true -> None
                    | false -> Some s
                    |> Ok
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
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetDataSource(name) =
            ({ Name = name }: IPC.GetDataSourceRequest)
            |> IPC.RequestMessage.GetDataSource
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> deserializeOption Models.DataSource.Deserialize s
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
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetArtifact(name) =
            ({ Name = name }: IPC.GetArtifactRequest)
            |> IPC.RequestMessage.GetArtifact
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> deserializeOption Models.Artifact.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetArtifactBucket(name) =
            ({ Name = name }: IPC.GetArtifactBucketRequest)
            |> IPC.RequestMessage.GetArtifactBucket
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Models.ArtifactBucket.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddResource(name, resourceType, data) =
            ({ Name = name
               Type = resourceType
               Base64Data = Conversions.toBase64 data }
            : IPC.AddResourceRequest)
            |> IPC.RequestMessage.AddResource
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetResource(name) =
            ({ Name = name }: IPC.GetResourceRequest)
            |> IPC.RequestMessage.GetResource
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> deserializeOption Models.Resource.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddCacheItem(name, ttl, data) =
            ({ Name = name
               Base64Data = Conversions.toBase64 data
               Ttl = ttl }
            : IPC.AddCacheItemRequest)
            |> IPC.RequestMessage.AddCacheItem
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.GetCacheItem(name) =
            ({ Name = name }: IPC.GetCacheItemRequest)
            |> IPC.RequestMessage.GetCacheItem
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> deserializeOption Models.CacheItem.Deserialize s
                | r -> Error $"Invalid response type `{r}`.")

        member _.DeleteCacheItem(name) =
            ({ Name = name }: IPC.DeleteCacheItemRequest)
            |> IPC.RequestMessage.DeleteCacheItem
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddResult(step, result, startUtc, endUtc, serial) =
            ({ Step = step
               Result = result
               StartUtc = startUtc
               EndUtc = endUtc
               Serial = serial }
            : IPC.AddResultRequest)
            |> IPC.RequestMessage.AddResult
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddImportError(step, error, value) =
            ({ Step = step
               Error = error
               Value = value }
            : IPC.AddImportErrorRequest)
            |> IPC.RequestMessage.AddImportError
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.AddVariable(name, value) =
            ({ Name = name; Value = value }: IPC.AddVariableRequest)
            |> IPC.RequestMessage.AddVariable
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.SubstituteValue(value) =
            ({ Value = value }: IPC.SubstituteValueRequest)
            |> IPC.RequestMessage.SubstituteValues
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.String s -> Ok s
                | r -> Error $"Invalid response type `{r}`.")

        member _.CreateTable(table) =
            ({ Table = table }: IPC.CreateTableRequest)
            |> IPC.RequestMessage.CreateTable
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.InsertRows(table) =
            ({ Table = table }: IPC.InsertRowsRequest)
            |> IPC.RequestMessage.InsertRows
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.SelectRows(table, query, parameters) =
            ({ Table = table
               QuerySql = query
               Parameters = parameters }
            : IPC.SelectRowsRequest)
            |> IPC.RequestMessage.SelectRows
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Rows rows -> Ok rows
                | r -> Error $"Invalid response type `{r}`.")

        member _.Log(step, message) =
            ({ Step = step; Message = message }: IPC.LogRequest)
            |> IPC.RequestMessage.Log
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.LogError(step, message) =
            ({ Step = step; Message = message }: IPC.LogErrorRequest)
            |> IPC.RequestMessage.LogError
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

        member _.LogWarning(step, message) =
            ({ Step = step; Message = message }: IPC.LogWarningRequest)
            |> IPC.RequestMessage.LogWarning
            |> Client.sendRequest ctx
            |> Result.bind (function
                | IPC.ResponseMessage.Acknowledge -> Ok()
                | r -> Error $"Invalid response type `{r}`.")

    module ScriptHost =
        open System.IO
        open System.Text
        open FSharp.Compiler.Interactive.Shell
        open Freql.Sqlite

        let fsiSession _ =
            // Initialize output and input streams
            let sbOut = new StringBuilder()
            let sbErr = new StringBuilder()
            let inStream = new StringReader("")
            let outStream = new StringWriter(sbOut)
            let errStream = new StringWriter(sbErr)

            // Build command line arguments & start FSI session
            let argv = [| "C:\\fsi.exe" |]

            let allArgs = Array.append argv [| "--noninteractive" |]

            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()

            FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)

        let eval (path: string) (name: string) (fsi: FsiEvaluationSession) =
            try
                match fsi.EvalScriptNonThrowing path with
                | Choice1Of2 _, diagnostics ->

                    match fsi.EvalExpressionNonThrowing name with
                    | Choice1Of2 r, diagnostics ->
                        //printfn $"Evaluate script diagnostics: {diagnostics}"
                        //printfn $"Evaluate expression diagnostics: {diagnostics}"
                        // NOTE - assumed a script will return nothing.
                        Ok()
                    (*
                        r
                        |> Option.bind (fun v -> v.ReflectionValue |> unbox<'T> |> Ok |> Some)
                        |> Option.defaultValue (Result.Error "No result")
                        *)
                    | Choice2Of2 exn, diagnostics ->
                        printfn $"Error evaluating expression: {exn.Message}"
                        printfn $"Evaluate expression diagnostics: {diagnostics}"
                        Result.Error exn.Message
                | Choice2Of2 exn, diagnostics ->
                    printfn $"Error evaluating script: {exn.Message}"
                    printfn $"Evaluate script diagnostics: {diagnostics}"
                    Result.Error exn.Message
            with exn ->
                Error $"Unhandled error: {exn.Message}"

        type HostContext =
            { FsiSession: FsiEvaluationSession }

            static member Create() = { FsiSession = fsiSession () }

            member hc.Eval<'T>(path, name) = eval path name hc.FsiSession


    let executeScript (ctx: ScriptHost.HostContext) (path: string) (name: string) (pipeName: string) =

        // NOTE assuming there is a "execute" function

        ctx.Eval(path, $"{name} \"{pipeName}\"")

// Start pipe server

// Run script (on background thread?)

// On main handle requests etc

// Once script is complete a close signal should be sent

// Main thread handles close.
