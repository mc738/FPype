namespace FPype.Visualizations.Charts

open System.Text.Json
open FSVG.Charts
open FsToolbox.Core

module ScatterCharts =

    type TwoWayComparisonChartGeneratorSettings =
        { XValueIndex: int
          YValueIndex: int
          XRange: ValueRange
          YRange: ValueRange }

    and TwoWayComparisonChartSettings =
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
          XMajorMarkers: float list
          XMinorMarkers: float list
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
              XMajorMarkers = [ 50.; 100. ]
              XMinorMarkers = [ 25.; 75. ]
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
              XMajorMarkers =
                Json.tryGetArrayProperty "yMajorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue []
              XMinorMarkers =
                Json.tryGetArrayProperty "yMinorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue []
              YMajorMarkers =
                Json.tryGetArrayProperty "yMajorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue []
              YMinorMarkers =
                Json.tryGetArrayProperty "yMinorMarkers" json
                |> Option.map (List.choose (Json.tryGetDouble))
                |> Option.defaultValue [] }

        member tsc.ToScatterChartSettings() =
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
               XMajorMarkers = tsc.YMajorMarkers
               XMinorMarkers = tsc.YMinorMarkers
               YMajorMarkers = tsc.YMajorMarkers
               YMinorMarkers = tsc.YMinorMarkers }
            : ScatterCharts.Settings)


    ()
