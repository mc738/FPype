namespace FPype.Infrastructure.Services

open Microsoft.Extensions.Logging
open Freql.MySql
open FPype.Infrastructure

type SchedulingService(ctx: MySqlContext, log: ILogger<SchedulingService>) =
    
    
    member _.AddSchedule(userReference, schedule) =
        Scheduling.CreateOperations.schedule ctx log userReference schedule
        
    
    member _.UpdateSchedule(userReference, scheduleReference, update) =
        Scheduling.UpdateOperations.schedule ctx log userReference scheduleReference update
        