namespace FPype.Data.Anonymization

open System

module Common =
    
    type AnonymizationContext =
        { RNG: Random }

        static member Create(seed) = { RNG = Random(seed) }

        member ctx.NextRandom() = ctx.RNG.Next()

        member ctx.NextRandom(min, max) = ctx.RNG.Next(min, max)

        member ctx.NextChance(value: float) = ctx.RNG.NextDouble() <= value
        
    [<RequireQualifiedAccess>]
    type IncludeType =
        | Always
        | Never
        | Sometimes of Chance: float
    
    
    ()

