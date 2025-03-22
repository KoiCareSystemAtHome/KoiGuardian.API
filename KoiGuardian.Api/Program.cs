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
using KoiGuardian.DataAccess.MongoDB;
//using static KoiGuardian.Api.Services.IAuthServices;

var builder = WebApplication.CreateBuilder(args);

// Database 
builder.Services.AddDbContext<KoiGuardianDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("MyDB"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("KoiGuardian.DataAccess"));
});
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Apisettings:JwtOptions"));
builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<KoiGuardianDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IAccountServices, AccountService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped <IPackageServices, PackageServices>();
builder.Services.AddScoped<IParameterService, ParameterService>();
builder.Services.AddScoped<IPondReminderService, PondReminderService>();
builder.Services.AddScoped <IKoiMongoDb, KoiMongoDb>();
builder.Services.AddHttpClient<GhnService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddSingleton<IVnpayService, VnpayServices>();
builder.Services.AddScoped<IFishService, FishService>();
builder.Services.AddScoped<IPondServices, PondServices>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IParameterService, ParameterService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISaltCalculatorService, SaltCalculatorService>();
builder.Services.AddScoped<IFoodCalculatorService, FoodCalculatorService>();
builder.Services.AddScoped<ISymptomService, SymptomService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IKoiDiseaseService, KoiDiseaseService>();
builder.Services.AddScoped<IDiseaseService, DiseaseService>();
builder.Services.AddScoped<IReportServices, ReportServices>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
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
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(p => p.AddPolicy(MyAllowSpecificOrigins, builder =>
{
    builder.WithOrigins(
        "http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:3003", "exp://192.168.1.14:8081")
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials(); // Add this line to allow credentials

    // Other configurations...
}));
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    if (!app.Environment.IsDevelopment())
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KOI GUADIAN API");
        c.RoutePrefix = string.Empty;
    }
});

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
