namespace FPype.Infrastructure.Scheduling

[<RequireQualifiedAccess>]
module Events =
    
    
    type ScheduleEvent =
        | ScheduleCreated
        | ScheduleUpdated
        | ScheduleActivated
        | ScheduleDeactivated
        
    
    
    
    ()