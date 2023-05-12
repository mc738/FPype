namespace FPype.Visualizations.Charts

open System.Text.Json
open FSVG
open FSVG.Charts
open FsToolbox.Core
open Microsoft.FSharp.Core

[<AutoOpen>]
module Common =

    open System

    let ceiling (units: float) (value: float) = Math.Ceiling(value / units) * units

    let floor (units: float) (value: float) = Math.Floor(value / units) * units

    //let floatValueSplitter (percent: float) (maxValue: float) (minValue: float) =
    //    (minValue) + (((maxValue - minValue) / 100.) * percent) |> string

    type ValueRange =
        { Minimum: RangeValueType
          Maximum: RangeValueType }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetProperty "min" json
                |> Option.map RangeValueType.TryFromJson
                |> Option.defaultValue (Error "Missing min property"),
                Json.tryGetProperty "max" json
                |> Option.map RangeValueType.TryFromJson
                |> Option.defaultValue (Error "Missing max property")
            with
            | Ok min, Ok max -> { Minimum = min; Maximum = max } |> Ok
            | Error e, _ -> Error e
            | _, Error e -> Error e

    and [<RequireQualifiedAccess>] RangeValueType =
        | Specific of Value: float
        | UnitSize of Units: float

        static member TryFromJson(json: JsonElement) =
            Json.tryGetStringProperty "type" json
            |> Option.map (function
                | "specific" ->
                    Json.tryGetDoubleProperty "value" json
                    |> Option.map RangeValueType.Specific
                    |> Option.map Ok
                    |> Option.defaultValue (Error "Missing value property")
                | "unitSize" ->
                    Json.tryGetDoubleProperty "size" json
                    |> Option.map RangeValueType.UnitSize
                    |> Option.map Ok
                    |> Option.defaultValue (Error "Missing size property")
                | t -> Error $"Unknown RangeValueType: `{t}`")
            |> Option.defaultValue (Error "Missing type property")




    [<AutoOpen>]
    module Extensions =

        type SvgColor with

            static member TryFromJson(json: JsonElement) =
                match Json.tryGetStringProperty "type" json with
                | Some t ->
                    match t with
                    | "named" ->
                        match Json.tryGetStringProperty "name" json with
                        | Some n -> SvgColor.Named n |> Ok
                        | None -> Error "Missing name property"
                    | "hex" ->
                        match Json.tryGetStringProperty "value" json with
                        | Some v -> SvgColor.Hex v |> Ok
                        | None -> Error "Missing value property"
                    | "rgba" ->
                        SvgColor.Rgba(
                            Json.tryGetByteProperty "r" json |> Option.defaultValue 0uy,
                            Json.tryGetByteProperty "g" json |> Option.defaultValue 0uy,
                            Json.tryGetByteProperty "b" json |> Option.defaultValue 0uy,
                            Json.tryGetDoubleProperty "a" json |> Option.defaultValue 1.
                        )
                        |> Ok
                    | "hsl" ->
                        SvgColor.Hsl(
                            Json.tryGetByteProperty "h" json |> Option.defaultValue 0uy,
                            Json.tryGetByteProperty "s" json |> Option.defaultValue 0uy,
                            Json.tryGetByteProperty "l" json |> Option.defaultValue 0uy
                        )
                        |> Ok
                    | t -> Error $"Unknown SvgColor type: {t}"
                | None -> Error "Missing type property"

        type PaddingType with


            static member TryFromJson(json: JsonElement) =
                match Json.tryGetStringProperty "type" json, Json.tryGetDoubleProperty "value" json with
                | Some t, Some v ->
                    match t with
                    | "percent" -> PaddingType.Percent v |> Ok
                    | "specific" -> PaddingType.Specific v |> Ok
                    | t -> Error $"Unknown padding type: {t}"
                | None, _ -> Error "Missing type property"
                | _, None -> Error "Missing value property"

            static member FromJson(json: JsonElement, ?defaultValue: PaddingType) =
                match PaddingType.TryFromJson json with
                | Ok v -> v
                | Error _ -> defaultValue |> Option.defaultValue (PaddingType.Specific 1.)
