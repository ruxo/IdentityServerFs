namespace IdentityServerFs

#nowarn "20"
open IdentityServer4
open IdentityServerFs.QuickStart
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
                
        printfn "Starting host..."

        let builder = WebApplication.CreateBuilder(args)

        builder.Host.UseSerilog()
        builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation()
        builder.Services.AddIdentityServer(fun options ->
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim <- true
                
                options.Events.RaiseErrorEvents <- true
                options.Events.RaiseInformationEvents <- true
                options.Events.RaiseFailureEvents <- true
                options.Events.RaiseSuccessEvents <- true
              ).AddTestUsers(TestUsers.defaultUser())
               .AddInMemoryIdentityResources(Config.IdentityResources)
               .AddInMemoryApiScopes(Config.ApiScopes)
               .AddInMemoryClients(Config.Clients)
               .AddDeveloperSigningCredential() // not recommended for production - you need to store your key material somewhere secure
               
        builder.Services
               .AddAuthentication()
               .AddGoogle(fun options ->
                   options.SignInScheme <- IdentityServerConstants.ExternalCookieAuthenticationScheme
                    
                   // register your IdentityServer with Google at https://console.developers.google.com
                   // enable the Google+ API
                   // set the redirect URI to https://localhost:5001/signin-google
                   options.ClientId <- "copy client ID from Google here";
                   options.ClientSecret <- "copy client secret from Google here";
               )

        let app = builder.Build()

        if not (builder.Environment.IsDevelopment()) then
            app.UseExceptionHandler("/Home/Error")
            app.UseHsts() |> ignore // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            
        app.UseHttpsRedirection()
        
        app.UseStaticFiles()
        app.UseRouting()
        
        app.UseIdentityServer()

        // uncomment, if you want to add MVC
        app.UseAuthorization()
        app.MapControllerRoute(name = "default", pattern = "{controller=Home}/{action=Index}/{id?}")

        try
            app.Run()
        finally
            Log.CloseAndFlush()

        exitCode
