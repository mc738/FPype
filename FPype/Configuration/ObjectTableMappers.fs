namespace FPype.Configuration


[<RequireQualifiedAccess>]
module ObjectTableMappers =

    open System.IO
    open Freql.Core.Common.Types
    open Microsoft.FSharp.Core
    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FsToolbox.Extensions
    open FPype.Core
    open FPype.Configuration.Persistence
    open FPype.Data.Models
    
    type NewObjectTableMapperVersion =
        { Id: IdType
          Name: string
          TableVersionId: string
          Version: ItemVersion
          Mapper: string }

    let getLatestVersion (ctx: SqliteContext) (mapperName: string) =
        Operations.selectTableObjectMapperVersionRecord
            ctx
            [ "WHERE object_table_mapper = @0 ORDER BY version DESC LIMIT 1;" ]
            [ mapperName ]

    let getVersion (ctx: SqliteContext) (mapperName: string) (version: int) =
        Operations.selectTableObjectMapperVersionRecord
            ctx
            [ "WHERE object_table_mapper = @0 AND version = @1;" ]
            [ mapperName; version ]

    let get (ctx: SqliteContext) (mapperName: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx mapperName
        | ItemVersion.Specific v -> getVersion ctx mapperName v

    (*
    let load (ctx: SqliteContext) (mapperName: string) (version: ItemVersion) =
        get ctx mapperName version
        |> Option.map (fun omv ->
            let json = omv.Mapper |> blobToString |> toJson

            tryCreateObjectMapper ctx json)
        |> Option.defaultValue (Error $"Could not find object mapper `{mapperName}`")
    *)
    
    let latestVersion (ctx: SqliteContext) (name: string) =
        ctx.Bespoke(
            "SELECT version FROM table_object_mapper_versions WHERE table_object_mapper = @0 ORDER BY version DESC LIMIT 1;",
            [ name ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead

    let getVersionId (ctx: SqliteContext) (name: string) (version: int) =
        ctx.Bespoke(
            "SELECT id FROM table_object_mapper_versions WHERE table_object_mapper = @0 AND version = @1 LIMIT 1;",
            [ name; version ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let addRawLatestVersion (ctx: SqliteContext) (id: IdType) (name: string) (tableVersionId: string) (mapper: string) =
        use ms = new MemoryStream(mapper.ToUtf8Bytes())

        let version =
            match latestVersion ctx name with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION if no version exists the query is new.
                ({ Name = name }: Parameters.NewTableObjectMapper)
                |> Operations.insertTableObjectMapper ctx

                1

        let hash = ms.GetSHA256Hash()

        ({ Id = id.Get()
           ObjectTableMapper = name
           Version = version
           TableModelVersionId = tableVersionId 
           Mapper = BlobField.FromBytes ms
           Hash = hash
           CreatedOn = timestamp () }: Parameters.NewObjectTableMapperVersion)
        |> Operations.insertObjectTableMapperVersion ctx

    let addRawLatestVersionTransaction (ctx: SqliteContext) (id: IdType) (name: string) (mapper: string) =
        ctx.ExecuteInTransaction(fun t -> addRawLatestVersion t id name mapper)

    let addRawSpecificVersion (ctx: SqliteContext) (id: IdType) (name: string) (tableVersionId: string) (mapper: string) (version: int) =
        match getVersionId ctx name version with
        | Some _ -> Error $"Version `{version}` of table object mapper `{name}` already exists."
        | None ->
            match Operations.selectTableObjectMapperRecord ctx [ "WHERE name = @0;" ] [ name ] with
            | Some _ -> ()
            | None ->
                ({ Name = name }: Parameters.NewTableObjectMapper)
                |> Operations.insertTableObjectMapper ctx

            use ms = new MemoryStream(mapper.ToUtf8Bytes())

            let hash = ms.GetSHA256Hash()

            ({ Id = id.Get()
               ObjectTableMapper = name
               Version = version
               TableModelVersionId =  tableVersionId
               Mapper = BlobField.FromBytes ms
               Hash = hash
               CreatedOn = timestamp () }: Parameters.NewObjectTableMapperVersion)
            |> Operations.insertObjectTableMapperVersion ctx
            |> Ok

    let addSpecificVersionRawTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (name: string)
        (tableVersionId: string)
        (mapper: string)
        (version: int)
        =
        ctx.ExecuteInTransactionV2(fun t -> addRawSpecificVersion t id name tableVersionId mapper version)

    let addRawVersion (ctx: SqliteContext) (mapper: NewObjectTableMapperVersion) =
        match mapper.Version with
        | ItemVersion.Latest -> addRawLatestVersion ctx mapper.Id mapper.Name mapper.TableVersionId mapper.Mapper |> Ok
        | ItemVersion.Specific v -> addRawSpecificVersion ctx mapper.Id mapper.Name mapper.TableVersionId mapper.Mapper v

    let addRawVersionTransaction (ctx: SqliteContext) (mapper: NewObjectTableMapperVersion) =
        ctx.ExecuteInTransactionV2(fun t -> addRawVersion t mapper)

    let add (ctx: SqliteContext) (mapperName: string) =
        ({ Name = mapperName }: Parameters.NewObjectTableMapper)
        |> Operations.insertObjectTableMapper ctx 
       
    let addTransaction (ctx: SqliteContext) (mapperName: string) =
        ctx.ExecuteInTransaction(fun t -> add t mapperName)
