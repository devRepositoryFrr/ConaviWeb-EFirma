using ConaviWeb.Data;
using ConaviWeb.Data.Repositories;
using ConaviWeb.Model.Common;
using ConaviWeb.Models;
using ConaviWeb.Services;
using ConaviWeb.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ConaviWeb
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
            services.AddControllersWithViews();
            services.AddDistributedMemoryCache();
            services.AddRazorPages();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownProxies.Add(IPAddress.Parse("172.16.250.2"));
            });

            //services.Configure<FormOptions>(options =>
            //{
            //    // Set the limit to 256 MB
            //    options.MultipartBodyLengthLimit = 268435456;
            //});

            //Sesion
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
            });

            //Conexion DataBase
            var mySQLConnectionConfig = new MySQLConfiguration(Configuration.GetConnectionString("MySQLConnection"));
            services.AddSingleton(mySQLConnectionConfig);

            //Mail Service
            services.Configure<MailSetting>(Configuration.GetSection("MailSettings"));
            services.AddTransient<IMailService, MailService>();

            //Dependency 
            services.AddScoped<ISecurityRepository, SecurityRepository>();
            services.AddScoped<ISourceFileRepository, SourceFileRepository>();
            services.AddScoped<ISecurityTools, SecurityTools>();
            services.AddScoped<IUserRepository, UserRepository>();
            var appSettingSection = Configuration.GetSection("AppSettings");
            //services.AddSingleton<HttpClient>();  Revisar el uso de esta inyecci�n
            services.AddScoped<IProcessSignRepository, ProcessSignRepository>();
            services.AddScoped<IProcessSigningService, ProcessSigningService>();
            services.AddScoped<IProcessCancelService, ProcessCancelService>();
            //JWT
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSetting>(appSettingsSection);

            var appSetting = appSettingsSection.Get<AppSetting>();
            var llave = Encoding.ASCII.GetBytes(appSetting.SecretJWT);
            services.AddAuthentication(d =>
            {
                d.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                d.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(d =>
                {
                    d.RequireHttpsMetadata = false;
                    d.SaveToken = true;
                    d.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(llave),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error500");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Status code handling
            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;
                var pathBase = context.HttpContext.Request.PathBase.HasValue
                    ? context.HttpContext.Request.PathBase.Value
                    : "";

                if (response.StatusCode == 401 || response.StatusCode == 403)
                {
                    // Limpiar sesión expirada o inválida
                    context.HttpContext.Session.Clear();
                    response.Redirect($"{pathBase}/Login?expired=true");
                }
                else if (response.StatusCode == 404)
                {
                    response.Redirect($"{pathBase}/Home/Error404");
                }
                await System.Threading.Tasks.Task.CompletedTask;
            });

            app.UseHttpsRedirection();
            //Se requiere para acceder a los archivoscargados
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();

            app.Use(async (context, next) =>
            {
                var token = context.Session.GetString("Token");
                var path = context.Request.Path.Value?.ToLower() ?? "";

                // Rutas públicas que no requieren autenticación
                var publicPaths = new[] { "/login", "/createuser", "/css", "/js", "/lib", "/images", "/favicon" };
                var isPublicPath = Array.Exists(publicPaths, p => path.StartsWith(p) || path == "/");

                if (!string.IsNullOrEmpty(token))
                {
                    // Usar indexer en lugar de Add para evitar excepción si ya existe
                    if (!context.Request.Headers.ContainsKey("Authorization"))
                    {
                        context.Request.Headers["Authorization"] = "Bearer " + token;
                    }
                }
                else if (!isPublicPath && context.Request.Method == "GET")
                {
                    // Sesión expirada en ruta protegida - redirigir a login
                    context.Response.Redirect("/Login");
                    return;
                }
                else if (!isPublicPath && context.Request.Method == "POST")
                {
                    // Sesión expirada en POST - redirigir a login con mensaje
                    context.Response.Redirect("/Login?expired=true");
                    return;
                }

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");
            });
        }
    }
}
