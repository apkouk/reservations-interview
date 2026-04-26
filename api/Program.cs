using System.Data;
using Authorization;
using Db;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
}

var app = builder.Build();


{
    try
    {
        Setup.EnsureDb(app.Services.CreateScope());
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to setup the database, aborting");
        Console.WriteLine(ex.ToString());
        Environment.Exit(1);
        return;
    }

    app.UsePathBase("/api")
        .UseAuthentication()
        .UseAuthorization()
        .UseMvc()
        .UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
        .UseSwagger()
        .UseSwaggerUI();
}

app.Run();
