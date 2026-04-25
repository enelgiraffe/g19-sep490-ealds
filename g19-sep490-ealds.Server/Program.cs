using System.Text;
using g19_sep490_ealds.Server.Configuration;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Mappers.Implementation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Services.ServiceImplementation;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<EaldsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<GoogleCloudStorageSettings>(
    builder.Configuration.GetSection(GoogleCloudStorageSettings.SectionName));
var gcsBucket = builder.Configuration["GoogleCloudStorage:BucketName"];
if (!string.IsNullOrWhiteSpace(gcsBucket))
    builder.Services.AddSingleton<IFileStorageService, GoogleCloudFileStorageService>();
else
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5173", "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//DI
builder.Services.AddScoped<IMaintenanceTemplateMapper, MaintenanceTemplateMapper>();
builder.Services.AddScoped<IMaintenanceTemplateService, MaintenanceTemplateService>();
builder.Services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();
builder.Services.AddScoped<IMaintenanceScheduleMapper, MaintenanceScheduleMapper>();
builder.Services.AddScoped<IMaintenanceRecordService, MaintenanceRecordService>();
builder.Services.AddScoped<IRepairRecordService, RepairRecordService>();
builder.Services.AddScoped<IMaintenanceRecordMapper, MaintenanceRecordMapper>();
builder.Services.AddScoped<IMaintenanceTaskService, MaintenanceTaskService>();
builder.Services.AddScoped<IInventoryNotificationService, InventoryNotificationService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAssetRequestNotificationService, AssetRequestNotificationService>();
builder.Services.AddScoped<ITransferRequestService, TransferRequestService>();
builder.Services.AddScoped<IAllocationRequestService, AllocationRequestService>();
builder.Services.AddScoped<IHandoverRequestService, HandoverRequestService>();
builder.Services.AddScoped<IAllocationsService, AllocationsService>();
builder.Services.AddScoped<IBudgetAllocationService, BudgetAllocationService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();

// Asset capitalization
builder.Services.AddScoped<IAssetCapitalizationMapper, AssetCapitalizationMapper>();
builder.Services.AddScoped<IAssetCapitalizationService, AssetCapitalizationService>();
builder.Services.AddScoped<IAssetDepreciationService, AssetDepreciationService>();
builder.Services.AddScoped<IAssetRevaluationService, AssetRevaluationService>();

// Asset type (controller/service exists)
builder.Services.AddScoped<IAssetTypeMapper, AssetTypeMapper>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();

builder.Services.AddMediatR(typeof(Program));

//Quarzt chạy job
builder.Services.AddQuartz(q =>
{
    //Job cho Maintenance
    var maintenanceJobKey = new JobKey("MaintenanceTaskJob");
    q.AddJob<MaintenanceTaskJobs>(opts => opts.WithIdentity(maintenanceJobKey));
    q.AddTrigger(opts => opts
        .ForJob(maintenanceJobKey)
        .WithIdentity("MaintenanceTaskJob-trigger")
        //.WithCronSchedule("0 0 1 * * ?")); // 1 ngày 1 lần
        .WithCronSchedule("0 * * * * ?")); //test 1 phút

    var inventoryNotifyJobKey = new JobKey("InventoryScheduledCheckNotificationJob");
    q.AddJob<InventoryScheduledCheckNotificationJob>(opts => opts.WithIdentity(inventoryNotifyJobKey));
    q.AddTrigger(opts => opts
        .ForJob(inventoryNotifyJobKey)
        .WithIdentity("InventoryScheduledCheckNotificationJob-trigger")
        .WithCronSchedule("0 */15 * * * ?")); // 15 phút/lần — nhắc trong khung Đến lịch (tối đa 1 TB/ngày/user/phiên)
});

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("DepreciationJob");

    q.AddJob<DepreciationJobs>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DepreciationJob-trigger")
        .WithCronSchedule("0 59 23 L * ?")); // 23:59 ngày cuối tháng
});
//dky host service cho Quarzt
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Keep local/dev databases aligned with current EF model (e.g., new nullable columns like GoodsReceiptLineId).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EaldsDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigration");
        logger.LogError(ex, "Failed to apply database migrations at startup.");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
