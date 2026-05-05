using Xunit;
using g19_sep490_ealds.Server.Utils;

namespace g19_sep490_ealds.Tests.Utils
{
    public class DepreciationFormulaTests
    {
        [Fact]
        public void CalculateStraightLine_UsefulLifeIsZero_ReturnsZero()
        {
            // Boundary Case 1: Chia cho 0 (Divide by Zero)
            // Cố tình truyền vào thời gian sử dụng = 0 tháng
            decimal cost = 1000000m;
            decimal salvage = 0m;
            int usefulLifeMonths = 0;

            var result = DepreciationFormula.CalculateStraightLine(cost, salvage, usefulLifeMonths);

            // Kỳ vọng hàm bắt được lỗi và trả về 0 thay vì ném ra lỗi DivideByZeroException
            Assert.Equal(0m, result);
        }

        [Fact]
        public void CalculateStraightLine_UsefulLifeIsNegative_ReturnsZero()
        {
            // Mở rộng Boundary Case 1: Thời gian sử dụng bị âm (do lỗi data)
            var result = DepreciationFormula.CalculateStraightLine(1000000m, 0m, -5);
            Assert.Equal(0m, result);
        }

        [Fact]
        public void ClampFinalPeriodAmount_CalculatedAmountExceedsRemaining_ClampsToMaxAllowed()
        {
            // Boundary Case 2: Giá trị chạm đáy vào tháng thứ N+1
            // Giả sử tài sản đang còn giá trị đầu kỳ là 50.000 VNĐ
            // Giá trị thu hồi (không được khấu hao thêm) là 10.000 VNĐ
            // Vậy giá trị TỐI ĐA ĐƯỢC PHÉP khấu hao là 40.000 VNĐ.
            // Nhưng công thức tính lại đòi khấu hao 50.000 VNĐ (lớn hơn mức trần).
            
            decimal openingValue = 50000m;
            decimal salvage = 10000m;
            decimal calculatedAmount = 50000m; // Công thức cố tình đòi trừ 50k

            var result = DepreciationFormula.ClampFinalPeriodAmount(openingValue, salvage, calculatedAmount);
            
            // Kỳ vọng hệ thống tự động ép (clamp) mức khấu hao xuống còn 40.000 VNĐ
            // Để đảm bảo RemainingValue cuối cùng sẽ = Giá trị thu hồi (10.000 VNĐ)
            Assert.Equal(40000m, result);
        }

        [Fact]
        public void ClampFinalPeriodAmount_OpeningValueBelowSalvage_ReturnsZero()
        {
            // Mở rộng Boundary Case 2: Nếu lỡ may giá trị đã chạm đáy từ kỳ trước
            // OpeningValue = 5k < Salvage = 10k
            var result = DepreciationFormula.ClampFinalPeriodAmount(5000m, 10000m, 50000m);
            
            // Kỳ vọng ko được phép trừ thêm đồng nào nữa
            Assert.Equal(0m, result);
        }
    }
}
