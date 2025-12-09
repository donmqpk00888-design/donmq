using API._Services.Interfaces.Report;
using API.Data;
namespace API._Services.Services.Report
{
    public class S_6_1_ItemNumberQualityReportPrinting : BaseServices, I_6_1_ItemNumberQualityReportPrinting
    {
        public S_6_1_ItemNumberQualityReportPrinting(DBContext dbContext) : base(dbContext)
        {
        }
    }
}