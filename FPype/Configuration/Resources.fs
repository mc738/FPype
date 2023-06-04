namespace FPype.Configuration

open System.IO
open Freql.Core.Common.Types

[<RequireQualifiedAccess>]
module Resources =

    open System.IO
    open Freql.Sqlite
    open FsToolbox.Extensions
    open FPype.Configuration.Persistence

    let getPipelineResources (ctx: SqliteContext) (pipelineVersionId: string) =
        Operations.selectPipelineResourceRecords ctx [ "WHERE pipeline_version_id = @0;" ] [ pipelineVersionId ]

    let getResourceVersionById (ctx: SqliteContext) (id: string) =
        Operations.selectResourceVersionRecord ctx [ "WHERE id = @0;" ] [ id ]

    let getLatestVersion (ctx: SqliteContext) (resource: string) =
        Operations.selectResourceVersionRecord ctx [ "WHERE resource = @0 ORDER BY version DESC LIMIT 1;" ] [ resource ]

    let getVersion (ctx: SqliteContext) (resource: string) (version: int) =
        Operations.selectResourceVersionRecord ctx [ "WHERE resource = @0 AND version = @1;" ] [ resource; version ]

    let get (ctx: SqliteContext) (resource: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx resource
        | ItemVersion.Specific v -> getVersion ctx resource v

    let latestVersion (ctx: SqliteContext) (resource: string) =
        ctx.Bespoke(
            "SELECT version FROM resource_versions WHERE resource = @0 ORDER BY version DESC LIMIT 1;",
            [ resource ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead

    let getVersionId (ctx: SqliteContext) (resource: string) (version: int) =
        ctx.Bespoke(
            "SELECT id FROM resource_versions WHERE resource = @0 AND version = @1 LIMIT 1;",
            [ resource; version ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let addLatestVersion
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        =
        let version =
            match latestVersion ctx resource with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION if no version exists the resource is new.
                ({ Name = resource }: Parameters.NewResource) |> Operations.insertResource ctx
                1

        let hash = raw.GetSHA256Hash()

        ({ Id = id.Get()
           Resource = resource
           Version = version
           ResourceType = resourceType
           RawBlob = BlobField.FromBytes raw
           Hash = hash
           CreatedOn = timestamp () }: Parameters.NewResourceVersion)
        |> Operations.insertResourceVersion ctx

    let addLatestVersionTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        =
        ctx.ExecuteInTransaction(fun t -> addLatestVersion t id resource resourceType raw)

    let addSpecificVersion
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        (version: int)
        =
        match getVersionId ctx resource version with
        | Some _ -> Error $"Version `{version}` of resource `{resource}` already exists."
        | None ->
            match Operations.selectResourceRecord ctx [ "WHERE name = @0;" ] [ resource ] with
            | Some _ -> ()
            | None -> ({ Name = resource }: Parameters.NewResource) |> Operations.insertResource ctx

            let hash = raw.GetSHA256Hash()

            ({ Id = id.Get()
               Resource = resource
               Version = version
               ResourceType = resourceType
               RawBlob = BlobField.FromBytes raw
               Hash = hash
               CreatedOn = timestamp () }: Parameters.NewResourceVersion)
            |> Operations.insertResourceVersion ctx
            |> Ok

    let addSpecificVersionTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        (version: int)
        =
        ctx.ExecuteInTransactionV2(fun t -> addSpecificVersion t id resource resourceType raw version)

    let addVersion
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        (version: ItemVersion)
        =
        match version with
        | ItemVersion.Latest -> addLatestVersion ctx id resource resourceType raw |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx id resource resourceType raw v

    let addVersionTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (resource: string)
        (resourceType: string)
        (raw: MemoryStream)
        (version: ItemVersion)
        =
        ctx.ExecuteInTransactionV2(fun t -> addVersion t id resource resourceType raw version)


    let add (ctx: SqliteContext) (resourceName: string) =
        ({ Name = resourceName }: Parameters.NewResource)
        |> Operations.insertResource ctx
        
    let addTransaction (ctx: SqliteContext) (resourceName: string) =
        ctx.ExecuteInTransaction(fun t -> add t resourceName)