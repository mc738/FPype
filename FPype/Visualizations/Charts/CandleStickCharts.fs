namespace FPype.Visualizations.Charts

module CandleStickCharts =

    open System
    open System.Text.Json
    open FsToolbox.Core
    open FSVG
    open FSVG.Charts
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models

    type TimeSeriesChartGeneratorSettings =
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
        { OpenValueIndex: int
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
                { OpenValueIndex = ovi
                  CloseValueIndex = cvi
                  HighValueIndex = hvi
                  LowValueIndex = lvi }
                |> Ok
            | None, _, _, _ -> Error "Missing openValueIndex property"
            | _, None, _, _ -> Error "Missing closeValueIndex property"
            | _, _, None, _ -> Error "Missing highValueIndex property"
            | _, _, _, None -> Error "Missing lowValueIndex property"
            
    let createCandleStickSeriesFromTable (settings: TimeSeriesChartGeneratorSettings) (table: TableModel) =
        
        
        
        ({
            SplitValueHandler = floatValueSplitter
            Normalizer = floatRangeNormalizer
            ValueComparer = floatValueComparer
            SectionNames = []
            Series = failwith "todo" 
        }: CandleStickCharts.SeriesCollection<float>)
        |> Ok
