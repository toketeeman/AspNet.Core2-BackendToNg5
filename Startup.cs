using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)    // DI.
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)      // I.E. set up the injectable services.
        {
            var key = Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value);  // Get JWT secret key.
            services.AddDbContext<DataContext>(
                x => x.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.AddTransient<Seed>();    // One service instance per call (and at most one will happen).        
            services.AddMvc();
            services.AddCors(); // Allows access from the SPA front-end stage. Order does not matter here.
            services.AddAutoMapper();
            services.AddScoped<IAuthRepository, AuthRepository>();  // One service instance per http request.
            services.AddScoped<IDatingRepository, DatingRepository>();  // One service instance per http request.
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => {
                            options.TokenValidationParameters = new TokenValidationParameters{
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(key),
                                ValidateIssuer = false,
                                ValidateAudience = false
                            };
                        });
            services.AddMvc().AddJsonOptions(opt => 
            {
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);  // Adds error message to custom header.
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            // seeder.SeedUsers();      // Uncomment and then enter "dotnet watch run" to re-seed entire database.
            app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());
            app.UseAuthentication();
            app.UseMvc();  // Must come last because it issues a response to the request, ending the pipeline.
        }
    }
}
