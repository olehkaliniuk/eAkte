using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Middleware;
using EAktePrototypProjektNewMvc.Repositories;
using EAktePrototypProjektNewMvc.Services;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.EventLog;


using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;


namespace EAktePrototypProjektNewMvc
{
    class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===============================
            // LOGGING CONFIGURATION
            // ===============================
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddEventLog(settings =>
            {
                settings.SourceName = "EAktePrototypProjekt";
                settings.LogName = "Application";
            });

            // ===============================
            // SERVICES
            // ===============================
            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "EAkte.Session";

                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // ===============================
            // COOKIE POLICY: dynamisch je nach Schema (http/https)
            // ===============================
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.OnAppendCookie = ctx => ApplySchemeBasedCookiePolicy(ctx.Context, ctx.CookieOptions);
                options.OnDeleteCookie = ctx => ApplySchemeBasedCookiePolicy(ctx.Context, ctx.CookieOptions);
            });




            // ===============================
            // SSO: Cookie + OpenID Connect (Global Login)
            // ===============================
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // Lokale Anmeldung in eAkte. Absolute Obergrenze wie beim SSO-Cookie (8 Stunden).
                options.Cookie.Name = "EAkte.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = builder.Configuration["Sso:Authority"];
                options.ClientId = builder.Configuration["Sso:ClientId"];
                options.ClientSecret = builder.Configuration["Sso:ClientSecret"];

                // Authorization Code Flow mit PKCE, keine Tokens in der URL.
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("dreif");

                // Claim-Namen bleiben wie im Token (sub, email, name, uid, mandant_id).
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";

                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";

                // Wird für id_token_hint beim Logout gebraucht (wird im Logout-Schritt noch verfeinert).
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.Events = new OpenIdConnectEvents
                {
                    // Nach erfolgreicher Anmeldung am Global Login: Session wie bisher füllen,
                    // damit der restliche eAkte-Code (SessionAuthorize, Mandant-Provider) unverändert funktioniert.
                    OnTokenValidated = context =>
                    {
                        string? email = context.Principal?.FindFirst("email")?.Value;

                        if (string.IsNullOrEmpty(email))
                        {
                            context.Fail("Der Token enthält keine E-Mail-Adresse.");
                            return Task.CompletedTask;
                        }

                        var loginRepository = new LoginRepository(builder.Configuration.GetConnectionString("DefaultConnection"));
                        var user = loginRepository.GetUserByEmail(email);

                        if (user == null || user.Id == 0)
                        {
                            context.Fail("Benutzer nicht gefunden.");
                            return Task.CompletedTask;
                        }

                        var session = context.HttpContext.Session;
                        session.SetInt32("UserId", user.Id);
                        session.SetString("UId", user.UId ?? "");
                        session.SetString("UserEmail", email);
                        session.SetString("UserName", $"{user.Vorname} {user.Name}".Trim());
                        session.SetString("Rollen", string.Join(",", user.Rollen.Select(r => r.Rolle)));
                        session.SetString("AccessCheckMode", "Bescheidbearbeitung");
                        session.SetInt32("LoginMandantId", user.MandantId);

                        return Task.CompletedTask;
                    }
                };
            });




            // Wird benötigt, damit SessionMandantProvider auf den aktuellen
            // HttpContext (und damit auf die Session) zugreifen kann.
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentMandantProvider, SessionMandantProvider>();


            builder.Services.AddScoped<EditSessionRepository>(sp => new EditSessionRepository(builder.Configuration.GetConnectionString("DefaultConnection"), sp.GetRequiredService<ICurrentMandantProvider>()));

            builder.Services.AddScoped<PdfConverterSessionRepository>(sp => new PdfConverterSessionRepository(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSingleton<WordVorlagenService>();

            builder.Services.AddScoped<eAkteRepository>(sp => new eAkteRepository(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sp.GetRequiredService<ICurrentMandantProvider>(),
                sp.GetRequiredService<WordVorlagenService>()));

            builder.Services.AddScoped<ConfigRepository>(sp => new ConfigRepository(builder.Configuration.GetConnectionString("DefaultConnection"), sp.GetRequiredService<ICurrentMandantProvider>()));

            builder.Services.AddScoped<KladdeSessionRepository>(sp => new KladdeSessionRepository(builder.Configuration.GetConnectionString("DefaultConnection"), sp.GetRequiredService<ICurrentMandantProvider>()));

            builder.Services.AddScoped(sp => new TableColumnRepository(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSignalR();

            const string CorsPolicyName = "AllowTaskpane";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.WithOrigins("https://localhost:7006")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            var pathBase = builder.Configuration["PathBase"];   // "/testEakte" 
            if (!string.IsNullOrEmpty(pathBase))
                app.UsePathBase(pathBase);

            // ===============================
            // DREIF.SHARED INITIALISIERUNG
            // ===============================
            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            var mandantProvider = new SessionMandantProvider(httpContextAccessor);

            DreiF.Shared.Setup.UseSqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                "EAktePrototypProjekt",
                "test",
                mandantProvider);

            // ===============================
            // STANDARD MIDDLEWARE
            // ===============================
            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<WebDavEditMiddleware>();


            app.UseRouting();

            app.UseCors(CorsPolicyName);

            // Muss vor UseSession stehen, damit die Cookie-Policy beim Setzen
            // der Session-Cookie greift.
            app.UseCookiePolicy();

            app.UseSession();

            // ===============================
            // GLOBAL EXCEPTION MIDDLEWARE
            // ===============================
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // ===============================
            // STANDARD MIDDLEWARE
            // ===============================
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseStaticFiles();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Login}/{id?}");



            app.Run();
        }

        // Passt SameSite/Secure der Cookie an das tatsächliche Schema des Requests an.
        // https -> SameSite=None + Secure (Standard, für Cross-Site-Szenarien wie Taskpane/CORS).
        // http  -> SameSite=Lax + nicht Secure (sonst würde der Browser die Cookie komplett verwerfen).
        private static void ApplySchemeBasedCookiePolicy(HttpContext context, CookieOptions cookieOptions)
        {
            if (context.Request.IsHttps)
            {
                cookieOptions.Secure = true;
                cookieOptions.SameSite = SameSiteMode.None;
            }
            else
            {
                cookieOptions.Secure = false;
                cookieOptions.SameSite = SameSiteMode.Lax;
            }
        }
    }
}