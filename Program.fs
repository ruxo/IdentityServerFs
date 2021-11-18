namespace IdentityServerFs

#nowarn "20"
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Serilog
open Serilog.Events
open Serilog.Sinks.SystemConsole.Themes

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        Log.Logger <-
            LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate="[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                                 theme=AnsiConsoleTheme.Code)
                .CreateLogger()

        let builder = WebApplication.CreateBuilder(args)

        builder.Host.UseSerilog()
        // builder.Services.AddControllersWithViews()  // for MVC-based UI
        builder.Services.AddControllers()
        builder.Services.AddIdentityServer(fun options ->
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim <- true
              ).AddInMemoryIdentityResources(Config.IdentityResources)
               .AddInMemoryApiScopes(Config.ApiScopes)
               .AddInMemoryClients(Config.Clients)
               .AddDeveloperSigningCredential() // not recommended for production - you need to store your key material somewhere secure

        let app = builder.Build()

        app.UseHttpsRedirection()
        
        // uncomment, if you want to add MVC
        // app.UseStaticFiles()
        // app.UseRouting()
        
        app.UseIdentityServer()

        // uncomment, if you want to add MVC
        // app.UseAuthorization()
        // app.MapControllers(name = "default", pattern = "{controller=Home}/{action=Index}/{id?}")

        try
            app.Run()
        finally
            Log.CloseAndFlush()

        exitCode
