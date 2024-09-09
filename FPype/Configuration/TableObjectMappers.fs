namespace FPype.Configuration

[<RequireQualifiedAccess>]
module TableObjectMappers =

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

    [<AutoOpen>]
    module private Internal =

        let createQueryAndTable (ctx: SqliteContext) (queryVersion: QueryVersion) (tableVersion: TableVersion) =
            Queries.get ctx queryVersion.Name queryVersion.Version
            |> Option.map (fun q ->
                Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                |> Result.map (fun t -> q, t))
            |> Option.defaultWith (fun _ ->
                Error
                    $"Error creating query: query `{queryVersion.Name}` (version `{queryVersion.Version.ToLabel()}`) not found")

    type NewTableObjectMapperVersion =
        { Id: IdType
          Name: string
          Version: ItemVersion
          Mapper: string }

    let createTableColumnSource (json: JsonElement) =
        match Json.tryGetIntProperty "index" json with
        | Some i -> PropertySource.TableColumn i |> Ok
        | None -> Error "Missing `index` property"

    let rec createTableSource (ctx: SqliteContext) (json: JsonElement) (properties: PropertyMap list) =
        match
            Json.tryGetStringProperty "name" json,
            TableVersion.TryCreate json,
            QueryVersion.TryCreate json,
            Json.tryGetIntArrayProperty "parameterIndexes" json
        with
        | Some name, Ok table, Ok query, Some parameterIndexes ->
            createQueryAndTable ctx query table
            |> Result.map (fun (q, t) ->
                ({ Name = name
                   Query = q
                   Table = t
                   ParameterIndexes = parameterIndexes
                   Properties = properties }
                : RelatedObjectTableSource)
                |> PropertySource.Table)
        | None, _, _, _ -> Error "Missing `name` property"
        | _, Error e, _, _ -> Error $"Error creating table: {e}"
        | _, _, Error e, _ -> Error $"Error creating query: {e}"
        | _, _, _, None -> Error "Missing `parameterIndexes` property"

    let rec tryCreatePropertyMap (ctx: SqliteContext) (json: JsonElement) =
        // TODO if the type is source `table` then rec.
        match Json.tryGetStringProperty "name" json, Json.tryGetProperty "source" json with
        | Some name, Some source ->
            let source =
                match Json.tryGetStringProperty "type" source with
                | Some t when t = "table_column" -> createTableColumnSource source

                | Some t when t = "table" ->
                    Json.tryGetArrayProperty "properties" source
                    |> Option.map (fun ps -> ps |> List.map (tryCreatePropertyMap ctx) |> flattenResultList)
                    |> Option.defaultValue (Error "Missing `properties` element")
                    |> Result.bind (createTableSource ctx source)
                | Some t -> Error $"Unknown source type `{t}`"
                | None -> Error "Missing `type` property"

            source |> Result.map (fun s -> ({ Name = name; Source = s }: PropertyMap))
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `type` property"

    let tryCreateObjectMapper (ctx: SqliteContext) (json: JsonElement) =
        match
            Json.tryGetStringProperty "objectName" json,
            QueryVersion.TryCreate json,
            TableVersion.TryCreate json,
            Json.tryGetArrayProperty "properties" json
        with
        | Some objectName, Ok query, Ok table, Some properties ->

            match properties |> List.map (tryCreatePropertyMap ctx) |> flattenResultList with
            | Ok p ->
                createQueryAndTable ctx query table
                |> Result.map (fun (q, t) ->
                    ({ ObjectName = objectName
                       Table = t
                       Query = q
                       Properties = p }
                    : TableObjectMap))
            | Error e -> Error $"Failed to create properties. Error: {e}"
        | None, _, _, _ -> Error "Missing `objectName` property"
        | _, Error e, _, _ -> Error $"Error creating query: {e}"
        | _, _, Error e, _ -> Error $"Error creating table: {e}"
        | _, _, _, None -> Error "Missing `properties` array"

    let getLatestVersion (ctx: SqliteContext) (mapperName: string) =
        Operations.selectTableObjectMapperVersionRecord
            ctx
            [ "WHERE table_object_mapper = @0 ORDER BY version DESC LIMIT 1;" ]
            [ mapperName ]

    let getVersion (ctx: SqliteContext) (mapperName: string) (version: int) =
        Operations.selectTableObjectMapperVersionRecord
            ctx
            [ "WHERE table_object_mapper = @0 AND version = @1;" ]
            [ mapperName; version ]

    let get (ctx: SqliteContext) (mapperName: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx mapperName
        | ItemVersion.Specific v -> getVersion ctx mapperName v

    let load (ctx: SqliteContext) (mapperName: string) (version: ItemVersion) =
        get ctx mapperName version
        |> Option.map (fun omv ->
            let json = omv.Mapper |> blobToString |> toJson

            tryCreateObjectMapper ctx json)
        |> Option.defaultValue (Error $"Could not find object mapper `{mapperName}`")

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

    let addRawLatestVersion (ctx: SqliteContext) (id: IdType) (name: string) (mapper: string) =
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
           TableObjectMapper = name
           Version = version
           Mapper = BlobField.FromBytes ms
           Hash = hash
           CreatedOn = timestamp () }
        : Parameters.NewTableObjectMapperVersion)
        |> Operations.insertTableObjectMapperVersion ctx

    let addRawLatestVersionTransaction (ctx: SqliteContext) (id: IdType) (name: string) (mapper: string) =
        ctx.ExecuteInTransaction(fun t -> addRawLatestVersion t id name mapper)

    let addRawSpecificVersion (ctx: SqliteContext) (id: IdType) (name: string) (mapper: string) (version: int) =
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
               TableObjectMapper = name
               Version = version
               Mapper = BlobField.FromBytes ms
               Hash = hash
               CreatedOn = timestamp () }
            : Parameters.NewTableObjectMapperVersion)
            |> Operations.insertTableObjectMapperVersion ctx
            |> Ok

    let addSpecificVersionRawTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (name: string)
        (mapper: string)
        (version: int)
        =
        ctx.ExecuteInTransactionV2(fun t -> addRawSpecificVersion t id name mapper version)

    let addRawVersion (ctx: SqliteContext) (mapper: NewTableObjectMapperVersion) =
        match mapper.Version with
        | ItemVersion.Latest -> addRawLatestVersion ctx mapper.Id mapper.Name mapper.Mapper |> Ok
        | ItemVersion.Specific v -> addRawSpecificVersion ctx mapper.Id mapper.Name mapper.Mapper v

    let addRawVersionTransaction (ctx: SqliteContext) (mapper: NewTableObjectMapperVersion) =
        ctx.ExecuteInTransactionV2(fun t -> addRawVersion t mapper)

    let add (ctx: SqliteContext) (mapperName: string) =
        ({ Name = mapperName }: Parameters.NewTableObjectMapper)
        |> Operations.insertTableObjectMapper ctx

    let addTransaction (ctx: SqliteContext) (mapperName: string) =
        ctx.ExecuteInTransaction(fun t -> add t mapperName)
