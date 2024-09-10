namespace FPype.Visualizations.Charts

open FSVG

module CandleStickCharts =

    open System.Text.Json
    open FsToolbox.Core
    open FSVG.Charts
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models

    type CandleStickChartGeneratorSettings =
        { TimestampValueIndex: int
          TimestampFormat: string
          Range: ValueRange
          ChartSettings: CandleStickChartSettings
          SeriesSettings: CandleStickSeriesSettings }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetIntProperty "timestampValueIndex" json,
                Json.tryGetProperty "range" json
                |> Option.map ValueRange.TryFromJson
                |> Option.defaultValue (Error "Missing range property"),
                Json.tryGetProperty "series" json
                |> Option.map CandleStickSeriesSettings.TryFromJson
                |> Option.defaultValue (Error "Missing series property")
            with
            | Some tsv, Ok r, Ok ss ->
                { TimestampValueIndex = tsv
                  TimestampFormat = Json.tryGetStringProperty "timestampFormat" json |> Option.defaultValue "u"
                  Range = r
                  ChartSettings =
                    Json.tryGetProperty "chartSettings" json
                    |> Option.map CandleStickChartSettings.FromJson
                    |> Option.defaultValue (CandleStickChartSettings.Default())
                  SeriesSettings = ss }
                |> Ok
            | None, _, _ -> Error "Missing timestampValueIndex"
            | _, Error e, _ -> Error e
            | _, _, Error e -> Error e

    and CandleStickChartSettings =
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
          YMinorMarkers: float list
          SectionPadding: PaddingType }

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
              YMinorMarkers = [ 25.; 75. ]
              SectionPadding = PaddingType.Specific 1. }

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
                |> Option.defaultValue []
              SectionPadding =
                Json.tryGetProperty "sectionPadding" json
                |> Option.map PaddingType.FromJson
                |> Option.defaultValue (PaddingType.Specific 1.) }

        member tsc.ToSettings() =
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
               SectionPadding = tsc.SectionPadding
               MajorMarkers = tsc.YMajorMarkers
               MinorMarkers = tsc.YMinorMarkers }
            : CandleStickCharts.Settings)


    and CandleStickSeriesSettings =
        { PositiveColor: SvgColor
          NegativeColor: SvgColor
          StrokeWidth: float
          OpenValueIndex: int
          CloseValueIndex: int
          HighValueIndex: int
          LowValueIndex: int }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetIntProperty "openValueIndex" json,
                Json.tryGetIntProperty "closeValueIndex" json,
                Json.tryGetIntProperty "highValueIndex" json,
                Json.tryGetIntProperty "lowValueIndex" json
            with
            | Some ovi, Some cvi, Some hvi, Some lvi ->
                { PositiveColor =
                    Json.tryGetProperty "positiveColor" json
                    |> Option.map (fun el -> SvgColor.FromJson(el, SvgColor.Named "green"))
                    |> Option.defaultValue (SvgColor.Named "green")
                  NegativeColor =
                    Json.tryGetProperty "negativeColor" json
                    |> Option.map (fun el -> SvgColor.FromJson(el, SvgColor.Named "red"))
                    |> Option.defaultValue (SvgColor.Named "red")
                  StrokeWidth = Json.tryGetDoubleProperty "strokeWidth" json |> Option.defaultValue 1.
                  OpenValueIndex = ovi
                  CloseValueIndex = cvi
                  HighValueIndex = hvi
                  LowValueIndex = lvi }
                |> Ok
            | None, _, _, _ -> Error "Missing openValueIndex property"
            | _, None, _, _ -> Error "Missing closeValueIndex property"
            | _, _, None, _ -> Error "Missing highValueIndex property"
            | _, _, _, None -> Error "Missing lowValueIndex property"

    let createCandleStickSeriesFromTable (settings: CandleStickChartGeneratorSettings) (table: TableModel) =

        //let (names, series) =
        table.Rows
        |> List.map (fun r ->
            match
                r.TryGetValue settings.TimestampValueIndex,
                r.TryGetValue settings.SeriesSettings.OpenValueIndex,
                r.TryGetValue settings.SeriesSettings.CloseValueIndex,
                r.TryGetValue settings.SeriesSettings.HighValueIndex,
                r.TryGetValue settings.SeriesSettings.LowValueIndex
            with
            | Some ts, Some ovi, Some cvi, Some hvi, Some lvi ->
                match ts with
                | Value.DateTime dt ->
                    (dt.ToString(settings.TimestampFormat),
                     ({ OpenValue = ovi.GetFloat()
                        CloseValue = cvi.GetFloat()
                        HighValue = hvi.GetFloat()
                        LowValue = lvi.GetFloat() }
                     : CandleStickCharts.SeriesValue<float>))
                    |> Ok
                | _ -> Error $"Value at index {settings.TimestampValueIndex} is not a datetime"
            | None, _, _, _, _ -> Error $"Timestamp at index {settings.TimestampValueIndex} not found"
            | _, None, _, _, _ -> Error $"Open value at index {settings.SeriesSettings.OpenValueIndex} not found"
            | _, _, None, _, _ -> Error $"Close value at index {settings.SeriesSettings.CloseValueIndex} not found"
            | _, _, _, None, _ -> Error $"High value at index {settings.SeriesSettings.HighValueIndex} not found"
            | _, _, _, _, None -> Error $"Low value at index {settings.SeriesSettings.LowValueIndex} not found"

        )
        |> flattenResultList
        |> Result.map List.unzip
        |> Result.map (fun (names, values) ->
            ({ SplitValueHandler = floatValueSplitter
               Normalizer = floatRangeNormalizer
               ValueComparer = floatValueComparer
               Style =
                 { PositiveColor = settings.SeriesSettings.PositiveColor
                   NegativeColor = settings.SeriesSettings.NegativeColor
                   StrokeWidth = settings.SeriesSettings.StrokeWidth }
               SectionNames = names
               Values = values }
            : CandleStickCharts.Series<float>))

    let generate (settings: CandleStickChartGeneratorSettings) (table: TableModel) =
        createCandleStickSeriesFromTable settings table
        |> Result.map (fun s ->
            let maxValue =
                match settings.Range.Maximum with
                | RangeValueType.Specific v -> v
                | RangeValueType.UnitSize us ->
                    s.Values
                    |> List.maxBy (fun v -> v.HighValue)
                    |> fun r -> r.HighValue
                    |> ceiling us

            let minValue =
                match settings.Range.Minimum with
                | RangeValueType.Specific v -> v
                | RangeValueType.UnitSize us ->
                    s.Values
                    |> List.minBy (fun v -> v.LowValue)
                    |> fun r -> r.LowValue
                    |> floor us

            CandleStickCharts.generate (settings.ChartSettings.ToSettings()) s minValue maxValue)
