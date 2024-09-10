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
                match types |> List.tryFind (fun t -> t.Name = jsonElement.GetString()) with
                | None -> raise (JsonException())
                | Some value ->
                    let jsonObject = doc.RootElement.GetRawText()
                    JsonSerializer.Deserialize(doc.RootElement.GetRawText(), value, options) :?> 'T
            | false, _ -> raise (JsonException())

        override this.Write(writer, value, options) =
            JsonSerializer.Serialize(writer, box value, options)
