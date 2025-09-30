import { Pagination } from "@utilities/pagination-utility";

export interface DirectoryProgramLanguageSetting_Param {
  kind: string;
  code: string;
  code_Name: string;
  name: string;
}
export interface DirectoryProgramLanguageSetting_Data extends DirectoryProgramLanguageSetting_Param {
  langs: Language[];
}
export interface DirectoryProgramLanguageSetting_Memory {
  pagination: Pagination;
  selectedData: DirectoryProgramLanguageSetting_Data
  param: DirectoryProgramLanguageSetting_Param
  data: DirectoryProgramLanguageSetting_Data[]
}
export interface Language {
  lang_Code: string;
  lang_Name: string;
}


