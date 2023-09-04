namespace FPype.Infrastructure.Scheduling

open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module Events =

    open System
    open System.Text.Json.Serialization
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core

    type ScheduleEvent =
        | ScheduleCreated of ScheduleCreatedEvent
        | ScheduleUpdated of ScheduleUpdatedEvent
        | ScheduleActivated of ScheduleActivatedEvent
        | ScheduleDeactivated of ScheduleDeactivatedEvent

        static member TryDeserialize(name: string, data: string) =
            match name with
            | _ when name = ScheduleCreatedEvent.Name() ->
                fromJson<ScheduleCreatedEvent> data |> Result.map ScheduleCreated
            | _ when name = ScheduleUpdatedEvent.Name() ->
                fromJson<ScheduleUpdatedEvent> data |> Result.map ScheduleUpdated
            | _ when name = ScheduleActivatedEvent.Name() ->
                fromJson<ScheduleActivatedEvent> data |> Result.map ScheduleActivated
            | _ when name = ScheduleDeactivatedEvent.Name() ->
                fromJson<ScheduleDeactivatedEvent> data |> Result.map ScheduleDeactivated
            | _ ->
                let message = $"Unknowing schedule event type: `{name}`"

                Error(
                    { Message = message
                      DisplayMessage = message
                      Exception = None }
                    : FailureResult
                )

        member se.Serialize() =
            match se with
            | ScheduleCreated data -> toJson data |> Result.map (fun r -> ScheduleCreatedEvent.Name(), r)
            | ScheduleUpdated data -> toJson data |> Result.map (fun r -> ScheduleUpdatedEvent.Name(), r)
            | ScheduleActivated data -> toJson data |> Result.map (fun r -> ScheduleActivatedEvent.Name(), r)
            | ScheduleDeactivated data -> toJson data |> Result.map (fun r -> ScheduleDeactivatedEvent.Name(), r)

    and [<CLIMutable>] ScheduleCreatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("scheduleCron")>]
          ScheduleCron: string }

        static member Name() = "schedule-created"

    and [<CLIMutable>] ScheduleUpdatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("newScheduleCron")>]
          NewScheduleCron: string }

        static member Name() = "schedule-updated"

    and [<CLIMutable>] ScheduleActivatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string }

        static member Name() = "schedule-activated"

    and [<CLIMutable>] ScheduleDeactivatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string }

        static member Name() = "schedule-deactivated"

    let addEvents
        (ctx: MySqlContext)
        (log: ILogger)
        (subscriptionId: int)
        (userId: int)
        (timestamp: DateTime)
        (events: ScheduleEvent list)
        =
        let batchReference = createReference ()

        events
        |> List.fold
            (fun last e ->
                match e.Serialize() with
                | Ok(name, data) ->
                    ({ ScheduleId = subscriptionId
                       EventType = name
                       EventTimestamp = timestamp
                       EventData = data
                       UserId = userId
                       BatchReference = batchReference }
                    : Parameters.NewPipelineScheduleEvent)
                    |> Operations.insertPipelineScheduleEvent ctx

                | Error e -> last)
            0UL
        |> int

    let selectScheduleEventRecords (ctx: MySqlContext) (scheduleId: int) (previousTip: int) =
        Operations.selectPipelineScheduleEventRecords
            ctx
            [ "WHERE schedule_id = @0 AND id > @1" ]
            [ scheduleId; previousTip ]

    let selectAllEventRecordsFromPreviousTip (ctx: MySqlContext) (previousTip: int) =
        Operations.selectPipelineScheduleEventRecords
            ctx
            [ "WHERE id > @1" ]
            [ previousTip ]

    let selectScheduleTip (ctx: MySqlContext) (scheduleId: int) =
        Operations.selectPipelineScheduleEventRecord
            ctx
            [ "WHERE schedule_id = @0 ORDER BY id DESC" ]
            [ scheduleId ]
        |> Option.map (fun er -> er.Id)
        |> Option.defaultValue 0
        
    let selectGlobalTip (ctx: MySqlContext) =
        Operations.selectPipelineScheduleEventRecord ctx [ "ORDER BY id DESC" ] []
        |> Option.map (fun er -> er.Id)
        |> Option.defaultValue 0
