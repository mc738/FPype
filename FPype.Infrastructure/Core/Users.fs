namespace FPype.Infrastructure.Core

open Freql.MySql
open FsToolbox.Core.Results

[<RequireQualifiedAccess>]
module Users =

    open System
    open FPype.Infrastructure.Core.Persistence

    let defaultSystemReference = "system"

    let isSystemUser (user: Records.User) =
        String.Equals(user.Reference, defaultSystemReference, StringComparison.OrdinalIgnoreCase)

    let getDefault (ctx: MySqlContext) =
        Operations.selectUserRecord ctx [ "WHERE reference = @0" ] [ defaultSystemReference ]

    let addOrGetSystemUser (ctx: MySqlContext) =
        match getDefault ctx with
        | Some ur -> ur.Id
        | None ->
            ({ Reference = defaultSystemReference
               SubscriptionId = Subscriptions.addOrGetSystemSubscription ctx
               Username = "system"
               Active = true }
            : Parameters.NewUser)
            |> Operations.insertUser ctx
            |> int
