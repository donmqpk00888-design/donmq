using API.DTOs.Kanban;

namespace API._Services.Interfaces.Kanban
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_5_1_ProductionEfficiency
    {
        Task<List<ProductionEfficiencyDTO>> GetData(ProductionEfficiencyParam param);
    }
}