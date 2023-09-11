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
        
    member _.GetAllEventsFromSerialInternal(previousSerial) = ReadOperations.allEventsInternal ctx log previousSerial
    
    member _.GetScheduleEventsFromSerialInternal(scheduleReference, previousSerial) =
        ReadOperations.scheduleEventsInternal ctx log scheduleReference previousSerial
    
    member _.GetAllEventsFromStartInternal() = ReadOperations.allEventsInternal ctx log -1
    
    member _.GetScheduleEventsFromStartInternal(scheduleReference) = ReadOperations.scheduleEventsInternal ctx log scheduleReference -1