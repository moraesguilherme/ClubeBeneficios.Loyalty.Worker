using ClubeBeneficios.Loyalty.Worker.Configuration;
using ClubeBeneficios.Loyalty.Worker.Infrastructure.Database;
using ClubeBeneficios.Loyalty.Worker.Infrastructure.Repositories;
using ClubeBeneficios.Loyalty.Worker.Services;
using ClubeBeneficios.Loyalty.Worker.Workers;
using Dapper;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Clube Beneficios Loyalty Worker";
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<LoyaltyWorkerOptions>(
            context.Configuration.GetSection(LoyaltyWorkerOptions.SectionName));

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ILoyaltyProcessingRepository, LoyaltyProcessingRepository>();
        services.AddScoped<ILoyaltyProcessingService, LoyaltyProcessingService>();
        services.AddHostedService<LoyaltyProcessingBackgroundWorker>();
    });

await builder.RunConsoleAsync();
