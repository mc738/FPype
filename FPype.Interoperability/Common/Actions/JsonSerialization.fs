namespace FPype.Interoperability.Common.Actions

open System
open System.Text.Json
open System.Text.Json.Serialization

module JsonSerialization =

    type PipelineActionConverter<'T when 'T :> IPipelineAction>() =

        inherit JsonConverter<'T>()

        let types =
            let t = typeof<'T>

            AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.collect (fun a ->
                a.GetTypes()
                |> Seq.filter (fun at -> t.IsAssignableFrom(at) && at.IsClass && (at.IsAbstract |> not)))
            |> Seq.toList


        override this.Read(reader, typeToConvert, options) =
            if reader.TokenType <> JsonTokenType.StartObject then
                raise (JsonException())
                
            use doc = JsonDocument.ParseValue(&reader)
            
            match doc.RootElement.TryGetProperty("actionType") with
            | true, jsonElement ->
                match types |> List.tryFind ()
                
                failwith "todo"
            | false, _ ->
                raise (JsonException())


            failwith "todo"

        override this.Write(writer, value, options) = failwith "todo"



    ()
