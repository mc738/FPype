namespace FPype.Core

open System

module Logging =

    open FsToolbox.Core.ConsoleIO

    type Log =
        { LogToConsole: bool
          
          Items: LogItem list }
        
        member l.Log(from: string, message: string, itemType: LogItemType) =
            let item = ({ From = from; Message = message; Timestamp = DateTime.UtcNow; ItemType = itemType })
            
            ()

    and LogItem =
        { From: string
          Message: string
          Timestamp: DateTime
          ItemType: LogItemType }

    
    and [<RequireQualifiedAccess>] LogItemType =
        | Information
        | Warning
        | Error 
       

    let logToConsole (color: ConsoleColor) (logType: string) (from: string) (message: string) =
        cprintfn color $"[{DateTime.UtcNow} {logType}] {from} - {message}"

    let logInfo from message =
        logToConsole ConsoleColor.Gray "INFO " from message

    let logSuccess from message =
        logToConsole ConsoleColor.Green "Ok   " from message

    let logError from message =
        logToConsole ConsoleColor.Red "ERR  " from message

    let logWarning from message =
        logToConsole ConsoleColor.DarkYellow "WARN " from message
