namespace FPype.Actions

open System.IO
open FPype.Data.Store


module Export =

    open System
    open FsToolbox.Extensions
    open FPype.Data
    open FPype.Data.Models
    open FPype.Data.Store

    module ``table-to-csv`` =

        let name = "table_to_csv"

        type Parameters =
            { Table: TableModel
              ArtifactName: string
              BucketName: string
              CsvExportSettings: CsvExportSettings }

        let run (parameters: Parameters) (store: PipelineStore) =
            let csv =
                store.SelectRows(parameters.Table).ToCsv(parameters.CsvExportSettings)
                |> String.concat Environment.NewLine

            store.AddArtifact(parameters.ArtifactName, parameters.BucketName, "csv", csv.ToUtf8Bytes())

            store


    module ``export-artifact`` =

        let name = "export_artifact"


        type Parameters =
            { ArtifactName: string
              OutputPath: string option
              FileExtension: string option }

        let run (parameters: Parameters) (store: PipelineStore) =
            // "__exports_path"
            match
                parameters.OutputPath
                |> Option.orElseWith (fun _ -> store.GetStateValue "__exports_path"),
                store.GetArtifact(parameters.ArtifactName)
            with
            | Some path, Some artifact ->
                try
                    File.WriteAllBytes(
                        Path.Combine(
                            path,
                            $"{artifact.Name}.{parameters.FileExtension |> Option.defaultValue artifact.Type}"
                        ),
                        artifact.Data.ToBytes()
                    )
                with exn ->
                    store.LogError(name, $"Unhandled exception when exporting artifact: {exn.Message}")
            | None, _ -> store.LogError(name, "Export path not found.")
            | _, None -> store.LogError(name, $"Artifact `{parameters.ArtifactName}` not found.")

            store


    ()
