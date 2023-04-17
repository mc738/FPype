open System
open System.IO
open System.Text.Json
open FPype
open FPype.Configuration
open FPype.Core
open FPype.Core.Expressions.Parsing
open FPype.Core.JPath
open FPype.Core.Paths
open FPype.Core.Types
open FPype.Data
open FPype.Data.Models
open FPype.ML
open FPype.Scripting.Core
open Microsoft.FSharp.Core
open Microsoft.ML
open Microsoft.ML.Data

module Maths =

    let t _ =

        let values = [ 1m; 2m; 3m; 3m; 9m; 10m ]

        let r = Statistics.standardDeviation values

        ()

module Example =

    let import _ =

        let cfg = ConfigurationStore.Initialize "C:\\ProjectData\\Fpype\\fpype.config"

        let r = cfg.ImportFromFile "C:\\ProjectData\\Fpype\\example_1\\config_v1.json"

        ()

    let run _ =
        let cfg = ConfigurationStore.Load "C:\\ProjectData\\Fpype\\fpype.config"

        match
            PipelineContext.Create(
                cfg,
                "C:\\ProjectData\\Fpype\\runs",
                true,
                "test_pipeline",
                ItemVersion.Specific 1,
                Map.empty
            )
        with
        | Ok ctx ->
            let r = ctx.Run()

            ()
        | Error e ->

            printfn $"Error: {e}"

module ServerReport =

    let import _ =

        let cfg = ConfigurationStore.Initialize "C:\\ProjectData\\Fpype\\fpype2.config"

        let r =
            cfg.ImportFromFile "C:\\ProjectData\\Fpype\\server_report\\config_v1.json"
            |> Result.bind (fun _ ->
                cfg.AddResourceFile(
                    IdType.Generated,
                    "grok_patterns",
                    "text",
                    "C:\\ProjectData\\Fgrok\\patterns.txt",
                    ItemVersion.Specific 1
                ))

        ()

    let run _ =
        let cfg = ConfigurationStore.Load "C:\\ProjectData\\Fpype\\fpype2.config"

        match
            PipelineContext.Create(
                cfg,
                "C:\\ProjectData\\Fpype\\runs\\server_report\\v1",
                true,
                "server_report",
                ItemVersion.Specific 1,
                Map.empty
            )
        with
        | Ok ctx ->
            let r = ctx.Run()

            ()
        | Error e ->

            printfn $"Error: {e}"


module ObjectTableMapperTest =

    let unwrap (r: Result<'a, 'b>) =
        match r with
        | Ok v -> v
        | Error _ -> failwith "Error"

    let run _ =

        let json =
            File.ReadAllText "C:\\ProjectData\\Fpype\\example_data\\example.json" |> toJson

        let scope =
            File.ReadAllText "C:\\ProjectData\\Fpype\\.prototype\\object_table_mapper.json"
            |> toJson
            |> ObjectTableMapScope.FromJson
            |> unwrap

        let table =
            ({ Name = "Test"
               Columns =
                 [ { Name = "item_id"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "name"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "sub_id"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "type"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "inner_name"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "inner_value"
                     Type = BaseType.String
                     ImportHandler = None } ]
               Rows = [] }
            : TableModel)


        let map = ({ Table = table; RootScope = scope }: ObjectTableMap)

        let r = Mapping.ObjectTable.run map json

        ()

