namespace FPype.Actions

module Visualizations =

    open FsToolbox.Extensions.Strings
    open FPype.Data.Models
    open FPype.Data.Store
    open FPype.Visualizations.Charts.LineCharts


    module ``generate-time-series-chart-collection`` =

        open FPype.Visualizations.Charts.LineCharts


        type Parameters =
            { ResultBucket: string
              FileNameFormat: string
              CategoriesQuerySql: string
              CategoriesTable: TableModel
              CategoryIndex: int
              TimeSeriesQuerySql: string
              TimeSeriesTable: TableModel
              GeneratorSettings: TimeSeriesChartGeneratorSettings }

        let run (parameters: Parameters) (store: PipelineStore) =


            // Fetch the categories
            // TODO support addition query parameters?
            let categories =
                store.BespokeSelectRows(parameters.CategoriesTable, parameters.CategoriesQuerySql, [])
                |> fun ct ->
                    ct.Rows
                    |> List.choose (fun ctr ->
                        ctr.TryGetValue parameters.CategoryIndex |> Option.map (fun c -> c.GetString()))

            // For each cat - create a time series chart

            // TODO support addition query parameters?
            categories
            |> List.fold
                (fun r cat ->
                    match r with
                    | Ok _ ->
                        store.BespokeSelectRows(parameters.TimeSeriesTable, parameters.TimeSeriesQuerySql, [ cat ])
                        |> generate parameters.GeneratorSettings
                        |> Result.map (fun c ->
                            // If all ok save the chart.
                            store.AddArtifact(
                                System.String.Format(parameters.FileNameFormat, cat),
                                parameters.ResultBucket,
                                "svg",
                                c.ToUtf8Bytes()
                            ))
                    | Error e -> Error e)
                (Ok())
            |> Result.map (fun _ -> store)
