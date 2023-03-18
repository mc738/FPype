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
      
        