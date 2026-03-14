using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Mappers.Implementation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Services.ServiceImplementation;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<EALDSDbcontext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//DI
builder.Services.AddScoped<IAssetCapitalizationService, AssetCapitalizationService>();
builder.Services.AddScoped<IAssetCapitalizationMapper, AssetCapitalizationMapper>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IAssetTypeMapper, AssetTypeMapper>();
builder.Services.AddScoped<IMaintenanceTemplateMapper, MaintenanceTemplateMapper>();
builder.Services.AddScoped<IMaintenanceTemplateService, MaintenanceTemplateService>();
builder.Services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();
builder.Services.AddScoped<IMaintenanceScheduleMapper, MaintenanceScheduleMapper>();

builder.Services.AddMediatR(typeof(Program));

//Quarzt chạy job
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("MaintenanceTaskJob");

    q.AddJob<MaintenanceTaskJobs>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("MaintenanceTaskJob-trigger")
        .WithCronSchedule("0 * * * * ?")); //test 1 phút
});
//dky host service cho Quarzt
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
