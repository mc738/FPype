namespace FPype.Actions

module Export =

    open System.IO
    open FPype.Data.Store
    open System
    open FsToolbox.Extensions
    open FPype.Data
    open FPype.Data.Models

    module ``table-to-csv`` =

        let name = "table_to_csv"

        type Parameters =
            { Table: TableModel
              ArtifactName: string
              BucketName: string
              CsvExportSettings: CsvExportSettings }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let csv =
                store.SelectRows(parameters.Table).ToCsv(parameters.CsvExportSettings)
                |> String.concat Environment.NewLine

            store.AddArtifact(parameters.ArtifactName, parameters.BucketName, "csv", csv.ToUtf8Bytes())

            store.Log(
                stepName,
                name,
                $"Table `{parameters.Table.Name}` exported to `{parameters.ArtifactName}` csv artifact."
            )

            Ok store

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``export-artifact`` =

        let name = "export_artifact"

        type Parameters =
            { ArtifactName: string
              OutputPath: string option
              FileExtension: string option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            // "__exports_path"
            match
                parameters.OutputPath
                |> Option.orElseWith (fun _ -> store.GetExportsPath())
                |> Option.map store.SubstituteValues,
                store.GetArtifact(parameters.ArtifactName)
            with
            | Some path, Some artifact ->
                try
                    let path =
                        Path.Combine(
                            path,
                            $"{artifact.Name}.{parameters.FileExtension |> Option.defaultValue artifact.Type}"
                        )

                    File.WriteAllBytes(path, artifact.Data.ToBytes())

                    store.Log(stepName, name, $"Artifact `{artifact.Name}` exported to `{path}`")


                with exn ->
                    store.LogError(stepName, name, $"Unhandled exception when exporting artifact: {exn.Message}")
            | None, _ -> store.LogError(stepName, name, "Export path not found.")
            | _, None -> store.LogError(stepName, name, $"Artifact `{parameters.ArtifactName}` not found.")

            Ok store

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``export-artifact-bucket`` =

        let name = "export_artifact_bucket"

        type Parameters =
            { BucketName: string
              OutputPath: string option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            match
                parameters.OutputPath
                |> Option.orElseWith (fun _ -> store.GetExportsPath())
                |> Option.map store.SubstituteValues
            with
            | Some path ->
                store.GetArtifactBucket parameters.BucketName
                |> List.iter (fun a -> File.WriteAllBytes(Path.Combine(path, $"{a.Name}.{a.Type}"), a.Data.ToBytes()))

                store.Log(stepName, name, $"Exported bucket `{parameters.BucketName}` to `{path}`")
                Ok store
            | None ->
                let msg = "Export path not found."
                store.LogError(stepName, name, msg)
                Error msg

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName
