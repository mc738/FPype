namespace FPype.Infrastructure.Services

open System.Threading.Tasks
open Freql.MySql
open Microsoft.Extensions.Diagnostics.HealthChecks

module HealthChecks =

    type FPypeServiceContextConnectionHealthCheck(serviceContext: ServiceContext) =

        interface IHealthCheck with
            member this.CheckHealthAsync(context, cancellationToken) =
                let _ = serviceContext.GetContext().ExecuteScalar("SELECT 1;")

                Task.FromResult(
                    HealthCheckResult.Healthy("Reports unhealthy if the FPype MySql database can not be contacted.")
                )
