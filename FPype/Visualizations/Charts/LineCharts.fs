namespace FPype.Visualizations.Charts

open System
open FPype.Core.Types
open FPype.Data.Models

module LineCharts =

    open FSVG.Charts
    open FPype.Core

    type TimeSeriesEntry = { Value: float; Timestamp: DateTime }

    //let floatSeries
    let createTimeSeriesFromTable (table: TableModel) (valueIndex: int) (timestampIndex: int) =
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

    let settings =
        ({ LeftOffset = 10
           BottomOffset = 10
           TopOffset = 10
           RightOffset = 10
           Title = None
           XLabel = None
           YMajorMarks = [ 50; 100 ]
           YMinorMarks = [ 25; 75 ] }
        : LineCharts.Settings)

    
    let generate ()
