namespace FPype.ML

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
    /// Based on https://stackoverflow.com/questions/66893993/ml-net-create-prediction-engine-using-dynamic-class
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
            
            properties |> Map.iter (fun k v -> v.GetBaseType().ToType() |> createProperty dynamicType k)
            
            let t = dynamicType.CreateType()
            
            let r = Activator.CreateInstance(t)
            
            dynamicType.GetProperties()
            |> Seq.iter (fun p ->
                match properties.TryFind p.Name with
                | Some v -> p.SetValue(r, v.Box())
                | None -> ())
            
            r
        
        let createObjectFromType (runTimeType: Type) (properties: Map<string, Value>) =
            //let assemblyName = AssemblyName("DynamicInput")

            //let dynamicType = createTypeBuilder assemblyName
            //createConstructor dynamicType |> ignore
            
            let r = Activator.CreateInstance(runTimeType)
            
            runTimeType.GetProperties()
            |> Seq.iter (fun p ->
                match properties.TryFind p.Name with
                | Some v -> p.SetValue(r, v.Box(stringToReadOnlySpan = true))
                | None -> ())
            
            r
    
    
    module Internal =
        
        let createRunTimeType (schema: DataViewSchema) = ClassFactory.createType schema
        
        let getDynamicPredictionEngine<'TOut> (mlCtx: MLContext) (runTimeType: Type) (schema: DataViewSchema) (model: ITransformer) =
            
            let genericPredictionMethod = mlCtx.Model.GetType().GetMethod("CreatePredictionEngine", [| typeof<ITransformer>; typeof<DataViewSchema> |])
            let predictionMethod = genericPredictionMethod.MakeGenericMethod(runTimeType, typeof<'TOut>)
            predictionMethod.Invoke(mlCtx.Model, [| model; schema |])
            
        let runDynamicPredictionEngine<'TOut> (runTimeType: Type) (engine: obj)  (inputObj: obj) =
            let ms = engine.GetType().GetMethods()
            let predictMethod = engine.GetType().GetMethod("Predict", [| runTimeType |])
            predictMethod.Invoke(engine, [| inputObj |]) :?> 'TOut
            
    
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
       