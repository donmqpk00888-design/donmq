
namespace API.DTOs.Kanban
{
    public class ProductionEfficiencyDTO
    {
        public string Category { get; set; }
        public ProductionEfficiencyDetail[] Detail { get; set; }
    }
    public class ProductionEfficiencyDetail
    {
        public string Shift { get; set; }
        public string Mdat { get; set; }
        public string NameEN { get; set; }
        public string Rmodel { get; set; }
        public decimal Qty { get; set; }
    }
    public class ProductionEfficiencyParam
    {
        public string ProductionDate { get; set; }
        public string Class { get; set; }
    }
}