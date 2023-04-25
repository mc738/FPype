namespace FPype.Infrastructure.Configuration.Common

[<RequireQualifiedAccess>]
module Fetch =
    
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results
    
    let subscriptionById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectSubscriptionRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Subscription (id: {id}) not found"
                   DisplayMessage = "Subscription not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching subscription"
               DisplayMessage = "Error fetching subscription"
               Exception = Some ex })
            |> FetchResult.Failure

    let user (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectUserRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"User (ref: {reference}) not found"
                   DisplayMessage = "User not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching user"
               DisplayMessage = "Error fetching user"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipeline (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline (ref: {reference}) not found."
                   DisplayMessage = "Pipeline not found."
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline"
               DisplayMessage = "Error fetching pipeline"
               Exception = Some ex })
            |> FetchResult.Failure

    
    let pipelineById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectPipelineRecord ctx [ "WHERE id = @0;" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline (id: {id}) not found."
                   DisplayMessage = "Pipeline not found."
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline"
               DisplayMessage = "Error fetching pipeline"
               Exception = Some ex })
            |> FetchResult.Failure

    
    let pipelineLatestVersion (ctx: MySqlContext) (pipelineId: int) =
        try
            Operations.selectPipelineVersionRecord
                ctx
                [ "WHERE pipeline_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ pipelineId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of pipeline (id: {id}) not found"
                   DisplayMessage = "Latest version of pipeline not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching latest pipeline version"
               DisplayMessage = "Error fetching latest pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineVersion (ctx: MySqlContext) (pipelineId: int) (version: int) =
        try
            Operations.selectPipelineVersionRecord
                ctx
                [ "WHERE pipeline_id = @0 AND version = @1;" ]
                [ pipelineId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of pipeline (id: {id}) not found"
                   DisplayMessage = $"Version {version} of pipeline not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline version"
               DisplayMessage = "Error fetching latest pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineVersionRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline version (ref: {ref}) not found"
                   DisplayMessage = "Pipeline versions not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline version"
               DisplayMessage = "Error fetching pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineActions (ctx: MySqlContext) (pipelineVersionId: int) =
        try
            Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_version_id = @0" ] [ pipelineVersionId ]
            |> FetchResult.Success
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline actions"
               DisplayMessage = "Error fetching pipeline actions"
               Exception = Some ex })
            |> FetchResult.Failure

    // Tables
    let table (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableModelRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table (ref: {ref}) not found"
                   DisplayMessage = "Table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table"
               DisplayMessage = "Error fetching table"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectTableModelRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table (id: {ref}) not found"
                   DisplayMessage = "Table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table"
               DisplayMessage = "Error fetching table"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableLatestVersion (ctx: MySqlContext) (tableId: int) =
        try
            Operations.selectTableModelVersionRecord
                ctx
                [ "WHERE table_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ tableId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of table (id: {tableId}) not found"
                   DisplayMessage = "Latest version of table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table latest version"
               DisplayMessage = "Error fetching latest table version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableVersion (ctx: MySqlContext) (tableId: int) (version: int) =
        try
            Operations.selectTableModelVersionRecord
                ctx
                [ "WHERE table_id = @0 AND version = @1;" ]
                [ tableId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {id} of table (id: {tableId}) not found"
                   DisplayMessage = $"Version {id} of table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table version"
               DisplayMessage = "Error fetching table version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableModelVersionRecord
                ctx
                [ "WHERE reference = @0 AND version = @1;" ]
                [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table version (ref: {reference}) not found"
                   DisplayMessage = "Table version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table version"
               DisplayMessage = "Error fetching table version"
               Exception = Some ex })
            |> FetchResult.Failure
            
    let tableColumns (ctx: MySqlContext) (tableVersionId: int) =
        try
            Operations.selectTableColumnRecords ctx [ "WHERE table_version_id = @0" ] [ tableVersionId ]
            |> FetchResult.Success
        with ex ->
            ({ Message = "Unhandled exception while fetching table columns"
               DisplayMessage = "Error fetching table columns"
               Exception = Some ex })
            |> FetchResult.Failure
    