module PathTest =



    // To create table columns
    // Start at top level and get values
    // Move down a level and fetch more
    // Continue until lowest level
    // Build rows


    let unwrap (r: Result<'a, 'b>) =
        match r with
        | Ok v -> v
        | Error _ -> failwith "Error"

    let run () =

        let expr =
            match Expressions.Parsing.parse "@.price<10 && @.i == 100 && @.i <= 90 && @.i >= 10" with
            | ExpressionStatementParseResult.Success r -> FilterExpression.FromToken r
            | _ -> failwith "Error"

        let expr2 = Expressions.Parsing.parse "@.price =~ '^s$'"

        //let p = JPath.Compile("$.store.books[?(@.price<10)].face")
        //let p2 = JPath.Compile("$.store.books.face")
        //let p3 = JPath.Compile("$.store.books[?(@.price<10)]")
        //let p4 = JPath.Compile("$.store.books.f")
        //let p5 = JPath.Compile("$.store.f.book")
        //let p6 = JPath.Compile("$.store.f[?(@.price<10)].book")

        let json =
            (File.ReadAllText "C:\\ProjectData\\Fpype\\example_data\\example.json"
             |> JsonDocument.Parse)
                .RootElement

        let topLevel =
            JPath.Compile("$.id") |> Result.map (fun jp -> jp.Run(json)) |> unwrap

        let path = JPath.Compile("$.items[?(@.type =~ '^type1$')]") |> unwrap

        let path = JPath.Compile("$.items[?(@.type =~ '^type1$')].subId") |> unwrap

        let p2 = JPath.Compile("$.id[0]") |> unwrap

        let itemsSelector = path.Run(json)
        let itemsSelector2 = p2.Run(json)

        let here = ()

        let r =
            itemsSelector
            |> List.map (fun el ->
                let sl1 = JPath.Compile("$.type") |> Result.map (fun jp -> jp.Run(el)) |> unwrap
                let sl2 = JPath.Compile("$.subId") |> Result.map (fun jp -> jp.Run(el)) |> unwrap
                let tls = JPath.Compile("$.values") |> Result.map (fun jp -> jp.Run(el)) |> unwrap

                tls
                |> List.map (fun el2 ->
                    let tl1 = JPath.Compile("$.name") |> Result.map (fun jp -> jp.Run(el2)) |> unwrap
                    let tl2 = JPath.Compile("$.value") |> Result.map (fun jp -> jp.Run(el2)) |> unwrap

                    // Zip the bottom level elements to create all rows

                    let r =
                        [ topLevel |> List.tryHead
                          sl1 |> List.tryHead
                          sl2 |> List.tryHead
                          tl1 |> List.tryHead
                          tl2 |> List.tryHead ]


                    ()))


        let secondLevel1 =
            JPath.Compile("$.items.type") |> Result.map (fun jp -> jp.Run(json)) |> unwrap

        let secondLevel2 =
            JPath.Compile("$.items.subId") |> Result.map (fun jp -> jp.Run(json)) |> unwrap


        let thirdLevel1 =
            JPath.Compile("$.items.values.name")
            |> Result.map (fun jp -> jp.Run(json))
            |> unwrap

        let thirdLevel2 =
            JPath.Compile("$.items.values.value")
            |> Result.map (fun jp -> jp.Run(json))
            |> unwrap


        let name =
            topLevel
            |> List.tryHead
            |> Option.map (fun v -> Value.FromJsonValue(v, BaseType.String))


        let p1 = JPath.Compile("$.items") |> Result.map (fun jp -> jp.Run(json))


        ()

