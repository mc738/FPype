namespace FPype.Infrastructure.Scheduling

open FPype.Infrastructure.Core
open FPype.Infrastructure.Core.Persistence

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    module Fetch =

        open FsToolbox.Core.Results
        open Freql.MySql

        let activeSchedules (ctx: MySqlContext) =
            try
                Operations.selectPipelineScheduleRecords ctx [ "WHERE active = TRUE" ] []
                |> FetchResult.Success
            with ex ->
                { Message = "Unhandled exception while fetching active schedules"
                  DisplayMessage = "Error fetching active schedules"
                  Exception = Some ex }
                |> FetchResult.Failure

        let inactiveSchedules (ctx: MySqlContext) =
            try
                Operations.selectPipelineScheduleRecords ctx [ "WHERE active = FALSE" ] []
                |> FetchResult.Success
            with ex ->
                { Message = "Unhandled exception while fetching inactive schedules"
                  DisplayMessage = "Error fetching inactive schedules"
                  Exception = Some ex }
                |> FetchResult.Failure

        let allSchedules (ctx: MySqlContext) =
            try
                Operations.selectPipelineScheduleRecords ctx [] [] |> FetchResult.Success
            with ex ->
                { Message = "Unhandled exception while fetching all schedules"
                  DisplayMessage = "Error fetching all schedules"
                  Exception = Some ex }
                |> FetchResult.Failure

        let getSchedulesBetweenIds (ctx: MySqlContext) (fromId: int) (toId: int) =
            try
                Operations.selectPipelineScheduleRecord ctx [ "WHERE (id <= @0) AND (id >= @1)" ] [ fromId; toId ]
                |> FetchResult.Success
            with ex ->
                { Message = "Unhandled exception while fetching active schedules"
                  DisplayMessage = "Error fetching active schedules"
                  Exception = Some ex }
                |> FetchResult.Failure

        let scheduleById (ctx: MySqlContext) (id: int) =
            try
                Operations.selectPipelineScheduleRecord ctx [ "WHERE id = @0" ] [ id ]
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
                Operations.selectPipelineScheduleRecord ctx [ "WHERE reference = @0" ] [ reference ]
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


    [<RequireQualifiedAccess>]
    module Verification =

        let scheduleIsActive (schedule: Records.PipelineSchedule) _ =
            schedule |> Verification.isActive "Schedule" (fun s -> s.Active)
