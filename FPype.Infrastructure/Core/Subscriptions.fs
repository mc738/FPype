namespace FPype.Infrastructure.Core

open System
open FPype.Infrastructure.Core.Persistence
open Freql.MySql

[<RequireQualifiedAccess>]
module Subscriptions =

    let defaultSystemReference = "system"

    let isSystemSubscription (user: Records.Subscription) =
        String.Equals(user.Reference, defaultSystemReference, StringComparison.OrdinalIgnoreCase)

    let getDefault (ctx: MySqlContext) =
        Operations.selectSubscriptionRecord ctx [ "WHERE reference = @0" ] [ defaultSystemReference ]

    let addOrGetSystemSubscription (ctx: MySqlContext) =
        match getDefault ctx with
        | Some sr -> sr.Id
        | None ->
            ({ Reference = defaultSystemReference
               Active = true }
            : Parameters.NewSubscription)
            |> Operations.insertSubscription ctx
            |> int
