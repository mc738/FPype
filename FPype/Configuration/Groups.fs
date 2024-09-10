namespace FPype.Configuration

module Groups =

    open System.Text.Json
    open FPype.Data.Grouping
    open FsToolbox.Core

    /// Example json:
    /// {
    ///    "type": "month",
    ///    "start": "2014-01-01",
    ///    "length": 48,
    ///    "fieldName": "date"
    ///    "label": "Date"
    /// }
    let createMonthGroups (elements: Map<string, JsonElement>) =
        match
            elements.TryFind "start" |> Option.bind Json.tryGetDateTime,
            elements.TryFind "length" |> Option.bind Json.tryGetInt,
            elements.TryFind "fieldName" |> Option.map Json.getString,
            elements.TryFind "label" |> Option.map Json.getString
        with
        | Some start, Some length, Some fieldName, Some label ->
            DateGroup.GenerateMonthGroups(start, length)
            |> fun dg ->
                ({ FieldName = fieldName
                   Label = label
                   Groups = dg }
                : DateGroups)
            |> Ok
        | None, _, _, _ -> Error "Missing start property"
        | _, None, _, _ -> Error "Missing length property"
        | _, _, None, _ -> Error "Missing fieldName property"
        | _, _, _, None -> Error "Missing label property"

    let createDateGroup (properties: JsonProperty list option) =
        properties
        |> Option.map Json.propertiesToMap
        |> Option.map (fun elements ->
            match
                elements.TryFind "type"
                |> Option.map (fun el -> el.GetString())
                |> Option.defaultValue ""
            with
            | "months" -> createMonthGroups elements
            | t -> Error $"Unknown group type: `{t}`")
        |> Option.defaultValue (Error "Missing dateGroup object")
