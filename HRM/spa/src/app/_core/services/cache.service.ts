import { Injectable } from '@angular/core';
import { S_1_1_1_DirectoryMaintenanceService } from './system-maintenance/s_1_1_1_directory-maintenance.service';
import { S_1_1_2_ProgramMaintenanceService } from './system-maintenance/s_1_1_2_program-maintenance.service';
import { S_1_1_3_SystemLanguageSettingService } from './system-maintenance/s_1_1_3_system-language-setting.service';
import { S_1_1_4_DirectoryProgramLanguageSettingService } from './system-maintenance/s_1_1_4_directory-program-language-setting.service';

import { S_2_1_1_RoleSetting } from './basic-maintenance/s_2_1_1_role-setting.service';
import { S_2_1_2_AccountAuthorizationSettingService } from './basic-maintenance/s_2_1_2_account-authorization-setting.service';
import { S_2_1_3_CodeTypeMaintenanceService } from './basic-maintenance/s_2_1_3_code-type-maintenance.service';
import { S_2_1_4_CodeMaintenanceService } from './basic-maintenance/s_2_1_4-code-maintenance.service';
import { S_2_1_5_CodeLanguageService } from './basic-maintenance/s_2_1_5_code-language.service';
import { S_2_1_6_GradeMaintenanceService } from './basic-maintenance/s_2_1_6_grade-maintenance.service';
import { S_2_1_7_FactoryComparisonService } from './basic-maintenance/s_2_1_7_factory-comparison.service';

import { S_3_1_1_DepartmentMaintenanceService } from './organization-management/s_3_1_1_department-maintenance.service';
import { S_3_1_2_WorktypeHeadcountMaintenanceService } from './organization-management/s_3_1_2_work-type-headcount-maintenance.service';
import { S_3_1_3_OrganizationChart } from './organization-management/s_3_1_3_organization-chart.service';
import { S_3_1_4_DirectDepartmentSettingService } from './organization-management/s_3_1_4_direct-department-setting.service';
import { S_3_1_5_DirectWorkTypeAndSectionSettingService } from './organization-management/s_3_1_5_direct-work-type-and-section-setting.service';

import { S_4_1_1_EmployeeBasicInformationMaintenanceService } from './employee-maintenance/s_4_1_1_employee-basic-information-maintenance.service';
import { S_4_1_2_EmployeeEmergencyContactsService } from './employee-maintenance/s_4_1_2_employee-emergency-contacts.service';
import { S_4_1_4_DependentInformationService } from './employee-maintenance/s_4_1_4_dependent-information.service';
import { S_4_1_6_IdentificationCardHistoryService } from './employee-maintenance/s_4_1_6_identification-card-history.service';
import { S_4_1_7_IdentificationCardToEmployeeIdHistoryService } from './employee-maintenance/s_4_1_7_identification-card-to-employee-id-history.service';
import { S_4_1_8_EmployeeGroupSkillSettings } from './employee-maintenance/s_4_1_8_employee-group-skill-settings.service';
import { S_4_1_9_DocumentManagement } from './employee-maintenance/s_4_1_9_document-management.service';
import { S_4_1_10_Certifications } from './employee-maintenance/s_4_1_10_certifications.service';
import { S_4_1_11_UnpaidLeaveService } from './employee-maintenance/s_4_1_11_unpaid-leave.service';
import { S_4_1_12_ResignationManagementService } from './employee-maintenance/s_4_1_12_resignation-management.service';
import { S_4_1_13_ExitEmployeesBlacklistService } from './employee-maintenance/s_4_1_13_exit-employees-blacklist.service';
import { S_4_1_14_ContractTypeSetupService } from './employee-maintenance/s_4_1_14_contract-type-setup.service';
import { S_4_1_15_ContractManagementService } from './employee-maintenance/s_4_1_15_contract-management.service';
import { S_4_1_16_ContractManagementReportService } from './employee-maintenance/s_4_1_16_contract-management-report.service';
import { S_4_1_17_EmployeeTransferHistoryService } from './employee-maintenance/s_4_1_17_employee-transfer-history.service';
import { S_4_1_18_RehireEvaluationForFormerEmployeesService } from './employee-maintenance/s_4_1_18_rehire-evaluation-for-former-employees.service';
import { S_4_1_19_ExitEmployeeMasterFileHistoricalDataService } from './employee-maintenance/s_4_1_19_exit-employee-master-historical-data.service';
import { S_4_1_20_EmployeeTransferOperationOutboundService } from './employee-maintenance/s_4_1_20_employee-transfer-operation-outbound.service';
import { S_4_1_21_EmployeeTransferOperationInboundService } from './employee-maintenance/s_4_1_21_employee-transfer-operation-inbound.service';
import { S_4_2_1_EmployeeBasicInformationReportService } from './employee-maintenance/s_4_2_1_employee-basic-information-report.service';
import { S_4_2_2_EmergencyContactsReportService } from './employee-maintenance/s_4_2_2_emergency-contacts-report.service';

