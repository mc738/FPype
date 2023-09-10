namespace FPype.Infrastructure.Services

open FPype.Infrastructure.Scheduling
open Microsoft.Extensions.Logging
open Freql.MySql
open FPype.Infrastructure

type SchedulingService(ctx: MySqlContext, log: ILogger<SchedulingService>) =
    
    member _.AddSchedule(userReference, schedule) =
        CreateOperations.schedule ctx log userReference schedule
    
    member _.UpdateSchedule(userReference, scheduleReference, update) =
        UpdateOperations.schedule ctx log userReference scheduleReference update
    
    member _.ActivateSchedule(userReference, scheduleReference) =
        UpdateOperations.activateSchedule ctx log userReference scheduleReference
        
    member _.DeactivateSchedule(userReference, scheduleReference) =
        UpdateOperations.deactivateSchedule ctx log userReference scheduleReference
        
    member _.GetSerialTipInternal() = Events.selectGlobalTip ctx
        
    member _.GetAllEventsInternal(previousSerial) = Events.selectAllEventRecordsFromPreviousTip ctx previousSerial