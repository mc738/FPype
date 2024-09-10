namespace FPype.Interoperability.Common.Actions

[<AutoOpen>]
module Shared =

    type IPipelineAction =

        abstract member ActionType: string