module MLTest =

    let unwrap (r: Result<'a, 'b>) =
        match r with
        | Ok v -> v
        | Error _ -> failwith "Error"

    module BinaryClassification =

        let dataPath =
            "D:\\DataSets\\ML_dot_net_test_data\\binary_classification\\wikiDetoxAnnotated40kRows.tsv"

        let modelPath =
            "D:\\DataSets\\ML_dot_net_test_data\\binary_classification\\model\\prediction.zip"

        let train _ =
            let mlCtx = createCtx (Some 1)

            let settings =
                ({ General =
                    { HasHeaders = true
                      Separators = [| '\t' |]
                      AllowQuoting = false
                      ReadMultilines = false
                      TrainingTestSplit = 0.2
                      Columns =
                        [ { Index = 0
                            Name = "Label"
                            DataKind = DataKind.Boolean }
                          { Index = 2
                            Name = "Text"
                            DataKind = DataKind.String } ]
                      RowFilters = []
                      Transformations = [ TransformationType.FeaturizeText("Features", "Text") ] }
                   TrainerType =
                     BinaryClassification.SdcaLogisticRegressionSettings.Default()
                     |> BinaryClassification.TrainerType.SdcaLogisticRegression }
                : BinaryClassification.TrainingSettings)

            let metrics = BinaryClassification.train mlCtx modelPath settings dataPath |> unwrap

            printfn "Model metrics"
            printfn $"Accuracy: {metrics.Accuracy}"
            printfn $"Entropy: {metrics.Entropy}"
            printfn $"Entropy: {metrics.Entropy}"
            printfn $"Confusion matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable()}"
            printfn $"F1 score: {metrics.F1Score}"
            printfn $"Log loss: {metrics.LogLoss}"
            printfn $"Negative precision: {metrics.NegativePrecision}"
            printfn $"Negative recall: {metrics.NegativeRecall}"
            printfn $"Positive precision: {metrics.PositivePrecision}"
            printfn $"Positive recall: {metrics.PositiveRecall}"
            printfn $"Log loss reduction: {metrics.LogLossReduction}"
            printfn $"Area under roc curve: {metrics.AreaUnderRocCurve}"
            printfn $"Area under precision recall curve: {metrics.AreaUnderPrecisionRecallCurve}"

        let run _ =

            let mlCtx = createCtx (Some 1)
            let engine = BinaryClassification.load mlCtx modelPath |> unwrap

            let r = BinaryClassification.predict engine "I hate this movie, it's crap!"


            ()

    module MulticlassClassification =

        let dataPath =
            "D:\\DataSets\\ML_dot_net_test_data\\multiclass_classification\\github_issues.tsv"

        let modelPath =
            "D:\\DataSets\\ML_dot_net_test_data\\multiclass_classification\\model\\prediction.zip"

        let train _ =
            let mlCtx = createCtx (Some 0)

            let settings =
                ({ General =
                    { HasHeaders = true
                      Separators = [| '\t' |]
                      AllowQuoting = false
                      ReadMultilines = false
                      TrainingTestSplit = 0.2
                      Columns =
                        [ { Index = 0
                            Name = "ID"
                            DataKind = DataKind.String }
                          { Index = 1
                            Name = "Area"
                            DataKind = DataKind.String }
                          { Index = 2
                            Name = "Title"
                            DataKind = DataKind.String }
                          { Index = 3
                            Name = "Description"
                            DataKind = DataKind.String } ]
                      RowFilters = []
                      Transformations =
                        [ TransformationType.MapValueToKey("Label", "Area")
                          TransformationType.FeaturizeText("TitleFeaturized", "Title")
                          TransformationType.FeaturizeText("DescriptionFeaturized", "Description")
                          TransformationType.Concatenate("Features", [ "TitleFeaturized"; "DescriptionFeaturized" ]) ] }
                   TrainerType =
                     MulticlassClassification.SdcaMaximumEntropySettings.Default()
                     |> MulticlassClassification.TrainerType.SdcaMaximumEntropy }
                : MulticlassClassification.TrainingSettings)

            let metrics =
                MulticlassClassification.train mlCtx modelPath settings dataPath |> unwrap

            printfn "Model metrics"
            printfn $"Confusion matrix: {metrics.ConfusionMatrix}"
            printfn $"Log loss: {metrics.LogLoss}"
            printfn $"Macro accuracy: {metrics.MacroAccuracy}"
            printfn $"Micro accuracy: {metrics.MicroAccuracy}"
            printfn $"Log loss reduction: {metrics.LogLossReduction}"
            printfn $"Top K accuracy: {metrics.TopKAccuracy}"
            printfn $"Per class log loss: {metrics.PerClassLogLoss}"
            printfn $"Top K prediction count: {metrics.TopKPredictionCount}"
            printfn $"Top K accuracy for all K: {metrics.TopKAccuracyForAllK}"


        let run _ =
            let mlCtx = createCtx (Some 0)

            let value =
                [ "Id", Value.String ""
                  "Area", Value.String ""
                  "Title", Value.String "WebSockets communication is slow in my machine"
                  "Description",
                  Value.String
                      "The WebSockets communication used under the covers by SignalR looks like is going slow in my development machine.." ]
                |> Map.ofList

            let (t, dvs) = MulticlassClassification.load mlCtx modelPath |> unwrap

            let r = MulticlassClassification.predict mlCtx t dvs value

            ()

    module Regression =

        let dataPath = "D:\\DataSets\\ML_dot_net_test_data\\regression\\taxi-fare-full.csv"

        let modelPath =
            "D:\\DataSets\\ML_dot_net_test_data\\regression\\\\model\\prediction.zip"

        let train _ =
            let mlCtx = createCtx (Some 0)

            let settings =
                ({ General =
                    { HasHeaders = true
                      Separators = [| ',' |]
                      AllowQuoting = false
                      ReadMultilines = false
                      TrainingTestSplit = 0.2
                      Columns =
                        [ { Index = 0
                            Name = "VendorId"
                            DataKind = DataKind.String }
                          { Index = 1
                            Name = "RateCode"
                            DataKind = DataKind.String }
                          { Index = 2
                            Name = "PassengerCount"
                            DataKind = DataKind.Single }
                          { Index = 3
                            Name = "TripTime"
                            DataKind = DataKind.Single }
                          { Index = 4
                            Name = "TripDistance"
                            DataKind = DataKind.Single }
                          { Index = 5
                            Name = "PaymentType"
                            DataKind = DataKind.String }
                          { Index = 6
                            Name = "FareAmount"
                            DataKind = DataKind.Single } ]
                      RowFilters =
                        [ { ColumnName = "FareAmount"
                            Minimum = Some 1
                            Maximum = Some 150 } ]
                      Transformations =
                        [ TransformationType.CopyColumns("Label", "FareAmount")
                          TransformationType.OneHotEncoding("VendorIdEncoded", "VendorId")
                          TransformationType.OneHotEncoding("RateCodeEncoded", "RateCode")
                          TransformationType.OneHotEncoding("PaymentTypeEncoded", "PaymentType")
                          TransformationType.NormalizeMeanVariance "PassengerCount"
                          TransformationType.NormalizeMeanVariance "TripTime"
                          TransformationType.NormalizeMeanVariance "TripDistance"
                          TransformationType.Concatenate(
                              "Features",
                              [ "VendorIdEncoded"
                                "RateCodeEncoded"
                                "PaymentTypeEncoded"
                                "PassengerCount"
                                "TripTime"
                                "TripDistance" ]
                          ) ] }
                   TrainerType = Regression.SdcaSettings.Default() |> Regression.TrainerType.Sdca }
                : Regression.TrainingSettings)

            let metrics = Regression.train mlCtx modelPath settings dataPath |> unwrap

            printfn "Model metrics"
            printfn $"Loss function: {metrics.LossFunction}"
            printfn $"R squared: {metrics.RSquared}"
            printfn $"Mean absolute error: {metrics.MeanAbsoluteError}"
            printfn $"Mean squared error: {metrics.MeanSquaredError}"
            printfn $"Root mean squared error: {metrics.RootMeanSquaredError}"


        let run _ =
            let mlCtx = createCtx (Some 0)

            let value =
                [ "VendorId", Value.String "VTS"
                  "RateCode", Value.String "1"
                  "PassengerCount", Value.Float 1f
                  "TripTime", Value.Float 1140f
                  "TripDistance", Value.Float 3.75f
                  "PaymentType", Value.String "CRD"
                  "FareAmount", Value.Float 0f ]
                |> Map.ofList

            let (t, dvs) = Regression.load mlCtx modelPath |> unwrap

            let r = Regression.predict mlCtx t dvs value

            ()

    module MatrixFactorization =

        let dataPath =
            "D:\\DataSets\\ML_dot_net_test_data\\matrix_factorization\\movie_recommendations.csv"

        let modelPath =
            "D:\\DataSets\\ML_dot_net_test_data\\matrix_factorization\\model\\prediction.zip"

        let train _ =
            let mlCtx = createCtx (Some 0)

            let settings =
                ({ General =
                    { HasHeaders = true
                      Separators = [| ',' |]
                      AllowQuoting = false
                      ReadMultilines = false
                      TrainingTestSplit = 0.01
                      Columns =
                        [ { Index = 0
                            Name = "UserId"
                            DataKind = DataKind.Single }
                          { Index = 1
                            Name = "MovieId"
                            DataKind = DataKind.Single }
                          { Index = 2
                            Name = "Label"
                            DataKind = DataKind.Single } ]
                      RowFilters = []
                      Transformations =
                        [ TransformationType.MapValueToKey("UserIdEncoded", "UserId")
                          TransformationType.MapValueToKey("MovieIdEncoded", "MovieId") ] }
                   TrainerType =
                     ({ Alpha = None
                        C = None
                        Lambda = None
                        ApproximationRank = Some 100
                        LearningRate = None
                        LossFunction = None
                        NonNegative = None
                        LabelColumnName = "Label"
                        NumberOfIterations = Some 20
                        NumberOfThreads = None
                        MatrixColumnIndexColumnName = "UserIdEncoded"
                        MatrixRowIndexColumnName = "MovieIdEncoded" }
                     : MatrixFactorization.MatrixFactorizationSettings)
                     |> MatrixFactorization.TrainerType.MatrixFactorization }
                : MatrixFactorization.TrainingSettings)

            let metrics = MatrixFactorization.train mlCtx modelPath settings dataPath |> unwrap

            printfn "Model metrics"
            printfn $"Loss function: {metrics.LossFunction}"
            printfn $"R squared: {metrics.RSquared}"
            printfn $"Mean absolute error: {metrics.MeanAbsoluteError}"
            printfn $"Mean squared error: {metrics.MeanSquaredError}"
            printfn $"Root mean squared error: {metrics.RootMeanSquaredError}"


        let run _ =
            let mlCtx = createCtx (Some 0)

            let value =
                [ "UserId", Value.Float 6f
                  "MovieId", Value.Float 10f
                  "Label", Value.Float 0f ]
                |> Map.ofList

            let (t, dvs) = MatrixFactorization.load mlCtx modelPath |> unwrap

            let r = MatrixFactorization.predict mlCtx t dvs value

            ()

