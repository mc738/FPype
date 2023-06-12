namespace FPype.Infrastructure.Configuration.Resources

[<AutoOpen>]
module Models =

    open System
        
    type NewResource =
        { Reference: string
          Name: string
          Version: NewResourceVersion }

    and NewResourceVersion = {
        Reference: string
        Type: string
        Path: string
        Hash: string
    }

    type ResourceDetails =
        { Reference: string
          Name: string
          Version: ResourceVersionDetails }

    and ResourceVersionDetails =
        { Reference: string
          Version: int
          Type: string
          Path: string
          Hash: string
          CreatedOn: DateTime }

    type ResourceVersionOverview =
        { ResourceReference: string
          Reference: string
          Name: string
          Version: int }

    type ResourceOverview = { Reference: string; Name: string }