namespace FPype.Infrastructure.Services

[<AutoOpen>]
module Extensions =

    open Microsoft.Extensions.DependencyInjection
    open Freql.MySql

    type IServiceCollection with

        member sc.AddFPypeServices(connectionString) =
            sc
                .AddScoped<MySqlContext>(fun _ -> MySqlContext.Connect(connectionString))
                .AddScoped<PipelineService>()
                .AddScoped<ConfigurationService>()
                .AddScoped<SchedulingService>()
