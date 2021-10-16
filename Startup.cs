using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using Glossary.OpenApiSecurity;

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
      });

      services.AddControllers();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyProject", Version = "v1.0.0" });

        string securityDefinitionName = Configuration["SwaggerUISecurityMode"] ?? "Bearer";
        OpenApiSecurityScheme securityScheme = new OpenApiBearerSecurityScheme();
        OpenApiSecurityRequirement securityRequirement = new OpenApiBearerSecurityRequirement(securityScheme);

        if (securityDefinitionName.ToLower() == "oauth2")
        {
          securityScheme = new OpenApiOAuthSecurityScheme(Configuration["Auth0:Domain"], Configuration["Auth0:Audience"]);
          securityRequirement = new OpenApiOAuthSecurityRequirement();
        }

        c.AddSecurityDefinition(securityDefinitionName, securityScheme);

        c.AddSecurityRequirement(securityRequirement);
      });

      services.AddAuthorization(options =>
      {
        options.AddPolicy("CreateAccess", policy =>
                          policy.RequireClaim("permissions", "create:term"));
        options.AddPolicy("UpdateAccess", policy =>
                          policy.RequireClaim("permissions", "update:term"));
        options.AddPolicy("DeleteAccess", policy =>
                          policy.RequireClaim("permissions", "delete:term"));
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
          c.SwaggerEndpoint("/swagger/v1/swagger.json", "Glossary v1");

          if (Configuration["SwaggerUISecurityMode"]?.ToLower() == "oauth2")
          {
            c.OAuthClientId(Configuration["Auth0:ClientId"]);
            c.OAuthClientSecret(Configuration["Auth0:ClientSecret"]);
            c.OAuthAppName("GlossaryClient");
            c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "audience", Configuration["Auth0:Audience"] } });
            c.OAuthUsePkce();
          }
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
