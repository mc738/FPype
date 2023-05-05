namespace FPype.Visualizations.Charts

[<AutoOpen>]
module Common =
   
    open System 
    
    let ceiling (units: float) (value: float) =
        Math.Ceiling(value / units) * units

