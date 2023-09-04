namespace FPype.Infrastructure.Scheduling

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    module Fetch =

        open FsToolbox.Core.Results        
        open Freql.MySql
        open FPype.Infrastructure.Core
        open FPype.Infrastructure.Core.Persistence

        let scheduleById (ctx: MySqlContext) (id: int) =
            try
                Operations.selectPipelineScheduleEventRecord ctx [ "WHERE id = @0" ] [ id ]
                |> Option.map FetchResult.Success
                |> Option.defaultWith (fun _ ->
                    ({ Message = $"Schedule (id: {id}) not found"
                       DisplayMessage = "Schedule not found"
                       Exception = None }
                    : FailureResult)
                    |> FetchResult.Failure)
            with ex ->
                { Message = "Unhandled exception while fetching schedule"
                  DisplayMessage = "Error fetching schedule"
                  Exception = Some ex }
                |> FetchResult.Failure