module FakeNewsTest =

    let unwrap (r: Result<'a, 'b>) =
        match r with
        | Ok v -> v
        | Error _ -> failwith "Error"

    let dataPath = "D:\\DataSets\\fake_news\\fake_news_dataset.csv"

    let modelPath = "D:\\DataSets\\fake_news\\model\\model.zip"

    let train _ =
        let mlCtx = createCtx (Some 0)

        let settings =
            ({ General =
                { HasHeaders = true
                  Separators = [| ',' |]
                  AllowQuoting = true
                  ReadMultilines = true
                  TrainingTestSplit = 0.2
                  Columns =
                    [ { Index = 0
                        Name = "Author"
                        DataKind = DataKind.String }
                      { Index = 9
                        Name = "Title"
                        DataKind = DataKind.String }
                      //{ Index = 10
                      //  Name = "Text"
                      //  DataKind = DataKind.String }
                      { Index = 11
                        Name = "SiteUrl"
                        DataKind = DataKind.String }
                      //{ Index = 7
                      //  Name = "Type"
                      //  DataKind = DataKind.String }
                      { Index = 8
                        Name = "Fake"
                        DataKind = DataKind.String } ]
                  RowFilters = []
                  Transformations =
                    [ TransformationType.MapValueToKey("Label", "Fake")
                      TransformationType.FeaturizeText("TitleFeaturized", "Title")
                      //TransformationType.FeaturizeText("TextFeaturized", "Text")
                      TransformationType.OneHotEncoding("AuthorEncoded", "Author")
                      TransformationType.OneHotEncoding("SiteUrlEncoded", "SiteUrl")
                      TransformationType.Concatenate(
                          "Features",
                          [ "TitleFeaturized" (*"TextFeaturized";*) ; "AuthorEncoded"; "SiteUrlEncoded" ]
                      ) ] }
               TrainerType =
                 MulticlassClassification.SdcaMaximumEntropySettings.Default()
                 |> MulticlassClassification.TrainerType.SdcaMaximumEntropy }
            : MulticlassClassification.TrainingSettings)

        let metrics =
            MulticlassClassification.train mlCtx modelPath settings dataPath |> unwrap

        printfn "Model metrics"
        printfn $"Confusion matrix: {metrics.ConfusionMatrix}"
        printfn $"Log loss: {metrics.LogLoss}"
        printfn $"Macro accuracy: {metrics.MacroAccuracy}"
        printfn $"Micro accuracy: {metrics.MicroAccuracy}"
        printfn $"Log loss reduction: {metrics.LogLossReduction}"
        printfn $"Top K accuracy: {metrics.TopKAccuracy}"
        printfn $"Per class log loss: {metrics.PerClassLogLoss}"
        printfn $"Top K prediction count: {metrics.TopKPredictionCount}"
        printfn $"Top K accuracy for all K: {metrics.TopKAccuracyForAllK}"


    let run _ =

        let mlCtx = createCtx (Some 0)

        let value =
            [ "Author", Value.String "Fed Up"
              "Title", Value.String "hilary angry at protestor"
              "SiteUrl", Value.String "100percentfedup.com"
              "Fake", Value.String "" ]
            |> Map.ofList

        let (t, dvs) = MulticlassClassification.load mlCtx modelPath |> unwrap

        let r = MulticlassClassification.predict mlCtx t dvs value

        ()

