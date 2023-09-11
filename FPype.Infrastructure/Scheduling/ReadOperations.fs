namespace FPype.Infrastructure.Scheduling

open Freql.MySql

[<RequireQualifiedAccess>]
module ReadOperations =

    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence

    let allEventsInternal (ctx: MySqlContext) (logger: ILogger) (previousTip: int) =
        let rc = Events.selectAllEvents ctx previousTip

        match rc.HasErrors() with
        | true ->
            rc.Errors
            |> List.iter (fun e -> logger.LogError($"Failed to deserialize event. Error: {e}"))
        | false -> ()

        rc.Success |> FetchResult.Success

    let scheduleEventsInternal (ctx: MySqlContext) (logger: ILogger) (scheduleReference: string) (previousTip: int) =
        Fetch.scheduleByReference ctx scheduleReference
        |> FetchResult.map (fun sr ->
            let rc = Events.selectScheduleEvents ctx sr.Id previousTip
            
            match rc.HasErrors() with
            | true ->
                rc.Errors
                |> List.iter (fun e -> logger.LogError($"Failed to deserialize event. Error: {e}"))
            | false -> ()
        
            rc.Success)
    
