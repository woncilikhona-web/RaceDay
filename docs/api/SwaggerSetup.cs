using Microsoft.OpenApi.Models;

namespace RaceDay.Api.Setup;

public static class SwaggerSetup
{
    public static IServiceCollection AddRaceDaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RaceDay API",
                Version = "v1",
                Description = "Log in via POST /api/auth/login, copy the token, click Authorize and paste: Bearer {token}"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    new string[] { }
                }
            });
        });
        return services;
    }

    public static WebApplication UseRaceDaySwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1"));
        return app;
    }
}
