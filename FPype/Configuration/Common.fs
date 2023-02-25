namespace FPype.Configuration

open System
open System.Globalization
open System.Text
open System.Text.Json
open FPype.Core.Types
open FPype.Data.Store
open Freql.Core.Common.Types

[<AutoOpen>]
module Common =

    module ImportHandlers =

        let parseDate (format: string) (v: string) =
            match DateTime.TryParseExact(v, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal) with
            | true, dt -> CoercionResult.Success <| Value.DateTime dt
            | false, _ -> CoercionResult.Failure "Wrong date format"

    type PipelineAction =
        { Name: string
          Action: PipelineStore -> Result<PipelineStore, string> }

        static member Create(name, action) = { Name = name; Action = action }
        
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

    
    

    