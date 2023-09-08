namespace FPype.Infrastructure.Scheduling

[<RequireQualifiedAccess>]
module UpdateOperations =
    
    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    
    let schedule (ctx: MySqlContext) (logger: ILogger) (userReference: string) (schedule: UpdateSchedule) =
        
        ()
    

