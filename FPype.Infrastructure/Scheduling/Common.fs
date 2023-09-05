namespace FPype.Infrastructure.Scheduling

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    module Fetch =

        open FsToolbox.Core.Results        
        open Freql.MySql
        open FPype.Infrastructure.Core
        open FPype.Infrastructure.Core.Persistence

        let activeSchedules (ctx: MySqlContext) =
            try
                Operations.selectPipelineScheduleEventRecord ctx [ "WHERE active = TRUE" ] []
                |> FetchResult.Success
            with ex ->
                { Message = "Unhandled exception while fetching active schedules"
                  DisplayMessage = "Error fetching active schedules"
                  Exception = Some ex }
                |> FetchResult.Failure
        
        let scheduleById (ctx: MySqlContext) (id: int) =
            try
                Operations.selectPipelineScheduleEventRecord ctx [ "WHERE id = @0" ] [ id ]
                |> Option.map FetchResult.Success
                |> Option.defaultWith (fun _ ->
                    ({ Message = $"Schedule (id: {id}) not found"
                       DisplayMessage = "Schedule not found"
                       Exception = None }
                    : FailureResult)
                    |> FetchResult.Failure)
            with ex ->
                { Message = "Unhandled exception while fetching schedule"
                  DisplayMessage = "Error fetching schedule"
                  Exception = Some ex }
                |> FetchResult.Failure

        let scheduleByReference (ctx: MySqlContext) (reference: string) =
            try
                Operations.selectPipelineScheduleEventRecord ctx [ "WHERE reference = @0" ] [ reference ]
                |> Option.map FetchResult.Success
                |> Option.defaultWith (fun _ ->
                    ({ Message = $"Schedule (ref: {reference}) not found"
                       DisplayMessage = "Schedule not found"
                       Exception = None }
                    : FailureResult)
                    |> FetchResult.Failure)
            with ex ->
                { Message = "Unhandled exception while fetching schedule"
                  DisplayMessage = "Error fetching schedule"
                  Exception = Some ex }
                |> FetchResult.Failure


    