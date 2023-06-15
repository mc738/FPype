namespace FPype.Infrastructure.Configuration.Queries

open System
open FPype.Infrastructure.Core.Persistence

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

        static member FromEntity(entity: Records.QueryVersion) =
            { Reference = entity.Reference
              Version = entity.Version
              RawQuery = entity.RawQuery
              Hash = entity.Hash
              CreatedOn = entity.CreatedOn }

    type QueryVersionOverview =
        { QueryReference: string
          Reference: string
          Name: string
          Version: int
          CreatedOn: DateTime }

    type QueryOverview =

        { Reference: string
          Name: string }

        static member FromEntity(entity: Records.Query) =
            { Reference = entity.Reference
              Name = entity.Name }
