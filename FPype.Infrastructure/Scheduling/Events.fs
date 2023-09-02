namespace FPype.Infrastructure.Scheduling

open System
open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
module Events =


    type ScheduleEvent =
        | ScheduleCreated of ScheduleCreatedEvent
        | ScheduleUpdated of ScheduleUpdatedEvent
        | ScheduleActivated of ScheduleActivatedEvent
        | ScheduleDeactivated


    and [<CLIMutable>] ScheduleCreatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("scheduleCron")>]
          ScheduleCron: string }

    and [<CLIMutable>] ScheduleUpdatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("newScheduleCron")>]
          NewScheduleCron: string }

    and [<CLIMutable>] ScheduleActivatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string }

    ()
