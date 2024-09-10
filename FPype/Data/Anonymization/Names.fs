namespace FPype.Data.Anonymization

[<RequireQualifiedAccess>]
module Names =

    type GeneratorConfiguration =
        { MaleTitles: WeightedList<string>
          FemaleTitles: WeightedList<string>
          MaleFirstNames: string array
          FemaleFirstNames: string array
          MiddleNames: string array
          LastNames: string array }


    ()