import { S_5_1_1_FactoryCalendar } from './attendance-maintenance/s_5_1_1_factory-calendar.service';
import { S_5_1_2_ShiftScheduleSettingService } from './attendance-maintenance/s_5_1_2_shift-schedule-setting.service';
import { S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService } from './attendance-maintenance/s_5_1_3_special-work-type-annual-leave-days-maintenance.service';
import { S_5_1_4_OvertimeParameterSettingService } from './attendance-maintenance/s_5_1_4_overtime-parameter-setting.service';
import { S_5_1_5_PregnancyAndMaternityDataMaintenanceService } from './attendance-maintenance/s_5_1_5_pregnancy-and-maternity-data-maintenance.service';
import { S_5_1_6_EmployeeLunchBreakTimeSettingService } from './attendance-maintenance/s_5_1_6_employee-lunch-break-time-setting.service';
import { S_5_1_7_MaintenanceOfAnnualLeaveEntitlementService } from './attendance-maintenance/s_5_1_7_maintenance-of-annual-leave-entitlement.service';
import { S_5_1_8_CardSwipingDataFormatSettingService } from './attendance-maintenance/s_5_1_8_card-swiping-data-format-setting.service';
import { S_5_1_9_MonthlyAttendanceSettingService } from './attendance-maintenance/s_5_1_9_monthly-attendance-setting.service';
import { S_5_1_10_ShiftManagementProgram } from './attendance-maintenance/s_5_1_10_shift-management-program.service';
import { S_5_1_11_Leave_Application_Maintenance } from './attendance-maintenance/s_5_1_11_leave-application-maintenance.service';
import { S_5_1_12_Overtime_Application_Maintenance } from './attendance-maintenance/s_5_1_12_overtime-application-maintenance.service';
import { S_5_1_15_AttendanceAbnormalityDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_15_attendance-abnormality-data-maintenance.service';
import { S_5_1_16_OvertimeTemporaryRecordMaintenanceService } from './attendance-maintenance/s_5_1_16_overtime_temporary_record_maintenance.service';
import { S_5_1_17_DailyAttendancePostingService } from './attendance-maintenance/s_5_1_17_daily-attendance-posting.service';
import { S_5_1_18_AttendanceChangeRecordMaintenanceService } from './attendance-maintenance/s_5_1_18_attendance-change-record-maintenance.service';
import { S_5_1_19_LeaveRecordModificationMaintenanceService } from './attendance-maintenance/s_5_1_19_leave-record-modification-maintenance.service';
import { S_5_1_20_OvertimeModificationMaintenanceService } from './attendance-maintenance/s_5_1_20_overtime-modification-maintenance.service';
import { S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService } from './attendance-maintenance/s_5_1_21_monthly-attendance-data-generation-active-employees.service';
import { S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService } from './attendance-maintenance/s_5_1_22_monthly-attendance-data-maintenance-active-employees.service';
import { S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService } from './attendance-maintenance/s_5_1_23_monthly-attendance-data-generation-resigned-employees.service';
import { S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService } from './attendance-maintenance/s_5_1_24_monthly-attendance-data-maintenance-resigned-employees.service';
import { S_5_1_25_LoanedMonthAttendanceDataGenerationService } from './attendance-maintenance/s_5_1_25_loaned-month-attendance-data-generation.service';
import { S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService } from './attendance-maintenance/s_5_1_26_female-employee-menstrual-leave-hours-maintenance.service';
import { S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService } from './attendance-maintenance/s_5_1_27_loaned-monthly-attendance-data-maintenance.service';
import { S_5_2_1_NewResignedEmployeeDataPrintingService } from './attendance-maintenance/s_5_2_1_new-resigned-employee-data-printing.service';
import { S_5_2_2_WorkingHoursReportService } from './attendance-maintenance/s_5_2_2_working-hours-report.service';
import { S_5_2_3_WeeklyWorkingHoursReportService } from './attendance-maintenance/s_5_2_3_weekly-working-hours-report.service';
import { S_5_2_4_EmployeeAttendanceDataSheetService } from './attendance-maintenance/s_5_2_4_employee-attendance-data-sheet.service';
import { S_5_2_5_DailySwipeCardAnomaliesList } from './attendance-maintenance/s_5_2_5_daily_swipe_card_anomalies_list.service';
import { S_5_2_6_DailyDinnerAllowanceList } from './attendance-maintenance/s_5_2_6_daily_dinner_allowance_list.service';
import { S_5_2_7_DailyUnregisteredOvertimeList } from './attendance-maintenance/s_5_2_7_daily_unregistered_overtime_list.service';
import { S_5_2_8_DailyNoNightShiftHoursList } from './attendance-maintenance/s_5_2_8_daily_no_night_shift_hours_list.service';
import { S_5_2_9_AbsenceDailyReportService } from './attendance-maintenance/s_5_2_9_absence-daily-report.service';
import { S_5_2_10_HRDailyReportService } from './attendance-maintenance/s_5_2_10_hr-daily-report.service';
import { S_5_2_11_monthlyEmployeeStatusChangesSheet_byDepartmentService } from './attendance-maintenance/s_5_2_11_monthly-employee-status-changes-sheet_by-department.service';
import { S_5_2_12_monthlyEmployeeStatusChangesSheet_byGenderService } from './attendance-maintenance/s_5_2_12_monthly-employee-status-changes-sheet_by-gender.service';
import { S_5_2_13_MonthlyEmployeeStatusChangesSheetByWorkTypeJobService } from './attendance-maintenance/s_5_2_13_monthly-employee-status-changes-sheet-by-work-type-job.service';
import { S_5_2_14_MonthlyEmployeeStatusChangesSheetByReasonForResignationService } from './attendance-maintenance/s_5_2_14_monthly-employee-status-changes-sheet-by-reason-for-resignation.service';
import { S_5_2_15_DepartmentMonthlyWorkingHoursReportService } from './attendance-maintenance/s_5_2_15_department-monthly-working-hours-report.service';
import { S_5_2_16_IndividualMonthlyWorkingHoursReportService } from './attendance-maintenance/s_5_2_16_individual-monthly-working-hours-report.service';
import { S_5_2_17_MonthlyWorkingHoursLeaveHoursReportService } from '@services/attendance-maintenance/s_5_2_17_monthly-working-hours-leave-hours-report.service';
import { S_5_2_18_EmployeeOvertimeDataSheetService } from './attendance-maintenance/s_5_2_18_employee-overtime-data-sheet.service';
import { S_5_2_19_OvertimeHoursReportService } from './attendance-maintenance/s_5_2_19_overtime-hours-report.service';
import { S_5_2_20_AnnualOvertimeHoursReportService } from "./attendance-maintenance/s_5_2_20_annual-overtime-hours-report.service";
import { S_5_2_21_EmployeeOvertimeExceedingHoursReportService } from "./attendance-maintenance/s_5_2_21_employee-overtime-exceeding-hours-report.service";
import { S_5_2_22_AnnualLeaveCalculationService } from "./attendance-maintenance/s_5_2_22_annual-leave-calculation.service";
import { S_5_2_23_FactoryResignationAnalysisReport } from './attendance-maintenance/s_5_2_23_factory_resignation_analysis_report.service';
import { S_5_2_24_ResignationAnnualLeaveWorktypeAnalysisReportService } from './attendance-maintenance/s_5_2_24_resignation-annual-leave-work-type-analysis-report.service';
import { S_5_2_25_MonthlyFactoryWorkingHoursReportService } from './attendance-maintenance/s_5_2_25_monthly-factory-working-hours-report.service';

import { S_6_1_1_Compulsory_Insurance_Data_MaintenanceService } from './compulsory-insurance-management/s_6_1_1_compulsory_insurance_data_maintenance.service';
import { S_6_1_2_ContributionRateSettingService } from './compulsory-insurance-management/s_6_1_2_contribution-rate-setting.service';
import { S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService } from './compulsory-insurance-management/s_6_1_3_apply_social_insurance_benefits_maintenance.service';
import { S_6_1_4_NewEmployeesCompulsoryInsurancePremium } from './compulsory-insurance-management/s_6_1_4_new_employees_compulsory_insurance_premium.service'
import { S_6_2_1_MonthlyCompulsoryInsuranceDetailedReportService } from './compulsory-insurance-management/s_6_2_1_monthly-compulsory-insurance-detailed-report.service';
import { S_6_2_2_MonthlyCompulsoryInsuranceSummaryReportService } from './compulsory-insurance-management/s_6_2_2_monthly-compulsory-insurance-summary-report.service';

import { S_7_1_1_SalaryItemAndAmountSettings } from './salary-maintenance/s_7_1_1_salary-item-and-amount-settings.service';
import { S_7_1_2_MonthlyExchangeRateSetting } from './salary-maintenance/s_7_1_2_monthly-exchange-rate-setting.service';
import { S_7_1_3_Leave_Salary_Calculation_MaintenanceService } from './salary-maintenance/s_7_1_3_leave_salary_calculation_maintenance.service';
import { S_7_1_4_Bank_Account_MaintenanceService } from './salary-maintenance/s_7_1_4_bank_account_maintenance.service';
import { S_7_1_5_PayslipDeliveryByEmailMaintenanceService } from './salary-maintenance/s_7_1_5_payslip-delivery-by-email-maintenance.service';
import { S_7_1_6_PersonalIncomeTaxNumberMaintenanceService } from './salary-maintenance/s_7_1_6_personal-income-tax-number-maintenance.service';
import { S_7_1_7_ListofChildcareSubsidyRecipientsMaintenanceService } from './salary-maintenance/s_7_1_7_list-of-child-care-subsidy-recipients-maintenance.service';
import { S_7_1_8_SapCostCenterSettingService } from './salary-maintenance/s_7_1_8_sap-cost-center-setting.service';
import { S_7_1_9_departmentToSapCostCenterMappingService } from "./salary-maintenance/s_7_1_9_department-to-sap-cost-center-mapping.service";
import { S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService } from './salary-maintenance/s_7_1_10_salary-item-to-accounting-code-mapping-maintenance.service';
import { S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService } from "./salary-maintenance/s_7_1_11_addition-deduction-item-to-accounting-code-mapping-maintenance.service";
import { S_7_1_12_AdditionDeductionItemAndAmountSettingsService } from "./salary-maintenance/s_7_1_12_addition-deduction-item-and-amount-settings.service";
import { S_7_1_13_IncomeTaxBracketSettingService } from './salary-maintenance/s_7_1_13_income-tax-bracket-setting.service';
import { S_7_1_14_IncomeTaxFreeSettingService } from './salary-maintenance/s_7_1_14_income-tax-free-setting.service';
import { S_7_1_15_ChildcareSubsidyGenerationService } from './salary-maintenance/s_7_1_15_childcare-subsidy-generation.service';
import { S_7_1_16_SalaryMasterFileService } from './salary-maintenance/s_7_1_16_salary-master-file.service';
import { S_7_1_17_MonthlySalaryMasterFileBackupQueryService } from './salary-maintenance/s_7_1_17_monthly-salary-master-file-backup-query.service';
import { S_7_1_18_salaryAdjustmentMaintenanceService } from './salary-maintenance/s_7_1_18_salary-adjustment-maintenance.service';
import { S_7_1_19_SalaryAdditionsAndDeductionsInputService } from './salary-maintenance/s_7_1_19_salary-additions-and-deductions-input.service';
import { S_7_1_20_NightShiftSubsidyMaintenanceService } from './salary-maintenance/s_7_1_20_night-shift-subsidy-maintenance.service';
import { S_7_1_21_MenstrualLeaveHoursAllowanceService } from "./salary-maintenance/s_7_1_21_menstrual-leave-hours-allowance.service";
import { S_7_1_22_MonthlySalaryGenerationService } from "./salary-maintenance/s_7_1_22_monthly-salary-generation.service";
import { S_7_1_23_MonthlySalaryGenerationExitedEmployees } from "./salary-maintenance/s_7_1_23_monthly-salary-generation-exited-employees.service";
import { S_7_1_24_MonthlySalaryMaintenanceService } from './salary-maintenance/s_7_1_24_monthly-salary-maintenance.service';
import { S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService } from './salary-maintenance/s_7_1_25_monthly-salary-maintenance-exited-employees.service';
import { S_7_1_26_FinSalaryCloseMaintenanceService } from './salary-maintenance/s-7-1-26-fin-salary-close-maintenance.service';
import { S_7_1_27_FinSalaryAttributionCategoryMaintenance } from './salary-maintenance/s_7_1_27_fin-salary-attribution-category-maintenance.service';
import { S_7_2_1_SalaryApprovalFormService } from './salary-report/s_7_2_1_salary-approval-form.service';
import { S_7_2_2_UtilityWorkersQualificationSeniorityPrinting } from './salary-report/s_7_2_2_utility-workers-qualification-seniority-printing.service';
import { S_7_2_4_MonthlySalarySummaryReportService } from './salary-report/s_7_2_4_monthly-salary-summary-report.service';
import { S_7_2_5_MonthlySalaryDetailReportService } from './salary-report/s_7_2_5_monthly-salary-detail-report.service';
import { S_7_2_6_MonthlyNonTransferSalaryPaymentReportService } from './salary-report/s_7_2_6_monthly-non-transfer-salary-payment-report.service';
import { S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReportService } from "./salary-report/s_7_2_7_monthly-salary-additions-deductions-summary-report.service";
import { S_7_2_9_SalarySummaryReportExitedEmployeeService } from "./salary-report/s_7_2_9_salary-summary-report-exited-employee.service";
import { S_7_2_10_SalarySummaryReportExitedEmployeeByDepartmentService } from "./salary-report/s_7_2_10_salary-summary-report-exited-employee-by-department.service";
import { S_7_2_12_MonthlySalaryTransferDetailsService } from './salary-report/s-7-2-12-monthly-salary-transfer-details.service';
import { S_7_2_13_MonthlySalaryTransferDetailsExitedEmployeeService } from './salary-report/s-7-2-13-monthly-salary-transfer-details-exited-employee.service';
import { S_7_2_14_taxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayService } from "./salary-report/s_7_2_14_tax-paying-employee-monthly-night-shift-extra-and-overtime-pay.service";
import { S_7_2_15_monthlyUnionDuesSummaryService } from "./salary-report/s_7_2_15_monthly-union-dues-summary.service";
import { S_7_2_16_DownloadPersonnelDataToExcelService } from './salary-report/s_7_2_16-download-personnel-data-to-excel.service';
import { S_7_2_17_MonthlyPersonalIncomeTaxAmountReportService } from './salary-report/s_7_2_17_monthly-personal-income-tax-amount-report.service';
import { S_7_2_18_AnnualIncomeTaxDetailReportService } from './salary-report/s_7_2_18-annual-income-tax-detail-report.service';
import { S_7_2_19_MonthlySalarySummaryReportForFinance } from './salary-report/s_7_2_19_monthly-salary-summary-report-for-finance.service';
import { S_7_2_20_MonthlySalarySummaryReportForTaxation } from './salary-report/s-7-2-20-monthly-salary-summary-report-for-taxation.service';
import { S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinanceService } from './salary-report/s_7_2_21_monthly-additions-and-deductions-summary-report-for-finance.service';
import { S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport } from './salary-report/s_7_2_22_monthly-additions-and-deductions-summary-report.service';
import { S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService } from './reward-and-penalty-maintenance/s_8_1_1_reward-and-penalty-reason-code-maintenance.service';
import { S_8_1_2_EmployeeRewardAndPenaltyRecordsService } from "./reward-and-penalty-maintenance/s_8_1_2_employee-reward-and-penalty-records.service";
import { S_8_2_1_EmployeeRewardAndPenaltyReportService } from './reward-and-penalty-report/s_8_2_1_employee-reward-and-penalty-report.service';

export interface IClearCache { clearParams: () => void }
@Injectable({
  providedIn: 'root'
})
export class CacheService {
  constructor(
    protected service_1_1_1: S_1_1_1_DirectoryMaintenanceService,
    protected service_1_1_2: S_1_1_2_ProgramMaintenanceService,
    protected service_1_1_3: S_1_1_3_SystemLanguageSettingService,
    protected service_1_1_4: S_1_1_4_DirectoryProgramLanguageSettingService,
    protected service_2_1_1: S_2_1_1_RoleSetting,
    protected service_2_1_2: S_2_1_2_AccountAuthorizationSettingService,
    protected service_2_1_3: S_2_1_3_CodeTypeMaintenanceService,
    protected service_2_1_4: S_2_1_4_CodeMaintenanceService,
    protected service_2_1_5: S_2_1_5_CodeLanguageService,
    protected service_2_1_6: S_2_1_6_GradeMaintenanceService,
    protected service_2_1_7: S_2_1_7_FactoryComparisonService,
    protected service_3_1_1: S_3_1_1_DepartmentMaintenanceService,
    protected service_3_1_2: S_3_1_2_WorktypeHeadcountMaintenanceService,
    protected service_3_1_3: S_3_1_3_OrganizationChart,
    protected service_3_1_4: S_3_1_4_DirectDepartmentSettingService,
    protected service_3_1_5: S_3_1_5_DirectWorkTypeAndSectionSettingService,
    protected service_4_1_1: S_4_1_1_EmployeeBasicInformationMaintenanceService,
    protected service_4_1_2: S_4_1_2_EmployeeEmergencyContactsService,
    protected service_4_1_6: S_4_1_6_IdentificationCardHistoryService,
    protected service_4_1_7: S_4_1_7_IdentificationCardToEmployeeIdHistoryService,
    protected service_4_1_8: S_4_1_8_EmployeeGroupSkillSettings,
    protected service_4_1_9: S_4_1_9_DocumentManagement,
    protected service_4_1_10: S_4_1_10_Certifications,
    protected service_4_1_11: S_4_1_11_UnpaidLeaveService,
    protected service_4_1_12: S_4_1_12_ResignationManagementService,
    protected service_4_1_13: S_4_1_13_ExitEmployeesBlacklistService,
    protected service_4_1_14: S_4_1_14_ContractTypeSetupService,
    protected service_4_1_15: S_4_1_15_ContractManagementService,
    protected service_4_1_16: S_4_1_16_ContractManagementReportService,
    protected service_4_1_17: S_4_1_17_EmployeeTransferHistoryService,
    protected service_4_1_18: S_4_1_18_RehireEvaluationForFormerEmployeesService,
    protected service_4_1_19: S_4_1_19_ExitEmployeeMasterFileHistoricalDataService,
    protected service_4_1_20: S_4_1_20_EmployeeTransferOperationOutboundService,
    protected service_4_1_21: S_4_1_21_EmployeeTransferOperationInboundService,
    protected service_4_2_1: S_4_2_1_EmployeeBasicInformationReportService,
    protected service_4_2_2: S_4_2_2_EmergencyContactsReportService,
    protected service_5_1_1: S_5_1_1_FactoryCalendar,
    protected service_5_1_2: S_5_1_2_ShiftScheduleSettingService,
    protected service_5_1_3: S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService,
    protected service_5_1_4: S_5_1_4_OvertimeParameterSettingService,
    protected service_5_1_5: S_5_1_5_PregnancyAndMaternityDataMaintenanceService,
    protected service_5_1_6: S_5_1_6_EmployeeLunchBreakTimeSettingService,
    protected service_5_1_7: S_5_1_7_MaintenanceOfAnnualLeaveEntitlementService,
    protected service_5_1_8: S_5_1_8_CardSwipingDataFormatSettingService,
    protected service_5_1_9: S_5_1_9_MonthlyAttendanceSettingService,
    protected service_5_1_10: S_5_1_10_ShiftManagementProgram,
    protected service_5_1_11: S_5_1_11_Leave_Application_Maintenance,
    protected service_5_1_12: S_5_1_12_Overtime_Application_Maintenance,
    protected service_5_1_15: S_5_1_15_AttendanceAbnormalityDataMaintenanceService,
    protected service_5_1_16: S_5_1_16_OvertimeTemporaryRecordMaintenanceService,
    protected service_5_1_17: S_5_1_17_DailyAttendancePostingService,
    protected service_5_1_18: S_5_1_18_AttendanceChangeRecordMaintenanceService,
    protected service_5_1_19: S_5_1_19_LeaveRecordModificationMaintenanceService,
    protected service_5_1_20: S_5_1_20_OvertimeModificationMaintenanceService,
    protected service_5_1_21: S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService,
    protected service_5_1_22: S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService,
    protected service_5_1_23: S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService,
    protected service_5_1_24: S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService,
    protected service_5_1_25: S_5_1_25_LoanedMonthAttendanceDataGenerationService,
    protected service_5_1_26: S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService,
    protected service_5_1_27: S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService,
    protected service_5_2_1: S_5_2_1_NewResignedEmployeeDataPrintingService,
    protected service_5_2_2: S_5_2_2_WorkingHoursReportService,
    protected service_5_2_3: S_5_2_3_WeeklyWorkingHoursReportService,
    protected service_5_2_4: S_5_2_4_EmployeeAttendanceDataSheetService,
    protected service_5_2_5: S_5_2_5_DailySwipeCardAnomaliesList,
    protected service_5_2_6: S_5_2_6_DailyDinnerAllowanceList,
    protected service_5_2_7: S_5_2_7_DailyUnregisteredOvertimeList,
    protected service_5_2_8: S_5_2_8_DailyNoNightShiftHoursList,
    protected service_5_2_9: S_5_2_9_AbsenceDailyReportService,
    protected service_5_2_10: S_5_2_10_HRDailyReportService,
    protected service_5_2_11: S_5_2_11_monthlyEmployeeStatusChangesSheet_byDepartmentService,
    protected service_5_2_12: S_5_2_12_monthlyEmployeeStatusChangesSheet_byGenderService,
    protected service_5_2_13: S_5_2_13_MonthlyEmployeeStatusChangesSheetByWorkTypeJobService,
    protected service_5_2_14: S_5_2_14_MonthlyEmployeeStatusChangesSheetByReasonForResignationService,
    protected service_5_2_15: S_5_2_15_DepartmentMonthlyWorkingHoursReportService,
    protected service_5_2_16: S_5_2_16_IndividualMonthlyWorkingHoursReportService,
    protected service_5_2_17: S_5_2_17_MonthlyWorkingHoursLeaveHoursReportService,
    protected service_5_2_18: S_5_2_18_EmployeeOvertimeDataSheetService,
    protected service_5_2_19: S_5_2_19_OvertimeHoursReportService,
    protected service_5_2_20: S_5_2_20_AnnualOvertimeHoursReportService,
    protected service_5_2_21: S_5_2_21_EmployeeOvertimeExceedingHoursReportService,
    protected service_5_2_22: S_5_2_22_AnnualLeaveCalculationService,
    protected service_5_2_23: S_5_2_23_FactoryResignationAnalysisReport,
    protected service_5_2_24: S_5_2_24_ResignationAnnualLeaveWorktypeAnalysisReportService,
    protected service_5_2_25: S_5_2_25_MonthlyFactoryWorkingHoursReportService,
    protected service_6_1_1: S_6_1_1_Compulsory_Insurance_Data_MaintenanceService,
    protected service_6_1_2: S_6_1_2_ContributionRateSettingService,
    protected service_6_1_3: S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService,
    protected service_6_1_4: S_6_1_4_NewEmployeesCompulsoryInsurancePremium,
    protected service_6_2_1: S_6_2_1_MonthlyCompulsoryInsuranceDetailedReportService,
    protected service_6_2_2: S_6_2_2_MonthlyCompulsoryInsuranceSummaryReportService,
    protected service_7_1: S_7_1_1_SalaryItemAndAmountSettings,
    protected service_7_2: S_7_1_2_MonthlyExchangeRateSetting,
    protected service_7_3: S_7_1_3_Leave_Salary_Calculation_MaintenanceService,
    protected service_7_4: S_7_1_4_Bank_Account_MaintenanceService,
    protected service_7_5: S_7_1_5_PayslipDeliveryByEmailMaintenanceService,
    protected service_7_6: S_7_1_6_PersonalIncomeTaxNumberMaintenanceService,
    protected service_7_7: S_7_1_7_ListofChildcareSubsidyRecipientsMaintenanceService,
    protected service_7_8: S_7_1_8_SapCostCenterSettingService,
    protected service_7_9: S_7_1_9_departmentToSapCostCenterMappingService,
    protected service_7_10: S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService,
    protected service_7_11: S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService,
    protected service_7_12: S_7_1_12_AdditionDeductionItemAndAmountSettingsService,
    protected service_7_13: S_7_1_13_IncomeTaxBracketSettingService,
    protected service_7_14: S_7_1_14_IncomeTaxFreeSettingService,
    protected service_7_15: S_7_1_15_ChildcareSubsidyGenerationService,
    protected service_7_16: S_7_1_16_SalaryMasterFileService,
    protected service_7_17: S_7_1_17_MonthlySalaryMasterFileBackupQueryService,
    protected service_7_18: S_7_1_18_salaryAdjustmentMaintenanceService,
    protected service_7_19: S_7_1_19_SalaryAdditionsAndDeductionsInputService,
    protected service_7_20: S_7_1_20_NightShiftSubsidyMaintenanceService,
    protected service_7_21: S_7_1_21_MenstrualLeaveHoursAllowanceService,
    protected service_7_22: S_7_1_22_MonthlySalaryGenerationService,
    protected service_7_23: S_7_1_23_MonthlySalaryGenerationExitedEmployees,
    protected service_7_24: S_7_1_24_MonthlySalaryMaintenanceService,
    protected service_7_25: S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService,
    protected service_7_26: S_7_1_26_FinSalaryCloseMaintenanceService,
    protected service_7_27: S_7_1_27_FinSalaryAttributionCategoryMaintenance,
    protected service_7_2_1: S_7_2_1_SalaryApprovalFormService,
    protected service_7_2_2: S_7_2_2_UtilityWorkersQualificationSeniorityPrinting,
    protected service_7_2_4: S_7_2_4_MonthlySalarySummaryReportService,
    protected service_7_2_5: S_7_2_5_MonthlySalaryDetailReportService,
    protected service_7_2_6: S_7_2_6_MonthlyNonTransferSalaryPaymentReportService,
    protected service_7_2_7: S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReportService,
    protected service_7_2_9: S_7_2_9_SalarySummaryReportExitedEmployeeService,
    protected service_7_2_10: S_7_2_10_SalarySummaryReportExitedEmployeeByDepartmentService,
    protected service_7_2_12: S_7_2_12_MonthlySalaryTransferDetailsService,
    protected service_7_2_13: S_7_2_13_MonthlySalaryTransferDetailsExitedEmployeeService,
    protected service_7_2_14: S_7_2_14_taxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayService,
    protected service_7_2_15: S_7_2_15_monthlyUnionDuesSummaryService,
    protected service_7_2_16: S_7_2_16_DownloadPersonnelDataToExcelService,
    protected service_7_2_17: S_7_2_17_MonthlyPersonalIncomeTaxAmountReportService,
    protected service_7_2_18: S_7_2_18_AnnualIncomeTaxDetailReportService,
    protected service_7_2_19: S_7_2_19_MonthlySalarySummaryReportForFinance,
    protected service_7_2_20: S_7_2_20_MonthlySalarySummaryReportForTaxation,
    protected service_7_2_21: S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinanceService,
    protected service_7_2_22: S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport,
    protected service_8_1_1: S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService,
    protected service_8_1_2: S_8_1_2_EmployeeRewardAndPenaltyRecordsService,
    protected service_8_2_1: S_8_2_1_EmployeeRewardAndPenaltyReportService,
  ) { }
  clearCache = () => Object.values(this).forEach(x => { if ('clearParams' in x && x['clearParams'] instanceof Function) (x as IClearCache).clearParams() })
}
