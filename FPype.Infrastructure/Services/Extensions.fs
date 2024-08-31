namespace FPype.Infrastructure.Services

open FPype.Infrastructure.Services
open Microsoft.Extensions.Diagnostics.HealthChecks

[<AutoOpen>]
module Extensions =

    open Microsoft.Extensions.DependencyInjection
    open Freql.MySql
    open HealthChecks

    type IServiceCollection with

        member sc.AddFPypeServices(connectionString) =
            sc
                .AddScoped<ServiceContext>(fun _ ->
                    // Use a special class "ServiceContext" here so a connection can be used specifically for the FPype instance.
                    ServiceContext(MySqlContext.Connect(connectionString)))
                .AddScoped<PipelineService>()
                .AddScoped<ConfigurationService>()
                .AddScoped<SchedulingService>()

    type IHealthChecksBuilder with

        member hcb.AddFPypeHealthChecks(?additionalCategories: string list) =
            let ac = additionalCategories |> Option.defaultValue list.Empty

            hcb.AddCheck<FPypeServiceContextConnectionHealthCheck>(
                "fpype-service-context-connection-check",
                HealthStatus.Unhealthy,
                [ "database"; "fpype" ] @ ac
            )
