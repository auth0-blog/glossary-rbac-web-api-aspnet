using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace Glossary
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(options =>
      {
        options.Authority = $"https://{Configuration["Auth0:Domain"]}/";
        options.Audience = Configuration["Auth0:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "https://menu-api.demo.com/roles"
        };
      });

      services.AddAuthorization(options =>
      {
        options.AddPolicy("AddAccess", policy =>
                          policy.RequireClaim("permissions", "create:term"));
        options.AddPolicy("UpdateAccess", policy =>
                          policy.RequireClaim("permissions", "update:term"));
        options.AddPolicy("DeleteAccess", policy =>
                          policy.RequireClaim("permissions", "delete:term"));
      });

      services.AddControllers();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyProject", Version = "v1.0.0" });

        c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
          Type = SecuritySchemeType.OAuth2,
          Flows = new OpenApiOAuthFlows()
          {
            Implicit = new OpenApiOAuthFlow()
            {
              AuthorizationUrl = new Uri($"https://{Configuration["Auth0:Domain"]}/authorize"),
              TokenUrl = new Uri($"https://{Configuration["Auth0:Domain"]}/token")
            }
          }
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "oauth2"
              },
              Scheme = "oauth2",
              Name = "oauth2",
              In = ParameterLocation.Header
            },
            new List<string>()
          }
        });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => {
          c.SwaggerEndpoint("/swagger/v1/swagger.json", "Glossary v1");

          c.OAuthClientId(Configuration["Auth0:ClientId"]);
          c.OAuthClientSecret(Configuration["Auth0:ClientSecret"]);
          c.OAuthAppName("GlossaryClient");
          c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "audience", Configuration["Auth0:Audience"] } }); 
          c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
        });
      }

      app.UseHttpsRedirection();
      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
