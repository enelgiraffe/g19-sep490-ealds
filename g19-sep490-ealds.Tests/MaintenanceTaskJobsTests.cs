using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class MaintenanceTaskJobsTests
{
    private static EaldsDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EaldsDbContext(options);
    }

    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<EaldsDbContext>(options => options.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    private static IJobExecutionContext BuildJobContext()
    {
        var mock = new Mock<IJobExecutionContext>();
        mock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static MaintenanceTaskJobs BuildJob(IServiceScopeFactory scopeFactory)
    {
        var logger = new Mock<ILogger<MaintenanceTaskJobs>>();
        return new MaintenanceTaskJobs(logger.Object, scopeFactory);
    }

    private static void SeedBaseAsset(EaldsDbContext db, int userId = 1, int assetId = 10, int instanceId = 100)
    {
        db.Users.Add(new User
        {
            UserId = userId,
            Email = $"u{userId}@test.local",
            Password = "123",
            Status = 1
        });

        db.Assets.Add(new Asset
        {
            AssetId = assetId,
            AssetTypeId = 1,
            Code = $"AS-{assetId}",
            Name = $"Asset {assetId}",
            Unit = "pcs",
            Status = 1,
            CreatedBy = userId
        });

        db.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = instanceId,
            AssetId = assetId,
            WarehouseId = 1,
            InstanceCode = $"INS-{instanceId}",
            Status = 1,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 1000,
            CurrentValue = 900
        });
    }

    [Fact]
    public async Task Execute_SendsOnceAndAvoidsDuplicateInSameDay()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateDbContext(dbName);
        SeedBaseAsset(db, userId: 1, assetId: 10, instanceId: 100);

        db.Roles.Add(new Role { RoleId = 4, Name = "Dept Head", Code = "DEPT_HEAD", CreateDate = DateTime.UtcNow });
        db.Users.Add(new User { UserId = 2, Email = "head@test.local", Password = "123", Status = 1 });
        db.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 2,
            DepartmentId = 10,
            Name = "Head",
            Code = "EMP-1",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        db.UserRoles.Add(new UserRole { UserId = 2, RoleId = 4 });
        db.AssetLocations.Add(new AssetLocation
        {
            LocationId = 1,
            AssetInstanceId = 100,
            DepartmentId = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsCurrent = true
        });

        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            ScheduleId = 500,
            AssetInstanceId = 100,
            TemplateId = 1,
            ScheduleType = 2,
            StartDate = DateTime.UtcNow.AddDays(-3),
            NextDueDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1),
            IsActive = true,
            CreateBy = 1,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = BuildProvider(dbName);
        var job = BuildJob(provider.GetRequiredService<IServiceScopeFactory>());
        var context = BuildJobContext();

        await job.Execute(context);
        await job.Execute(context);

        await using var verifyDb = CreateDbContext(dbName);
        var notifications = await verifyDb.Notifications.Where(n => n.UserId == 2 && n.Title == "Nhắc lịch bảo dưỡng: LS-500").ToListAsync();
        Assert.Single(notifications);
    }

    [Fact]
    public async Task Execute_DeactivatesExpiredSchedules()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateDbContext(dbName);
        SeedBaseAsset(db);

        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            ScheduleId = 501,
            AssetInstanceId = 100,
            TemplateId = 1,
            ScheduleType = 2,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddHours(7).AddDays(-1),
            IsActive = true,
            CreateBy = 1,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = BuildProvider(dbName);
        var job = BuildJob(provider.GetRequiredService<IServiceScopeFactory>());

        await job.Execute(BuildJobContext());

        await using var verifyDb = CreateDbContext(dbName);
        var schedule = await verifyDb.MaintenanceSchedules.SingleAsync(x => x.ScheduleId == 501);
        Assert.False(schedule.IsActive);
    }

    [Fact]
    public async Task Execute_NoDepartmentHeadRole_DoesNotSendReminder()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateDbContext(dbName);
        SeedBaseAsset(db);
        db.Users.Add(new User { UserId = 2, Email = "staff@test.local", Password = "123", Status = 1 });
        db.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 2,
            DepartmentId = 10,
            Name = "Staff",
            Code = "EMP-2",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        db.UserRoles.Add(new UserRole { UserId = 2, RoleId = 99 });
        db.AssetLocations.Add(new AssetLocation
        {
            LocationId = 1,
            AssetInstanceId = 100,
            DepartmentId = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsCurrent = true
        });
        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            ScheduleId = 502,
            AssetInstanceId = 100,
            TemplateId = 1,
            ScheduleType = 2,
            StartDate = DateTime.UtcNow.AddDays(-3),
            NextDueDate = DateTime.UtcNow.AddHours(7).Date,
            IsActive = true,
            CreateBy = 1,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = BuildProvider(dbName);
        var job = BuildJob(provider.GetRequiredService<IServiceScopeFactory>());

        await job.Execute(BuildJobContext());

        await using var verifyDb = CreateDbContext(dbName);
        Assert.Empty(verifyDb.Notifications);
    }

    [Fact]
    public async Task Execute_RecipientNotFoundInUsers_SkipsNotification()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateDbContext(dbName);
        SeedBaseAsset(db);
        db.Roles.Add(new Role { RoleId = 4, Name = "Dept Head", Code = "DEPT_HEAD", CreateDate = DateTime.UtcNow });
        db.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 999,
            DepartmentId = 10,
            Name = "Ghost",
            Code = "EMP-999",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        db.UserRoles.Add(new UserRole { UserId = 999, RoleId = 4 });
        db.AssetLocations.Add(new AssetLocation
        {
            LocationId = 1,
            AssetInstanceId = 100,
            DepartmentId = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsCurrent = true
        });
        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            ScheduleId = 503,
            AssetInstanceId = 100,
            TemplateId = 1,
            ScheduleType = 2,
            StartDate = DateTime.UtcNow.AddDays(-1),
            NextDueDate = DateTime.UtcNow.AddHours(7).Date,
            IsActive = true,
            CreateBy = 1,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = BuildProvider(dbName);
        var job = BuildJob(provider.GetRequiredService<IServiceScopeFactory>());

        await job.Execute(BuildJobContext());

        await using var verifyDb = CreateDbContext(dbName);
        Assert.Empty(verifyDb.Notifications);
    }
}
