using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WebApplication1
{
    public class Startup
    {
        public const string AcrClaimType = "http://schemas.microsoft.com/claims/authnclassreference";
        public static string ResetPasswordPolicyId;
        public static string SignInPolicyId;
        public static string ProfilePolicyId;
        public static string ClientId;
        public static string AadInstance;
        public static string Tenant;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Add Authentication services.
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme); 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // App config settings
            ClientId = Configuration["AzureAd:ClientId"];
            AadInstance = Configuration["AzureAd:AadInstance"];
            Tenant = Configuration["AzureAd:Tenant"];

            // B2C policy identifiers
            SignInPolicyId = Configuration["AzureAd:Policies:SignIn"].ToLowerInvariant();
            ResetPasswordPolicyId = Configuration["AzureAd:Policies:PasswordReset"].ToLowerInvariant();
            ProfilePolicyId = Configuration["AzureAd:Policies:EditProfile"].ToLowerInvariant();


            // Configure the OWIN pipeline to use OpenID Connect auth.
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(ResetPasswordPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignInPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(ProfilePolicyId));

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        OpenIdConnectOptions CreateOptionsFromPolicy(string policy)
        {
            return new OpenIdConnectOptions
            {
                // For each policy, give OWIN the policy-specific metadata address, and
                // set the authentication type to the id of the policy
                MetadataAddress = string.Format(AadInstance, Tenant, policy),
                AuthenticationScheme = policy,
                CallbackPath = new PathString(string.Format("/{0}", policy)),

                // These are standard OpenID Connect parameters, with values pulled from config.json
                ClientId = ClientId,
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = RemoteFailure
                },
                ResponseType = OpenIdConnectResponseType.IdToken,

                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                },
            };
        }

        // Used for avoiding yellow-screen-of-death
        Task RemoteFailure(FailureContext context)
        {
            context.HandleResponse();

            // Check for password reset message
            if (context.Failure.Message?.Contains("AADB2C90118") ?? false)
            {
                context.Response.Redirect("/Account/ResetPassword");
            }
            else if (context.Failure.Message?.Contains("AADB2C90091") ?? false)
            {
                // canceled edit profile
                context.Response.Redirect("/");
            }
            else
            {
                context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            }

            return Task.FromResult(0);
        }
    }
}
