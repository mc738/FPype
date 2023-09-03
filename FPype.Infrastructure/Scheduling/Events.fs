namespace FPype.Infrastructure.Scheduling

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
            | _ ->
                let message = $"Unknowing configuration event type: `{name}`"

                Error(
                    { Message = message
                      DisplayMessage = message
                      Exception = None }
                    : FailureResult
                )



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
