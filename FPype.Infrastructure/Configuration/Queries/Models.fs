namespace FPype.Infrastructure.Configuration.Queries

open System

[<AutoOpen>]
module Models =

    type NewQuery =
        { Reference: string
          Name: string
          Version: NewQueryVersion }

    and NewQueryVersion = { Reference: string; RawQuery: string }

    type QueryDetails =
        { Reference: string
          Name: string
          Version: QueryVersionDetails }

    and QueryVersionDetails =
        { Reference: string
          Version: int
          RawQuery: string
          Hash: string
          CreatedOn: DateTime }
