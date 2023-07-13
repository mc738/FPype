namespace FPype.Data

[<AutoOpen>]
module Common =

    type PipelineArg =
        { Name: string
          Required: bool
          DefaultValue: string option }

    [<RequireQualifiedAccess>]
    type DataSourceType =
        | File
        | Artifact

        static member Deserialize(str: string) =
            match str.ToLower() with
            | "file" -> Some DataSourceType.File
            | "artifact" -> Some DataSourceType.Artifact
            | _ -> None

        member ds.Serialize() =
            match ds with
            | File -> "file"
            | Artifact -> "artifact"

    type CsvExportSettings =
        { IncludeHeader: bool
          WrapStrings: bool
          WrapDateTimes: bool
          WrapGuids: bool
          WrapBools: bool
          WrapNumbers: bool
          WrapAllValues: bool
          BoolToWord: bool
          DefaultDateTimeFormation: string option
          DefaultGuidFormat: string option }

        static member Default() =
            { IncludeHeader = true
              WrapStrings = true
              WrapGuids = true
              WrapDateTimes = true
              WrapBools = true
              WrapNumbers = false
              WrapAllValues = false
              BoolToWord = true
              DefaultDateTimeFormation = None
              DefaultGuidFormat = None }
