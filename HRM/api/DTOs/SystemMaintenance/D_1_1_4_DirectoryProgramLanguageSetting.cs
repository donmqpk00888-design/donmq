namespace API.DTOs.SystemMaintenance
{
    public class DirectoryProgramLanguageSetting_Param
    {
        public string Kind { get; set; }
        public string Code { get; set; }
        public string Code_Name { get; set; }
        public string Name { get; set; }
    }
    public class DirectoryProgramLanguageSetting_Data : DirectoryProgramLanguageSetting_Param
    {
        public List<Language> Langs { get; set; }
    }

    public class Language
    {
        public string Lang_Code { get; set; }
        public string Lang_Name { get; set; }
    }

}