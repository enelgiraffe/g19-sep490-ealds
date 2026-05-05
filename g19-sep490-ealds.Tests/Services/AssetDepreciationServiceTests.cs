using System;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests.Services
{
    public class AssetDepreciationServiceTests
    {
        [Fact]
        public async Task RunMonthlyDepreciation_QuartzMisfire_UsesScheduledTimeInsteadOfRealTime()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EaldsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            await using var context = new EaldsDbContext(options);
            
            // Tạo chính sách khấu hao
            var policy = new DepreciationPolicy
            {
                PolicyId = 1,
                Name = "Khấu hao test",
                Method = 1,
                UsefullLifeMonths = 12,
                SalvageValue = 0,
                IsActive = true
            };
            context.DepreciationPolicies.Add(policy);

            // Tạo tài sản
            var assetInstance = new AssetInstance
            {
                AssetInstanceId = 1,
                InstanceCode = "TEST-01",
                OriginalPrice = 1200000m,
                CurrentValue = 1200000m,
                DepreciationPolicyId = 1,
                InUseDate = new DateOnly(2026, 1, 15), // Bắt đầu sử dụng từ tháng 1
                Status = (int)AssetStatus.InUse
            };
            context.AssetInstances.Add(assetInstance);

            // Cần có AssetCapitalization để service chịu chạy
            context.AssetCapitalizations.Add(new AssetCapitalization
            {
                Id = 1,
                AssetInstanceId = 1,
                CapitalizedDate = new DateTime(2026, 1, 15)
            });

            await context.SaveChangesAsync();

            var service = new AssetDepreciationService(context);

            // Giả lập Quartz Misfire Boundary Case:
            // Lẽ ra Job phải chạy lúc 23:59:59 ngày 31/01/2026 (Giờ UTC).
            // Nhưng Server đang chạy đoạn code này thì DateTime.UtcNow thực tế có thể đang là ngày 01/02 hoặc xa hơn.
            // Bằng cách truyền ScheduledFireTimeUtc, hệ thống phải hiểu là tính cho kỳ Tháng 1, không được lấy tháng hiện tại!
            // Giả lập ScheduledFireTimeUtc là 16:59:00 ngày 31/01/2026 UTC
            // Khi cộng VietnamOffset (+7 tiếng), sẽ ra 23:59:00 ngày 31/01/2026 Local.
            DateTime scheduledFireTimeUtc = new DateTime(2026, 1, 31, 16, 59, 0, DateTimeKind.Utc);

            // Act
            // Lúc Act, cho dù DateTime.UtcNow ở bên trong Service có đang là tháng 2 (nếu gọi hàm overload cũ)
            // nhưng nhờ ta truyền scheduledFireTimeUtc, nó sẽ lấy scheduledFireTimeUtc làm chuẩn.
            await service.RunMonthlyDepreciation(scheduledFireTimeUtc);

            // Assert
            var record = await context.DepreciationRecords.FirstOrDefaultAsync();
            
            Assert.NotNull(record);
            
            // Kỳ kế toán (Period) phải là mùng 1 tháng 1 (không được lệch sang tháng 2)
            Assert.Equal(new DateOnly(2026, 1, 1), record.Period);
            
            // Mức khấu hao = (1,200,000 - 0) / 12 = 100,000
            Assert.Equal(100000m, record.DepreciationAmount);
        }
    }
}
