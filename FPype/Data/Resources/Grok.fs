module FPype.Data.Resources

open System.Text.Json.Serialization

module Grok =

    [<CLIMutable>]
    type GrokPattern =
        { [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("pattern")>]
          Pattern: string }


    /// <summary>
    /// General grok patterns. These are embedded in code to make sure the are always available.
    /// However this might change.
    /// </summary>
    let patterns =
        seq {
            { Name = "USERNAME"
              Pattern = "[a-zA-Z0-9._-]+" }
            { Name = "USER"
              Pattern = "%{USERNAME}" }
            { Name = "EMAILLOCALPART"
              Pattern = "[a-zA-Z][a-zA-Z0-9_.+-=:]+" }
            { Name = "EMAILADDRESS"
              Pattern = "%{EMAILLOCALPART}@%{HOSTNAME}" }
            { Name = "INT"
              Pattern = "(?:[+-]?(?:[0-9]+))" }
            { Name = "BASE10NUM"
              Pattern = "(?<![0-9.+-])(?>[+-]?(?:(?:[0-9]+(?:\.[0-9]+)?)|(?:\.[0-9]+)))" }
            { Name = "NUMBER"
              Pattern = "(?:%{BASE10NUM})" }
            { Name = "BASE16NUM"
              Pattern = "(?<![0-9A-Fa-f])(?:[+-]?(?:0x)?(?:[0-9A-Fa-f]+))" }
            { Name = "BASE16FLOAT"
              Pattern = "\b(?<![0-9A-Fa-f.])(?:[+-]?(?:0x)?(?:(?:[0-9A-Fa-f]+(?:\.[0-9A-Fa-f]*)?)|(?:\.[0-9A-Fa-f]+)))\b" }
        }