module MiscTest =

    let createDynamicObj _ =

        let properties =
            [ "Field1", Value.Int 42; "Field2", Value.String "Hello, World!" ] |> Map.ofList

        let r = FPype.ML.Common.ClassFactory.createObject properties

        ()

module CommsTest =

    open System.IO
    open System.IO.Pipes

    let testClient _ =
        async {

            use ctx = new ScriptContext("testpipe")

            //let ctx = Client.start "testpipe"

            for i in [ 0..10 ] do
                match ctx.SendRequest(IPC.RequestMessage.RawMessage $"Hello server. Time is {DateTime.UtcNow}") with
                | Ok res ->
                    match res with
                    | IPC.ResponseMessage.RawMessage b -> printfn $"[CLIENT] {b}"
                    | IPC.ResponseMessage.Close -> ()
                | Error e -> printfn $"ERROR - {e}"

                do! Async.Sleep 1000

            printfn "Client complete."
            return true
        }

    let run _ =

        //let h = Scripting.Core.IPC.Header.Create(12, 1uy, 2uy)

        //let r = Scripting.Core.IPC.Header.TryDeserialize(h.Serialize())

        let handler (req: IPC.RequestMessage) =
            match req with
            | IPC.RequestMessage.RawMessage b ->
                printfn $"[SERVER] {b}"
                IPC.ResponseMessage.RawMessage $"Hello from server - {DateTime.UtcNow}" |> Some
            | IPC.RequestMessage.Close ->
                printfn $"[SERVER] Close request received."
                None



        let server = async { return Server.start handler "testpipe" }

        let _ = testClient () |> Async.Ignore |> Async.Start

        let r = server |> Async.RunSynchronously

        printfn "Complete"

        ()


CommsTest.run ()

//MiscTest.createDynamicObj ()
//MLTest.train ()
//MLTest.run ()
//FakeNewsTest.train ()
//FakeNewsTest.run ()

//MLTest.MatrixFactorization.train ()
//MLTest.MatrixFactorization.run ()
//MLTest.MulticlassClassification.train ()
//MLTest.MulticlassClassification.run ()
//MLTest.Regression.train ()
//MLTest.Regression.run ()
//MLTest.BinaryClassification.train ()
MLTest.BinaryClassification.run ()


ObjectTableMapperTest.run ()
PathTest.run ()
Maths.t ()

//Example.import ()
//Example.run ()

// ServerReport.import ()
ServerReport.run ()
// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
