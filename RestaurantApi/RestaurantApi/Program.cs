using System.Runtime.CompilerServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using RestaurantAPI.Authorization;
using RestaurantAPI.Entities;
using RestaurantAPI.Middleware;
using RestaurantAPI.Models.Validators;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using RestaurantAPI;
using System.Text;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.OpenApi.Models;

[assembly: InternalsVisibleTo("RestaurantAPI.IntegrationTests")]

var builder = WebApplication.CreateBuilder(args);


// NLog: Setup NLog for Dependency injection
 builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();




//configure service
var authenticationSettings = new AuthenticationSettings();

builder.Configuration.GetSection("Authentication").Bind(authenticationSettings);
builder.Services.AddSingleton(authenticationSettings);
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = "Bearer";
    option.DefaultScheme = "Bearer";
    option.DefaultChallengeScheme = "Bearer";

}).AddJwtBearer(cfg =>
{
    cfg.RequireHttpsMetadata = false;  // nie wymuszamy od klienta kontaktu tylko poprzez protokó³ https 
    cfg.SaveToken = true; // przekazujemy info ,¿e dany token powinien zostaæ zapisany po stronie serwera do celów autentykacji 
    cfg.TokenValidationParameters = new TokenValidationParameters // parametry walidacji - aby sprawdziæ czy dany token wys³any przez klienta 
                                                                  //jest zgodny z tym co wie serwer 
    {
        ValidIssuer = authenticationSettings.JwtIssuer,   //wydawca danego tokenu 
        ValidAudience = authenticationSettings.JwtIssuer,  //jakie podmioty mog¹ u¿ywaæ tego tokenu - jest to ta sama wartoœæ 
                                                           //poniewa¿ bedziemy generowaæ token w obrêbie naszej aplikacji i 
                                                           //tylko tacy klienci bêd¹ dopuszczeni do autentykacji 

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationSettings.JwtKey)),
        // klucz prywatny wygenerowany na podstawie wartoœci JwtKey która jest zapisana w pliku appsettings.json
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HasNationality", builder => builder.RequireClaim("Nationality"));
    options.AddPolicy("Atleast20", builder => builder.AddRequirements(new MinimumAgeRequirement(20)));
    options.AddPolicy("CreatedAtLeast2Restaurants", builder => builder.AddRequirements(new CreatedMultipleRestaurantsRequirement(2)));
});
builder.Services.AddScoped<IAuthorizationHandler, CreatedMultipleRestaurantsRequirementHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MinimumAgeRequirementHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceOperationRequirementHandler>();
builder.Services.AddControllers().AddFluentValidation();
//builder.Services.AddDbContext< RestaurantDbContext>();
builder.Services.AddScoped<RestaurantSeeder>();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IValidator<RegisterUserDto>, RegisterUserDtoValidator>();
builder.Services.AddScoped<IValidator<RestaurantQuery>, RestaurantQueryValidator>();
builder.Services.AddScoped<RequestTimeMiddleware>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c =>
{

c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "dotnetClaimAuthorization", Version = "v1" });
c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    In = ParameterLocation.Header,
    Description = "Please insert token",
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    BearerFormat = "JWT",
    Scheme = "bearer",

});
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference=new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer",
                }
            },
            new string[]{ }
        }
    });

});


builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEndClient", policybuilder =>
    policybuilder.AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins(builder.Configuration["AllowedOrigins"]));
});


builder.Services.AddDbContext<RestaurantDbContext>
    (options => options.UseSqlServer(builder.Configuration.GetConnectionString("RestaurantDbConnection")));




 
var app = builder.Build(); 

//configure 
var scope = app.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<RestaurantSeeder>();


app.UseResponseCaching();
app.UseStaticFiles();
app.UseCors("FrontAndClient");

seeder.Seed();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestTimeMiddleware>();
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API");
});

app.UseRouting();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();


