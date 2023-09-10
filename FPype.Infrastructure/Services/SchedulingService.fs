﻿namespace FPype.Infrastructure.Services

open Microsoft.Extensions.Logging
open Freql.MySql
open FPype.Infrastructure

type SchedulingService(ctx: MySqlContext, log: ILogger<SchedulingService>) =
    
    member _.AddSchedule(userReference, schedule) =
        Scheduling.CreateOperations.schedule ctx log userReference schedule
    
    member _.UpdateSchedule(userReference, scheduleReference, update) =
        Scheduling.UpdateOperations.schedule ctx log userReference scheduleReference update
    
    member _.ActivateSchedule(userReference, scheduleReference) =
        Scheduling.UpdateOperations.activateSchedule userReference scheduleReference
        
    member _.DeactivateSchedule(userReference, scheduleReference) =
        Scheduling.UpdateOperations.deactivateSchedule userReference scheduleReference