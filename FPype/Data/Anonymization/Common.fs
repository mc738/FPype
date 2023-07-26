namespace FPype.Data.Anonymization

open System

[<AutoOpen>]
module Common =

    type AnonymizationContext =
        { RNG: Random }

        static member Create(seed) = { RNG = Random(seed) }

        member ctx.NextRandom() = ctx.RNG.Next()

        member ctx.NextRandom(min, max) = ctx.RNG.Next(min, max)

        member ctx.NextChance(value: float) = ctx.RNG.NextDouble() <= value
        
        /// <summary>
        /// Encrypts and packs a byte array with the generated salt.
        /// The salt is prefixed to resulting array.
        /// </summary>
        /// <param name="data">The data to be encrypted.</param>
        /// <param name="key">The encryption key.</param>
        member ctx.EncryptBytesUsingAes(data: byte array, key: byte array) =
            let salt = FsToolbox.Core.Encryption.generateSalt ()
            
            FsToolbox.Core.Encryption.encryptBytesAes key salt data
            |> FsToolbox.Core.Encryption.pack
            
        member ctx.TryUnpackEncryptedAesBytes(data: byte array) =
            FsToolbox.Core.Encryption.unpack data
            

    [<RequireQualifiedAccess>]
    type IncludeType =
        | Always
        | Never
        | Sometimes of Chance: float

    type WeightedValue<'T> = { Value: 'T; Weight: int }

    type WeightedList<'T> =
        { Items: WeightedListItem<'T> list
          Minimum: int
          Maximum: int }

        static member Create<'T>(values: WeightedValue<'T> list) =
            let (items, max) =
                values
                |> List.fold
                    (fun (acc, prev) v ->
                        let next = prev + v.Weight

                        acc
                        @ [ { Value = v.Value
                              From = prev
                              To = next } ],
                        next)
                    ([], 0)

            { Items = items
              Minimum = 0
              Maximum = max }

        member wvl.GetRandom(ctx: AnonymizationContext) =
            let v = ctx.NextRandom(wvl.Minimum, wvl.Maximum)

            wvl.Items |> List.find (fun i -> i.From <= v && v < i.To) |> (fun v -> v.Value)

    and WeightedListItem<'T> = { From: int; To: int; Value: 'T }

    type NonWeightedList<'T> =
        { Values: 'T List }

        static member Create(values: 'T list) = { Values = values }

        member nwl.GetRandom(ctx: AnonymizationContext) =
            nwl.Values[ctx.NextRandom(0, nwl.Values.Length)]


    ()
