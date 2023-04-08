﻿namespace FPype.ML

open System
open System.Reflection
open FPype.Core.Types
open FPype.Data
open FPype.Data.Store
open Microsoft.Data.Sqlite
open Microsoft.ML
open Microsoft.ML.Data

[<AutoOpen>]
module Common =

    /// <summary>
    /// This is used because ML.net required generic types for prediction engines.
    /// This will create a dynamic class type that can be used.
    /// Based on https://stackoverflow.com/questions/66893993/ml-net-create-prediction-engine-using-dynamic-class/66913705#66913705
    /// </summary>
    [<RequireQualifiedAccess>]
    module ClassFactory =

        open System.Reflection
        open System.Reflection.Emit

        let createTypeBuilder (assemblyName: AssemblyName) =
            let assemblyBuilder =
                AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)

            let moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule")

            moduleBuilder.DefineType(
                assemblyName.FullName,
                TypeAttributes.Public
                ||| TypeAttributes.Class
                ||| TypeAttributes.AutoClass
                ||| TypeAttributes.AnsiClass
                ||| TypeAttributes.BeforeFieldInit
                ||| TypeAttributes.AutoLayout,
                null
            )

        let createConstructor (typeBuilder: TypeBuilder) =
            typeBuilder.DefineDefaultConstructor(
                MethodAttributes.Public
                ||| MethodAttributes.SpecialName
                ||| MethodAttributes.RTSpecialName
            )

        let createProperty (typeBuilder: TypeBuilder) (propertyName: string) (propertyType: Type) =
            let fieldBuilder =
                typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private)

            let propertyBuilder =
                typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null)

            let getPropMthdBldr =
                typeBuilder.DefineMethod(
                    $"get_{propertyName}",
                    MethodAttributes.Public
                    ||| MethodAttributes.SpecialName
                    ||| MethodAttributes.HideBySig,
                    propertyType,
                    Type.EmptyTypes
                )

            let getIl = getPropMthdBldr.GetILGenerator()

            getIl.Emit(OpCodes.Ldarg_0)
            getIl.Emit(OpCodes.Ldfld, fieldBuilder)
            getIl.Emit(OpCodes.Ret)

            let setPropMthdBldr =
                typeBuilder.DefineMethod(
                    $"set_{propertyName}",
                    MethodAttributes.Public
                    ||| MethodAttributes.SpecialName
                    ||| MethodAttributes.HideBySig,
                    null,
                    [| propertyType |]
                )

            let setIl = setPropMthdBldr.GetILGenerator()

            let modifyProperty = setIl.DefineLabel()
            let exitSet = setIl.DefineLabel()

            setIl.MarkLabel(modifyProperty)
            setIl.Emit(OpCodes.Ldarg_0)
            setIl.Emit(OpCodes.Ldarg_1)
            setIl.Emit(OpCodes.Stfld, fieldBuilder)

            setIl.Emit(OpCodes.Nop)
            setIl.MarkLabel(exitSet)
            setIl.Emit(OpCodes.Ret)

            propertyBuilder.SetGetMethod(getPropMthdBldr)
            propertyBuilder.SetSetMethod(setPropMthdBldr)

        let createType (schema: DataViewSchema) =
            let assemblyName = AssemblyName("DynamicInput")

            let dynamicType = createTypeBuilder assemblyName
            //let
            createConstructor dynamicType |> ignore
            schema |> Seq.iter (fun i -> createProperty dynamicType i.Name i.Type.RawType)

            dynamicType.CreateType()

        let createObject (properties: Map<string, Value>) =
            let assemblyName = AssemblyName("DynamicInput")

            let dynamicType = createTypeBuilder assemblyName
            createConstructor dynamicType |> ignore

            properties
            |> Map.iter (fun k v -> v.GetBaseType().ToType() |> createProperty dynamicType k)

            let t = dynamicType.CreateType()

            let r = Activator.CreateInstance(t)

            dynamicType.GetProperties()
            |> Seq.iter (fun p ->
                match properties.TryFind p.Name with
                | Some v -> p.SetValue(r, v.Box())
                | None -> ())

            r

        let createObjectFromType (runTimeType: Type) (properties: Map<string, Value>) =

            let r = Activator.CreateInstance(runTimeType)

            runTimeType.GetProperties()
            |> Seq.iter (fun p ->
                match properties.TryFind p.Name with
                | Some v -> p.SetValue(r, v.Box(stringToReadOnlyMemory = true))
                | None -> ())

            r


    module Internal =

        let createRunTimeType (schema: DataViewSchema) = ClassFactory.createType schema

        /// <summary>
        /// Get a dynamic prediction engine.
        /// This is used to work around the compile-time generics usually needed for ML.net.
        /// This function foregoes a lot of checks and should be used for internal use only.
        /// It is important to create the run time type separately in and pass it in,
        /// because creating the same runtime type twice will show as different types and cause issues.
        /// </summary>
        /// <param name="mlCtx"></param>
        /// <param name="runTimeType">The runtime type.</param>
        /// <param name="schema"></param>
        /// <param name="model"></param>
        let getDynamicPredictionEngine<'TOut>
            (mlCtx: MLContext)
            (runTimeType: Type)
            (schema: DataViewSchema)
            (model: ITransformer)
            =

            let genericPredictionMethod =
                mlCtx
                    .Model
                    .GetType()
                    .GetMethod("CreatePredictionEngine", [| typeof<ITransformer>; typeof<DataViewSchema> |])

            let predictionMethod =
                genericPredictionMethod.MakeGenericMethod(runTimeType, typeof<'TOut>)

            predictionMethod.Invoke(mlCtx.Model, [| model; schema |])

        /// <summary>
        /// Run a dynamic prediction engine, represented as a obj.
        /// This is used to work around the compile-time generics usually needed for ML.net.
        /// This function foregoes a lot of checks and should be used for internal use only.
        /// It is important to create the run time type separately in and pass it in,
        /// because creating the same runtime type twice will show as different types and cause issues.
        /// </summary>
        /// <param name="runTimeType"></param>
        /// <param name="engine"></param>
        /// <param name="inputObj"></param>
        let runDynamicPredictionEngine<'TOut> (runTimeType: Type) (engine: obj) (inputObj: obj) =
            let ms = engine.GetType().GetMethods()
            let predictMethod = engine.GetType().GetMethod("Predict", [| runTimeType |])
            predictMethod.Invoke(engine, [| inputObj |]) :?> 'TOut

        let downcastPipeline (x: IEstimator<_>) =
            match x with
            | :? IEstimator<ITransformer> as y -> y
            | _ -> failwith "downcastPipeline: expecting a IEstimator<ITransformer>"


    type BaseType with

        member bt.ToDataKind() =
            let rec handler (bt: BaseType) =
                match bt with
                | BaseType.Boolean -> DataKind.Boolean
                | BaseType.Byte -> DataKind.Byte
                | BaseType.Char -> DataKind.String
                | BaseType.Decimal -> DataKind.Double
                | BaseType.Double -> DataKind.Double
                | BaseType.Float -> DataKind.Single
                | BaseType.Guid -> DataKind.String
                | BaseType.Int -> DataKind.Int32
                | BaseType.Long -> DataKind.Int64
                | BaseType.Short -> DataKind.Int16
                | BaseType.String -> DataKind.String
                | BaseType.DateTime -> DataKind.DateTime
                | BaseType.Option ibt -> handler ibt

            handler bt

    type TrainingContext =
        { TrainingData: IDataView
          TestData: IDataView
          Pipeline: IEstimator<ITransformer> }

    [<RequireQualifiedAccess>]
    type TransformationType =
        | CopyColumns of OutputColumnName: string * InputColumnName: string
        | OneHotEncoding of OutputColumnName: string * InputColumnName: string
        | NormalizeMeanVariance of OutputColumnName: string
        | FeaturizeText of OutputColumnName: string * InputColumnName: string
        | MapValueToKey of OutputColumnName: string * InputColumnName: string
        | Concatenate of OutputColumnName: string * Columns: string list

    type DataColumn =
        { Index: int
          Name: string
          DataKind: DataKind }

    type RowFilter =
        { ColumnName: string
          Minimum: float option
          Maximum: float option }

    type GeneralTrainingSettings =
        { DataSource: DataSource
          ModelSavePath: string
          HasHeaders: bool
          Separators: char array
          TrainingTestSplit: float
          Columns: DataColumn list
          RowFilters: RowFilter list
          Transformations: TransformationType list }

    let createCtx (seed: int option) = MLContext(seed |> Option.toNullable)

    let getDataSourceUri (source: DataSource) =
        match DataSourceType.Deserialize source.Type with
        | Some DataSourceType.File -> source.Uri |> Ok
        | Some DataSourceType.Artifact ->
            // NOTE when not a file create a temporary file to hold it or load from another source?
            Error "Artifact data sources to be implemented"
        | None -> Error "Unknown data source type"

    let loadFromTable () =
        DatabaseSource(SqliteFactory.Instance, "", "")

    let createTextLoader (mlCtx: MLContext) (hasHeaders: bool) (separators: char array) (columns: DataColumn list) =
        let options = TextLoader.Options()

        options.HasHeader <- hasHeaders
        options.Separators <- separators

        options.Columns <-
            columns
            |> List.map (fun c -> TextLoader.Column(c.Name, c.DataKind, c.Index))
            |> Array.ofList

        mlCtx.Data.CreateTextLoader(options = options)

    let filterRows (mlCtx: MLContext) (data: IDataView) (filters: RowFilter list) =
        filters
        |> List.fold
            (fun dv rf ->
                match rf.Minimum, rf.Maximum with
                | Some min, Some max ->
                    mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, lowerBound = min, upperBound = max)
                | Some min, None -> mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, lowerBound = min)
                | None, Some max -> mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, upperBound = max)
                | None, None -> dv)
            data

    let runTransformations (mlCtx: MLContext) (transformations: TransformationType list) =
        transformations
        |> List.fold
            (fun (acc: IEstimator<ITransformer>) t ->
                match t with
                | TransformationType.Concatenate (outputColumnName, columns) ->
                    acc.Append(mlCtx.Transforms.Concatenate(outputColumnName, columns |> Array.ofList))
                    |> Internal.downcastPipeline
                | TransformationType.OneHotEncoding (outputColumnName, inputColumnName) ->
                    acc.Append(mlCtx.Transforms.Categorical.OneHotEncoding(outputColumnName, inputColumnName))
                    |> Internal.downcastPipeline
                | TransformationType.NormalizeMeanVariance outputColumnName ->
                    acc.Append(mlCtx.Transforms.NormalizeMeanVariance(outputColumnName))
                    |> Internal.downcastPipeline
                | TransformationType.FeaturizeText (outputColumnName, inputColumnName) ->
                    acc.Append(mlCtx.Transforms.Text.FeaturizeText(outputColumnName, inputColumnName))
                    |> Internal.downcastPipeline
                | TransformationType.MapValueToKey (outputColumnName, inputColumnName) ->
                    acc.Append(mlCtx.Transforms.Conversion.MapValueToKey(outputColumnName, inputColumnName))
                    |> Internal.downcastPipeline
                | TransformationType.CopyColumns (outputColumnName, inputColumnName) ->
                    acc.Append(mlCtx.Transforms.CopyColumns(outputColumnName, inputColumnName))
                    |> Internal.downcastPipeline)
            (EstimatorChain() |> Internal.downcastPipeline)

    let createTrainingContext (mlCtx: MLContext) (settings: GeneralTrainingSettings) (dataUri: string) =

        let loader =
            createTextLoader mlCtx settings.HasHeaders settings.Separators settings.Columns

        let dataView = loader.Load([| dataUri |])

        let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

        let trainingData = filterRows mlCtx trainTestSplit.TrainSet settings.RowFilters

        let dataProcessPipeline = runTransformations mlCtx settings.Transformations

        { TrainingData = trainingData
          TestData = trainTestSplit.TestSet
          Pipeline = dataProcessPipeline }
