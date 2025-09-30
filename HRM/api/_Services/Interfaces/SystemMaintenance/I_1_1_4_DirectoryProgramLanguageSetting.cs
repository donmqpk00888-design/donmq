using API.DTOs.SystemMaintenance;

namespace API._Services.Interfaces.SystemMaintenance
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_1_1_4_DirectoryProgramLanguageSetting
    {
        Task<PaginationUtility<DirectoryProgramLanguageSetting_Data>> GetData(PaginationParam pagination, DirectoryProgramLanguageSetting_Param param);
        Task<List<KeyValuePair<string, string>>> GetLanguage();
        Task<List<KeyValuePair<string, string>>> GetCodeProgram();
        Task<List<KeyValuePair<string, string>>> GetCodeDirectory();
        Task<DirectoryProgramLanguageSetting_Data> GetDetail(string kind, string code);
        Task<OperationResult> Add(DirectoryProgramLanguageSetting_Data model, string userName);
        Task<OperationResult> Update(DirectoryProgramLanguageSetting_Data model, string userName);
        Task<OperationResult> Delete(string kind, string code);
    }
}