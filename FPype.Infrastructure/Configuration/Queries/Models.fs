namespace FPype.Infrastructure.Configuration.Queries

open System
open FPype.Data
open FPype.Infrastructure.Core.Persistence

[<AutoOpen>]
module Models =

    type NewRawQuery =
        { Reference: string
          Name: string
          Version: NewRawQueryVersion }

    and NewRawQueryVersion = { Reference: string; RawQuery: string }

    type NewSerializedQuery =
        { Reference: string
          Name: string
          Version: NewSerializedQueryVersion }
    
    and NewSerializedQueryVersion = { Reference: string; SerializedQuery: SerializableQueries.Query }
    
    type QueryDetails =
        { Reference: string
          Name: string
          Versions: QueryVersionDetails list }
        
    and QueryVersionDetails =
        { QueryReference: string
          Reference: string
          Name: string
          Version: int
          RawQuery: string
          Hash: string
          IsSerialized: bool
          CreatedOn: DateTime }

        static member FromEntity(queryEntity: Records.Query, entity: Records.QueryVersion) =
            { QueryReference = queryEntity.Reference
              Reference = entity.Reference
              Name = queryEntity.Name
              Version = entity.Version
              RawQuery = entity.RawQuery
              Hash = entity.Hash
              IsSerialized = entity.IsSerialized 
              CreatedOn = entity.CreatedOn }

    type QueryVersionOverview =
        { QueryReference: string
          Reference: string
          Name: string
          Version: int
          CreatedOn: DateTime }

        static member FromEntity(queryEntity: Records.Query, entity: Records.QueryVersion) =
            { QueryReference = queryEntity.Reference
              Reference = entity.Reference
              Name = queryEntity.Name
              Version = entity.Version
              CreatedOn = entity.CreatedOn }

    type QueryOverview =

        { Reference: string
          Name: string }

        static member FromEntity(entity: Records.Query) =
            { Reference = entity.Reference
              Name = entity.Name }
