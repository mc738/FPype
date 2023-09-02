namespace FPype.Infrastructure.Scheduling

open System
open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
module Events =


    type ScheduleEvent =
        | ScheduleCreated of ScheduleCreatedEvent
        | ScheduleUpdated of ScheduleUpdatedEvent
        | ScheduleActivated of ScheduleActivatedEvent
        | ScheduleDeactivated of ScheduleDeactivatedEvent


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

    and [<CLIMutable>] ScheduleDeactivatedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string }
