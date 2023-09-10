namespace FPype.Infrastructure.Services

open Microsoft.Extensions.Logging
open Freql.MySql
open FPype.Infrastructure

type SchedulingService(ctx: MySqlContext, log: ILogger<SchedulingService>) =
    
    
    member _.AddSchedule(userReference: string, schedule: FPype.Infrastructure.Scheduling.Models.NewSchedule) =
        Scheduling.CreateOperations.schedule ctx log userReference schedule
        
    