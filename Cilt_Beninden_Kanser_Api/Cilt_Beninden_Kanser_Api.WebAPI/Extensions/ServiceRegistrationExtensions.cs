using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Cilt_Beninden_Kanser_Api.Application.UseCases.CreateAnalysis;
using Cilt_Beninden_Kanser_Api.Application.UseCases.GetAnalysisHistory;
using Cilt_Beninden_Kanser_Api.Application.Validators;
using Cilt_Beninden_Kanser_Api.Infrastructure.Persistence;
using Cilt_Beninden_Kanser_Api.Infrastructure.Persistence.Repositories;
using Cilt_Beninden_Kanser_Api.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

namespace Cilt_Beninden_Kanser_Api.WebAPI.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreateAnalysisHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddValidatorsFromAssemblyContaining<AnalysisRequestValidator>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddHttpClient<IAiInferenceService, AiInferenceService>(client =>
        {
            client.BaseAddress = new Uri(configuration["AiService:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 500)));

        var storageRelativePath = configuration["Storage:ImagesPath"] ?? "wwwroot/uploads/lesions";
        var storagePath = Path.Combine(environment.ContentRootPath, storageRelativePath);
        services.AddScoped<IImageStorageService>(sp =>
            new LocalImageStorageService(
                storagePath,
                sp.GetRequiredService<IImageRepository>(),
                sp.GetRequiredService<ILogger<LocalImageStorageService>>()));

        return services;
    }
}
