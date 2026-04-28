using System.Data;
using Authorization;
using Db;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Repositories;

var builder = WebApplication.CreateBuilder(args);


{
    var Services = builder.Services;
    var connectionString =
        builder.Configuration.GetConnectionString("ReservationsDb")
        ?? "Data Source=reservations.db;Cache=Shared";

    Services.AddScoped(_ => new SqliteConnection(connectionString));
    Services.AddScoped<IDbConnection>(sp => sp.GetRequiredService<SqliteConnection>());
    Services.AddScoped<GuestRepository>();
    Services.AddScoped<RoomRepository>();
    Services.AddScoped<ReservationRepository>();
    Services.AddMvc(opt =>
    {
        opt.EnableEndpointRouting = false;
    });
    Services.AddCors();
    Services.AddAuthentication("NoOp")
        .AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("NoOp", _ => { });
    Services.AddAuthorization(options =>
    {
        options.AddPolicy("StaffOnly", policy =>
            policy.AddRequirements(new StaffRequirement()));
    });
    Services.AddSingleton<IAuthorizationHandler, StaffAuthorizationHandler>();
    Services.AddEndpointsApiExplorer();
    Services.AddSwaggerGen();
    Services.AddDataProtection();
    Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Trust forwarded headers only from loopback addresses (127.0.0.1 / ::1).
        // Caddy runs on the same machine, so this is sufficient and prevents external
        // clients from spoofing X-Forwarded-For or X-Forwarded-Proto.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        //The fix pins the trusted proxy list to the loopback addresses(127.0.0.1 and::1) instead of trusting everyone. Since Caddy runs on the same machine,
        //all its forwarded headers arrive from loopback and are still honoured.Any request that reaches the ASP.NET Core process from a non-loopback
        //address — e.g. if the port is accidentally exposed — will have its X-Forwarded - *headers silently stripped before reaching auth or cookie middleware,
        //preventing scheme and IP spoofing.
        options.KnownProxies.Add(System.Net.IPAddress.Loopback);    // 127.0.0.1
        options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback); // ::1
    });
}

var app = builder.Build();


{
    try
    {
        using var scope = app.Services.CreateScope();
        await Setup.EnsureDb(scope);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to setup the database, aborting");
        Console.WriteLine(ex.ToString());
        Environment.Exit(1);
        return;
    }

    app.UseForwardedHeaders();
    app.UsePathBase("/api")
        .UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
        .UseAuthentication()
        .UseAuthorization()
        .UseMvc()       
        .UseSwagger()
        .UseSwaggerUI();
}

app.Run();
