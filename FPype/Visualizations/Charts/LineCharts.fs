namespace FPype.Visualizations.Charts

module LineCharts =

        
    open System
    open System.Text.Json
    open FsToolbox.Core
    open FSVG
    open FSVG.Charts
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models

        
    type TimeSeriesEntry = { Value: float; Timestamp: DateTime }

    type TimeSeriesChartGeneratorSettings =
        { TimestampValueIndex: int
          TimestampFormat: string
          Range: ValueRange
          ChartSettings: TimeSeriesChartSettings
          SeriesSettings: SeriesSettings list }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetIntProperty "timestampValueIndex" json,
                Json.tryGetProperty "range" json
                |> Option.map ValueRange.TryFromJson
                |> Option.defaultValue (Error "Missing range property"),
                Json.tryGetArrayProperty "series" json
                |> Option.map (List.map SeriesSettings.TryFromJson >> flattenResultList)
                |> Option.defaultValue (Error "Missing series property")
            with
            | Some tsv, Ok r, Ok ss ->
                { TimestampValueIndex = tsv
                  TimestampFormat = Json.tryGetStringProperty "timestampFormat" json |> Option.defaultValue "u"
                  Range = r
                  ChartSettings =
                    Json.tryGetProperty "chartSettings" json
                    |> Option.map TimeSeriesChartSettings.FromJson
                    |> Option.defaultValue (TimeSeriesChartSettings.Default())
                  SeriesSettings = ss }
                |> Ok
            | None, _, _ -> Error "Missing timestampValueIndex"
            | _, Error e, _ -> Error e
            | _, _, Error e -> Error e

    and TimeSeriesChartSettings =
        { Height: float option
          Width: float option
          BottomOffset: float option
          TopOffset: float option
          LeftOffset: float option
          RightOffset: float option
          LegendPosition: FSVG.Charts.Common.LegendPosition option
          Title: string option
          XLabel: string option
          YLabel: string option
          YMajorMarkers: float list
          YMinorMarkers: float list }

        static member Default() =
            { Height = None
              Width = None
              BottomOffset = None
              TopOffset = None
              LeftOffset = None
              RightOffset = None
              LegendPosition = None
              Title = None
              XLabel = None
              YLabel = None
              YMajorMarkers = [ 50.; 100. ]
              YMinorMarkers = [ 25.; 75. ] }

        static member FromJson(json: JsonElement) =
            { Height = Json.tryGetDoubleProperty "height" json
              Width = Json.tryGetDoubleProperty "width" json
              TopOffset = Json.tryGetDoubleProperty "topOffset" json
              BottomOffset = Json.tryGetDoubleProperty "bottomOffset" json
              LeftOffset = Json.tryGetDoubleProperty "leftOffset" json
              RightOffset = Json.tryGetDoubleProperty "rightOffset" json
              LegendPosition =
                Json.tryGetStringProperty "legendPosition" json
                |> Option.bind (function
                    | "bottom" -> Some LegendPosition.Bottom
                    | "right" -> Some LegendPosition.Right
                    | _ -> None)
              Title = Json.tryGetStringProperty "title" json
              XLabel = Json.tryGetStringProperty "xLabel" json
              YLabel = Json.tryGetStringProperty "yLabel" json
              YMajorMarkers =
                Json.tryGetArrayProperty "yMajorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue []
              YMinorMarkers =
                Json.tryGetArrayProperty "yMinorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue [] }

        member tsc.ToLineChartSettings() =
            ({ ChartDimensions =
                { Height = tsc.Height |> Option.defaultValue 100.
                  Width = tsc.Width |> Option.defaultValue 100.
                  LeftOffset = tsc.LeftOffset |> Option.defaultValue 10
                  RightOffset = tsc.RightOffset |> Option.defaultValue 10
                  TopOffset = tsc.TopOffset |> Option.defaultValue 10
                  BottomOffset = tsc.BottomOffset |> Option.defaultValue 10 }
               LegendStyle = tsc.LegendPosition |> Option.map (fun lp -> { Bordered = false; Position = lp })
               Title = tsc.Title
               XLabel = tsc.XLabel
               YLabel = tsc.YLabel
               YMajorMarkers = tsc.YMajorMarkers
               YMinorMarkers = tsc.YMinorMarkers }
            : LineCharts.Settings)

    and SeriesSettings =
        { Name: string
          ValueIndex: int
          StrokeWidth: float
          Color: SvgColor
          LineType: LineCharts.LineType
          Shading: LineCharts.ShadingOptions option }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetStringProperty "name" json,
                Json.tryGetIntProperty "valueIndex" json,
                Json.tryGetProperty "color" json
                |> Option.map SvgColor.TryFromJson
                |> Option.defaultValue (Error "Missing color property")
            with
            | Some n, Some vi, Ok color ->
                { Name = n
                  ValueIndex = vi
                  StrokeWidth = 0.3
                  Color = color
                  LineType =
                    Json.tryGetStringProperty "lineType" json
                    |> Option.bind (function
                        | "bezier" -> Some LineCharts.LineType.Bezier
                        | "straight" -> Some LineCharts.LineType.Straight
                        | _ -> None)
                    |> Option.defaultValue LineCharts.LineType.Straight
                  Shading =
                    Json.tryGetProperty "shading" json
                    |> Option.bind (fun sp ->
                        match SvgColor.TryFromJson sp with
                        | Ok c -> Some { Color = c }
                        | Error _ -> None) }
                |> Ok
            | None, _, _ -> Error "Missing name property"
            | _, None, _ -> Error "Missing valueIndex property"
            | _, _, Error e -> Error e

    let createTimeSeriesFromTable (settings: TimeSeriesChartGeneratorSettings) (table: TableModel) =
        // This will have it's values populated later.
        let seriesValueMap =
            settings.SeriesSettings
            |> List.mapi (fun i s ->
                i,
                ({ Name = s.Name
                   Style =
                     { Color = s.Color
                       StrokeWidth = s.StrokeWidth
                       LineType = s.LineType
                       Shading = s.Shading }
                   Values = [] }
                : LineCharts.Series<float>))
            |> Map.ofList


        // Get the timestamp and get the values
        //let tss, vs =
        table.Rows
        |> List.map (fun tr ->
            tr.TryGetValue settings.TimestampValueIndex
            |> Option.map (fun ts ->
                match ts with
                | Value.DateTime dt -> dt.ToString(settings.TimestampFormat) |> Ok
                | _ -> Error $"Value at index {settings.TimestampValueIndex} is not a datetime")
            |> Option.defaultWith (fun _ -> Error $"Timestamp at index {settings.TimestampValueIndex} not found")
            |> Result.bind (fun ts ->
                // For each series -
                // Create a list of series index and float values.
                settings.SeriesSettings
                |> List.mapi (fun i ss ->
                    tr.TryGetValue ss.ValueIndex
                    |> Option.map (fun v -> Ok(i, v.GetFloat()))
                    |> Option.defaultWith (fun _ -> Error $"Value at index {i} not found"))
                |> flattenResultList
                |> Result.map (fun vs -> ts, vs)))
        |> flattenResultList
        |> Result.map List.unzip
        |> Result.bind (fun (tss, vss) ->
            // Double fold -
            // The out side one iterates over all rows.
            // The inner one populates the series from each row.
            vss
            |> List.fold
                (fun acc vs ->
                    vs
                    |> List.fold
                        (fun (r: Result<Map<int, LineCharts.Series<float>>, string>) v ->
                            let (i, v) = v

                            r
                            |> Result.bind (fun acc2 ->
                                match acc2.TryFind i with
                                | Some s -> Ok <| acc2.Add(i, { s with Values = s.Values @ [ v ] })
                                | None -> Error ""))
                        acc)
                (Ok seriesValueMap)
            |> Result.map (fun vs ->
                ({ SplitValueHandler = floatValueSplitter
                   Normalizer = floatRangeNormalizer
                   PointNames = tss
                   Series = vs |> Map.toList |> List.map snd }
                : LineCharts.SeriesCollection<float>)))

    (*
        
        table.Rows
        |> List.map (fun tr ->
            match tr.TryGetValue valueIndex, tr.TryGetValue timestampIndex with
            | Some v, Some ts ->
                match ts with
                | Value.DateTime dt -> Ok { Value = v.GetFloat(); Timestamp = dt }
                | _ -> Error $"Value at index {timestampIndex} is not a datetime"
            | None, _ -> Error $"Value at index {timestampIndex} not found"
            | _, None -> Error $"Timestamp at index {timestampIndex} not found")
        |> flattenResultList
        |> Result.map (fun ts ->
            ({ Normalizer = fun p -> (p.Value.Value / p.MaxValue.Value) * 100.
               SplitValueHandler =
                 fun percent maxValue ->
                     (float maxValue.Value / float 100) * float percent
                     |> int
                     |> fun r -> r.ToString()
               Points =
                 ts
                 |> List.map (fun tse -> { Name = tse.Timestamp.ToString("dd-MM-yy"); Value = tse }: LineCharts.LineChartPoint<TimeSeriesEntry>) }
            : LineCharts.Series<TimeSeriesEntry>))
       *)

    let generate (settings: TimeSeriesChartGeneratorSettings) (table: TableModel) =
        createTimeSeriesFromTable settings table
        |> Result.map (fun sc ->
            let maxValue =
                match settings.Range.Maximum with
                | RangeValueType.Specific v -> v
                | RangeValueType.UnitSize us ->
                    sc.Series
                    |> List.fold
                        (fun v s ->
                            let sv = s.Values |> List.max

                            match sv > v with
                            | true -> sv
                            | false -> v)
                        0.
                    |> ceiling us

            let minValue =
                match settings.Range.Minimum with
                | RangeValueType.Specific v -> v
                | RangeValueType.UnitSize us ->
                    sc.Series
                    |> List.fold
                        (fun v s ->
                            let sv = s.Values |> List.min

                            match sv < v with
                            | true -> sv
                            | false -> v)
                        0.
                    |> floor us

            LineCharts.generate (settings.ChartSettings.ToLineChartSettings()) sc minValue maxValue)
