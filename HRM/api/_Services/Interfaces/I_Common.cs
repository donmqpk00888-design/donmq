
using API.Dtos.Auth;
using API.DTOs;

namespace API._Services.Interfaces;

[DependencyInjection(ServiceLifetime.Scoped)]
public interface I_Common
{
    Task<SystemInfo> GetSystemInfo(string username);
    Task<bool?> GetPasswordReset(string username);
    // code = 2 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListFactoryMain(string language);
    // code = 4 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string language);
    // code = 41 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string language);
    // code = 40 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListAttendanceOrLeave(string language);
    // 2.11.Query_Department_List
    Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
    // code = 43 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListReasonCode(string language);
    // code = 45 (2.2.Query_HRMS_Basic_Code_List )
    Task<List<KeyValuePair<string, string>>> GetListSalaryItems(string language);
    // 2.3.Query_HRMS_Basic_Code_Char1
    Task<List<KeyValuePair<string, string>>> GetListBasicCodeListChar1(string language, string typeSeq, int kind, string inputChar);
    // 2.8.Queryt_Factory_AddList新增模式廠別清單-by帳號權限
    Task<List<KeyValuePair<string, string>>> GetListAccountAdd(string userName, string language);
    Task<List<EmployeeCommonInfo>> GetListEmployeeAdd(string factory, string language);
}
