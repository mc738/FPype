namespace FPype.Visualizations.Charts

open Microsoft.FSharp.Core

[<AutoOpen>]
module Common =
   
    open System 
    
    let ceiling (units: float) (value: float) =
        Math.Ceiling(value / units) * units
        
    let floor (units: float) (value: float) =
        Math.Floor(value / units) * units
        
    let floatValueSplitter (percent: float) (maxValue: float) (minValue: float) =
        (minValue) + (((maxValue - minValue) / 100.) * percent) |> string
        

