using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Mappers.Implementation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceImplementation;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddMediatR(typeof(Program));

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
