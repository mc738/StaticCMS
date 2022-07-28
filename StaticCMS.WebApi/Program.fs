open System
open System.IO
open System.Security.Cryptography
open Fluff.Core
open Freql.Sqlite
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open StaticCMS
open StaticCMS.DataStore
open System
open System.IO
open System.Security
open System.Text.Json
open System.Text.Json.Serialization
open Freql.MySql
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Diagnostics.HealthChecks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.HttpOverrides
open Giraffe
open Microsoft.Extensions.Logging
open Peeps
open Peeps.Extensions
open Peeps.Logger
open Peeps.Store
open Peeps.Extensions
open Peeps.Monitoring.Extensions
open StaticCMS.Services
open StaticCMS.WebApi

let forwardingHeaderOptions =
    // Forwarding header options for nginx.
    // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0
    let options = ForwardedHeadersOptions()

    options.ForwardedHeaders <-
        ForwardedHeaders.XForwardedFor
        ||| ForwardedHeaders.XForwardedProto

    options

let private authenticationOptions (o: AuthenticationOptions) =
    o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

let getJwtBearerOptions (cfg: JwtBearerOptions) =
    //cfg.SaveToken <- true
    //cfg.IncludeErrorDetails <- true
    //cfg.Authority <- settings.Issuer
    //cfg.Audience <- settings.Audience
    // TODO is this right?
    // NOTE - do not use `use` here - it will cause issued. Instead use let to let it hang around.
    let rsa = new RSACryptoServiceProvider()
    //rsa.FromXmlString(settings.ServerPublicKey)
    cfg.RequireHttpsMetadata <- false

    let p = TokenValidationParameters()
    p.ValidateIssuerSigningKey <- true
    p.ValidateIssuer <- true
    p.ValidateAudience <- true
    p.ValidIssuer <- "StaticCMS"
    p.ValidAudience <- "all"
    p.IssuerSigningKey <- RsaSecurityKey(rsa)

    cfg.TokenValidationParameters <- p

let configureApp (app: IApplicationBuilder) =
    app.UseDeveloperExceptionPage() |> ignore

    app
        .UseForwardedHeaders(forwardingHeaderOptions)
        .UsePeepsMonitor()
        .UseRouting()
        .UseCors(fun (b: CorsPolicyBuilder) ->
            b
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
            |> ignore)

        .UsePeepsHealthChecks()
        .UseAuthentication()
        //.UseFAuth(context)
        
        .UseGiraffe Routes.app

let configureServices
    (monitoringCfg: Monitoring.DataStores.Common.MonitoringStoreConfiguration)
    (logStore: LogStore)
    //(securityContext: SecurityContext)
    //(jwt: Tokens.JwtSettings)
    (dataPath: string)
    (startedOn: DateTime)
    (services: IServiceCollection)
    =
    // TODO add comms support.
    //services.AddHttpClient<CommsClient>() |> ignore

    services
        .AddPeepsLogStore(logStore)
        .AddPeepsMonitorAgent(monitoringCfg)
        .AddPeepsRateLimiting(100)
        .AddSingleton<StaticStore>(fun _ -> StaticStore.Create(Path.Combine(dataPath, "static_store.db")))
        .AddSingleton<StaticCMSService>()
        .AddGiraffe()
        .AddCors()
        .AddAuthentication(authenticationOptions)
        .AddJwtBearer(Action<JwtBearerOptions>(getJwtBearerOptions)) |> ignore
        
    // TODO Add health check support.

    services
        .AddHealthChecks()
        .AddPeepsHealthChecks(5000000L, 1000, startedOn)
    //.AddCheck<DatabaseHealthCheck>("database-check", HealthStatus.Unhealthy, [| "database"; "basic" |])
    //.AddCheck<LogStoreHealthCheck>("log-store-check", HealthStatus.Degraded, [| "log"; "basic" |])
    |> ignore

let configureLogging (peepsCtx: PeepsContext) (logging: ILoggingBuilder) =
    logging.ClearProviders().AddPeeps(peepsCtx)
    |> ignore

[<EntryPoint>]
let main argv =

    let dataPath = "C:\\ProjectData\\static_cms"

    let startedOn = DateTime.UtcNow
    let runId = Guid.NewGuid()

    //let logConnectionString = Environment.GetEnvironmentVariable("CB_LOG_CONNECTION_STRING")

    let store =
        LogStore(dataPath, "static_cms_logs", runId, startedOn)

    let logActions =
        [ Actions.writeToConsole
          Actions.writeToStore store ]

    let monitoringCfg =
        Monitoring.DataStores.Sqlite.Store.config
        <| SqliteContext.Create(Path.Combine(store.Path, $"metrics_{runId}.db"))

    monitoringCfg.MetricsInitialization()

    let peepsCtx =
        PeepsContext.Create("logs", "static_cms_logs", logActions)

    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseKestrel()
                .UseUrls("http://0.0.0.0:8080")
                .Configure(configureApp)
                .ConfigureServices(configureServices monitoringCfg store dataPath startedOn)
                .ConfigureLogging(configureLogging peepsCtx)
            |> ignore)
        .Build()
        .Run()

    0 // return an integer exit code
