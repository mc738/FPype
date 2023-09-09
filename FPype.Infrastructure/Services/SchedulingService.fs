namespace FPype.Infrastructure.Services

open Microsoft.Extensions.Logging
open Freql.MySql

type SchedulingService(ctx: MySqlContext, log: ILogger<SchedulingService>) =
    
    
    member _.Stub() = ()

