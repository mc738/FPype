namespace FPype.Infrastructure.Services

open Freql.MySql

type ServiceContext(ctx: MySqlContext) =
    
    
    member _.GetContext() = ctx

