namespace FPype.Visualizations.Charts

open System
open FPype.Core.Types
open FPype.Data.Models
open FSVG

module LineCharts =

    open FSVG.Charts
    open FPype.Core

    type TimeSeriesEntry = { Value: float; Timestamp: DateTime }

    type ValueRange =
        { Minimum: RangeValueType
          Maximum: RangeValueType }

    and [<RequireQualifiedAccess>] RangeValueType =
        | Specific of Value: float
        | UnitSize of Units: float

    type TimeSeriesChartGeneratorSettings =
        { TimestampValueIndex: int
          TimestampFormat: string
          Range: ValueRange
          ChartSettings: TimeSeriesChartSettings
          SeriesSettings: SeriesSettings list }

    and TimeSeriesChartSettings =
        { LeftOffset: float option
          BottomOffset: float option
          TopOffset: float option
          RightOffset: float option
          Title: string option
          XLabel: string option
          YMajorMarks: float list
          YMinorMarks: float list }

        member tsc.ToLineChartSettings() =
            ({ LeftOffset = tsc.LeftOffset |> Option.defaultValue 10
               RightOffset = tsc.RightOffset |> Option.defaultValue 10
               TopOffset = tsc.TopOffset |> Option.defaultValue 10
               BottomOffset = tsc.BottomOffset |> Option.defaultValue 10
               Title = tsc.Title
               XLabel = tsc.XLabel
               YMajorMarks = tsc.YMajorMarks
               YMinorMarks = tsc.YMinorMarks }
            : LineCharts.Settings)

    and SeriesSettings =
        { ValueIndex: int
          StokeWidth: float
          Color: SvgColor
          LineType: LineCharts.LineType
          Shading: LineCharts.ShadingOptions option }

    let createTimeSeriesFromTable (settings: TimeSeriesChartGeneratorSettings) (table: TableModel) =
        // This will have it's values populated later.
        let seriesValueMap =
            settings.SeriesSettings
            |> List.mapi (fun i s ->
                i,
                ({ Style =
                    { Color = s.Color
                      StokeWidth = s.StokeWidth
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
