using KoiGuardian.Api.Services;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Commons;
using Microsoft.EntityFrameworkCore;
using KoiGuardian.DataAccess.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using KoiGuardian.Api.Extensions;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Database 
builder.Services.AddDbContext<KoiGuardianDbContext>(
                option =>
                {
                    option.UseSqlServer(builder.Configuration.GetConnectionString("MyDB"),
                        b => b.MigrationsAssembly("KoiGuardian.DataAccess"));
                });
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Apisettings:JwtOptions"));
builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<KoiGuardianDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.

builder.Services.AddScoped<IAuthServices, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter string as follow : Bearer Generated-JWT-Token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    }, new string[]{}
                    }
                });
}
            );

builder.AppAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
