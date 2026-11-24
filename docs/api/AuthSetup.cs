using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RaceDay.Api.Setup;

public static class AuthSetup
{
    public static IServiceCollection AddRaceDayJwt(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it in appsettings.json / environment variable Jwt__Key.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        services.AddAuthorization();
        return services;
    }
}
