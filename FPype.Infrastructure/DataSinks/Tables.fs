namespace FPype.Infrastructure.DataSinks

open FPype.Core.Types
open Microsoft.VisualBasic.CompilerServices


[<RequireQualifiedAccess>]
module Tables =

    open System.IO
    open Freql.Sqlite    
    open FPype.Data.Models
    open FPype.Data.ModelExtensions
    
    let dataSinkColumns = [
        ({ Name = "ds__id"
           Type = BaseType.String
           ImportHandler = None })
        ({ Name = "ds__timestamp"
           Type = BaseType.DateTime
           ImportHandler = None })
    ]
    
    let appendDataSinkColumns (table: TableModel) =
        table.AppendColumns dataSinkColumns
    
    let appendDataSinkData (row: TableRow) =
        ()
    
    
    let initialize (path: string) (model: TableModel)  =
        let fullPath = Path.Combine(path, ())
        
        match File.Exists path with
        | true -> Ok ()
        | false ->
            
            use ctx = SqliteContext.Create("")
            model.AppendColumns(
                [
                    
                ])
            |> fun tm -> tm.
            
            
            
            Ok ()
    
    
    
    let insertRow (row: TableRow) =
        
        
        ()

