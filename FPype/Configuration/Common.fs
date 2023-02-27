namespace FPype.Configuration

open System
open System.Globalization
open System.Text
open System.Text.Json
open FPype.Core.Types
open FPype.Data.Store
open Freql.Core.Common.Types
open FsToolbox.Core

[<AutoOpen>]
module Common =

    module ImportHandlers =

        let parseDate (format: string) (v: string) =
            match DateTime.TryParseExact(v, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal) with
            | true, dt -> CoercionResult.Success <| Value.DateTime dt
            | false, _ -> CoercionResult.Failure "Wrong date format"

    type ItemVersion =
        | Latest
        | Specific of int

        static member TryFromJson(json: JsonElement) =
            Json.tryGetIntProperty "version" json |> Option.map ItemVersion.Specific

        static member FromOptional(version: ItemVersion option) =
            version |> Option.defaultValue ItemVersion.Latest

        static member FromJson(json: JsonElement) =
            ItemVersion.TryFromJson json |> ItemVersion.FromOptional

        member iv.ToLabel() =
            match iv with
            | Latest -> "latest"
            | Specific v -> v.ToString()

    type TableVersion =
        { Name: string
          Version: ItemVersion }

        static member FromJson(json: JsonElement) =
            Json.tryGetStringProperty "name" json
            |> Option.map (fun n ->
                { Name = n
                  Version = ItemVersion.FromJson json })
        //|> Option.defaultWith (fun _ -> Error "Missing table version `name` property.")

        static member TryFromJson(json: JsonElement) =
            TableVersion.FromJson json
            |> Option.map Ok
            |> Option.defaultWith (fun _ -> Error "Missing table version `name` property.")

        static member TryCreate(json: JsonElement option) =
            json
            |> Option.map TableVersion.TryFromJson
            |> Option.defaultValue (Error "Missing `table` object")

    let createId _ = Guid.NewGuid().ToString("n")

    let timestamp _ = DateTime.UtcNow

    let toJson (str: string) = JsonDocument.Parse(str).RootElement

    let blobToString (blob: BlobField) =
        blob.ToBytes() |> Encoding.UTF8.GetString

    let flattenResultList (r: Result<'a, string> list) =
        r
        |> List.fold
            (fun (s, err) r ->
                match r with
                | Ok v -> s @ [ v ], err
                | Error e -> s, err @ [ e ])
            ([], [])
        |> fun (values, errors) ->
            match errors.IsEmpty with
            | true -> Ok values
            | false -> Error <| String.concat ", " errors
