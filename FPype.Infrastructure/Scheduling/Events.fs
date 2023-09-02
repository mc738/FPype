namespace FPype.Infrastructure.Scheduling

open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
module Events =


    type ScheduleEvent =
        | ScheduleCreated of ScheduleCreatedEvent
        | ScheduleUpdated
        | ScheduleActivated
        | ScheduleDeactivated


    and [<CLIMutable>] ScheduleCreatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("scheduleCron")>]
          ScheduleCron: string }

    

    ()
