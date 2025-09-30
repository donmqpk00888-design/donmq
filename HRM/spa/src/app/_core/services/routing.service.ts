import { Route, Router, Routes } from '@angular/router';
import { AuthService } from './auth/auth.service';
import { Injectable } from '@angular/core';
import { AuthGuard } from '@guards/auth/auth.guard';
import { appGuard } from '@guards/app.guard';
import { KeyValuePair } from '@utilities/key-value-pair';
import { DefaultLayoutComponent } from './../../containers/default-layout/default-layout.component';
import { DashboardComponent } from '@views/dashboard/dashboard.component';
import { LoginComponent } from '@views/login/login.component';
import { P404Component } from '@views/error/404.component';
import { P500Component } from '@views/error/500.component';
import { lastValueFrom } from 'rxjs';
import { LocalStorageConstants } from '@constants/local-storage.constants';

import { DirectoryMaintenanceModule } from '@views/system-maintenance/1_1_1_directory-maintenance/directory-maintenance.module';
import { ProgramMaintenanceModule } from '@views/system-maintenance/1_1_2_program-maintenance/program-maintenance.module';
import { SystemLanguageSettingModule } from '@views/system-maintenance/1_1_3_system-language-setting/system-language-setting.module';
import { DirectoryProgramLanguageSettingModule } from '@views/system-maintenance/1_1_4_directory-program-language-setting/directory-program-language-setting.module';

import { RoleSettingModule } from '@views/basic-maintenance/2_1_1_role-setting/role-setting.module';
import { AccountAuthorizationSettingModule } from '@views/basic-maintenance/2_1_2_account-authorization-setting/account-authorization-setting.module';
import { CodeTypeMaintenanceModule } from '@views/basic-maintenance/2_1_3_code-type-maintenance/code-type-maintenance.module';
import { CodeMaintenanceModule } from '@views/basic-maintenance/2_1_4_code-maintenance/code-maintenance.module';
import { CodeLanguageModule } from '@views/basic-maintenance/2_1_5_code-language/code-language.module';
import { GradeMaintenanceModule } from '@views/basic-maintenance/2_1_6_grade-maintenance/grade-maintenance.module';
import { FactoryComparisonModule } from '@views/basic-maintenance/2_1_7_factory-comparison/factory-comparison.module';
import { ResetPasswordModule } from '@views/basic-maintenance/2_1_8_reset-password/reset-password.module';

import { DepartmentMaintenanceModule } from '@views/organization-management/3_1_1_department-maintenance/department-maintenance.module';
import { WorkTypeHeadcountMaintenanceModule } from '@views/organization-management/3_1_2_work-type-headcount-maintenance/work-type-headcount-maintenance.module';
import { OrganizationChartModule } from '@views/organization-management/3_1_3_organization-chart/organization-chart.module';
import { DirectDepartmentSettingModule } from '@views/organization-management/3_1_4_direct_department_setting/direct-department-setting.module';
import { DirectWorkTypeAndSectionSettingModule } from '@views/organization-management/3_1_5_direct-work-type-and-section-setting/3_5_direct-work-type-and-section-setting.module';

import { EmployeeBasicInformationMaintenanceModule } from '@views/employee-maintenance/4_1_1_employee-basic-information-maintenance/employee-basic-information-maintenance.module';
import { IdentificationCardHistoryModule } from '@views/employee-maintenance/4_1_6_identification-card-history/identification-card-history.module';
import { IdentificationCardtoEmployeeIDHistoryModule } from '@views/employee-maintenance/4_1_7_identification-card-to-employee-id-history/identification-card-to-employee-id-history.module';
import { EmployeeGroupSkillSettingsModule } from '@views/employee-maintenance/4_1_8_employee-group-skill-settings/employee-group-skill-settings.module';
import { DocumentManagementModule } from '@views/employee-maintenance/4_1_9_document-management/document-management.module';
import { CertificationsModule } from '@views/employee-maintenance/4_1_10_certifications/certifications.module';
import { UnpaidLeaveModule } from '@views/employee-maintenance/4_1_11_unpaid-leave/unpaid-leave.module';
import { ResignationManagementModule } from '@views/employee-maintenance/4_1_12_resignation-management/resignation-management.module';
import { ExitEmployeesBlacklistModule } from '@views/employee-maintenance/4_1_13_exit-employees-blacklist/exit-employees-blacklist.module';
import { ContractTypeSetupModule } from '@views/employee-maintenance/4_1_14_contract-type-setup/contract-type-setup.module';
import { ContractManagementModule } from '@views/employee-maintenance/4_1_15_contract-management/contract-management.module';
import { ContractManagementReportsModule } from '@views/employee-maintenance/4_1_16_contract-management-report/contract-management-reports.module';
import { EmployeeTransferHistoryModule } from '@views/employee-maintenance/4_1_17_employee-transfer-history/employee-transfer-history.module';
import { RehireEvaluationforFormerEmployeesModule } from '@views/employee-maintenance/4_1_18_rehire-evaluation-for-former-employees/rehire-evaluation-for-former-employees.module';
import { ExitEmployeeMasterFileHistoricalDataModule } from '@views/employee-maintenance/4_1_19_exit-employee-master-file-historical-data/exit-employee-master-file-historical-data.module';
import { EmployeeTransferOperationOutboundModule } from '@views/employee-maintenance/4_1_20_employee-transfer-operation-outbound/employee-transfer-operation-outbound.module';
import { EmployeeTransferOperationInboundModule } from '@views/employee-maintenance/4_1_21_employee-transfer-operation-inbound/employee-transfer-operation-inbound.module';
import { EmployeeBasicInformationReportModule } from '@views/employee-maintenance/4_2_1_employee-basic-information-report/employee-basic-information-report.module';
import { EmergencyContactsSheetReportModule } from '@views/employee-maintenance/4_2_2_emergency-contacts-report/emergency-contacts-report.module';

import { FactoryCalendarModule } from '@views/attendance-maintenance/5_1_1_factory-calendar/factory-calendar.module';
import { ShiftScheduleSettingModule } from '@views/attendance-maintenance/5_1_2_shift-schedule-setting/shift-schedule-setting.module';
import { SpecialWorkTypeAnnualLeaveDaysMaintenanceModule } from '@views/attendance-maintenance/5_1_3_special-work-type-annual-leave-days-maintenance/special-work-type-annual-leave-days-maintenance.module';
import { OvertimeParameterSettingModule } from '@views/attendance-maintenance/5_1_4_overtime-parameter-setting/overtime-parameter-setting.module';
import { PregnancyandMaternityDataMaintenanceModule } from '@views/attendance-maintenance/5_1_5_pregnancy-and-maternity-data-maintenance/pregnancy-and-maternity-data-maintenance.module';
import { EmployeeLunchBreakTimeSettingModule } from '@views/attendance-maintenance/5_1_6_employee-lunch-break-time-setting/employee-lunch-break-time-setting.module';
import { MaintenanceofAnnualLeaveEntitlementModule } from '@views/attendance-maintenance/5_1_7_maintenance-of-annual-leave-entitlement/maintenance-of-annual-leave-entitlement.module';
import { CardSwipingDataFormatSettingModule } from '@views/attendance-maintenance/5_1_8_card-swiping-data-format-setting/card-swiping-data-format-setting.module';
import { MonthlyAttendanceSettingModule } from '@views/attendance-maintenance/5_1_9_monthly-attendance-setting/monthly-attendance-setting.module';
import { ShiftManagementProgramModule } from '@views/attendance-maintenance/5_1_10_shift-management-program/shift-management-program.module';
import { LeaveApplicationMaintenanceModule } from '@views/attendance-maintenance/5_1_11_leave-application-maintenance/leave-application-maintenance.module';
import { OvertimeApplicationMaintenanceModule } from '@views/attendance-maintenance/5_1_12_overtime-application-maintenance/overtime-application-maintenance.module';
import { SwipeCardDataUploadModule } from '@views/attendance-maintenance/5_1_13_swipe-card-data-upload/swipe-card-data-upload.module';
import { EmployeeDailyAttendanceDataGenerationModule } from '@views/attendance-maintenance/5_1_14_employee-daily-attendance-data-generation/employee-daily-attendance-data-generation.module';
import { AttendanceAbnormalityDataMaintenanceModule } from '@views/attendance-maintenance/5_1_15_attendance-abnormality-data-maintenance/attendance-abnormality-data-maintenance.module';
import { OvertimeTemporaryRecordMaintenanceModule } from '@views/attendance-maintenance/5_1_16_overtime_temporary_record_maintenance/overtime-temporary-record-maintenance.module';
import { DailyAttendancePostingModule } from '@views/attendance-maintenance/5_1_17_daily-attendance-posting/daily-attendance-posting.module';
import { AttendanceChangeRecordMaintenanceModule } from '@views/attendance-maintenance/5_1_18_attendance-change-record-maintenance/attendance-change-record-maintenance.module';
import { LeaveRecordModificationMaintenanceModule } from '@views/attendance-maintenance/5_1_19_leave-record-modification-maintenance/leave-record-modification-maintenance.module';
import { OvertimeModificationMaintenanceModule } from '@views/attendance-maintenance/5_1_20_overtime-modification-maintenance/overtime-modification-maintenance.module';
import { MonthlyAttendanceDataGenerationActiveEmployeesModule } from '@views/attendance-maintenance/5_1_21_monthly-attendance-data-generation-active-employees/monthly-attendance-data-generation-active-employees.module';
import { MonthlyAttendanceDataMaintenanceActiveEmployeesModule } from '@views/attendance-maintenance/5_1_22_monthly-attendance-data-maintenance-active-employees/monthly-attendance-data-maintenance-active-employees.module';
import { MonthlyAttendanceDataGenerationResignedEmployeesModule } from '@views/attendance-maintenance/5_1_23_monthly-attendance-data-generation-resigned-employees/monthly-attendance-data-generation-resigned-employees.module';
import { MonthlyAttendanceDataMaintenanceResignedEmployeesModule } from '@views/attendance-maintenance/5_1_24_monthly-attendance-data-maintenance-resigned-employees/monthly-attendance-data-maintenance-resigned-employees.module';
import { LoanedMonthAttendanceDataGenerationModule } from '@views/attendance-maintenance/5_1_25_loaned-month-attendance-data-generation/loaned-month-attendance-data-generation.module';
import { FemaleEmployeeMenstrualLeaveHoursMaintenanceModule } from '@views/attendance-maintenance/5_1_26_female-employee-menstrual-leave-hours-maintenance/female-employee-menstrual-leave-hours-maintenance.module';
import { LoanedMonthlyAttendanceDataMaintenanceModule } from '@views/attendance-maintenance/5_1_27_loaned-monthly-attendance-data-maintenance/loaned-monthly-attendance-data-maintenance.module';
import { NewResignedEmployeeDataPrintingModule } from '@views/attendance-maintenance/5_2_1_new-resigned-employee-data-printing/new-resigned-employee-data-printing.module';
import { WorkingHoursReportModule } from '@views/attendance-maintenance/5_2_2_working-hours-report/working-hours-report.module';
import { WeeklyWorkingHoursReportModule } from '@views/attendance-maintenance/5_2_3_weekly-working-hours-report/weekly-working-hours-report.module';
import { EmployeeAttendanceDataSheetModule } from '@views/attendance-maintenance/5_2_4_employee-attendance-data-sheet/employee-attendance-data-sheet.module';
import { DailySwipeCardAnomaliesListModule } from '@views/attendance-maintenance/5_2_5_daily_swipe_card_anomalies_list/daily_swipe_card_anomalies_list.module';
import { DailyDinnerAllowanceListModule } from '@views/attendance-maintenance/5_2_6_daily_dinner_allowance_list/daily_dinner_allowance_list.module';
import { DailyUnregisteredOvertimeListModule } from '@views/attendance-maintenance/5_2_7_daily_unregistered_overtime_list/daily_unregistered_overtime_list.module';
import { DailyNoNightShiftHoursListModule } from '@views/attendance-maintenance/5_2_8_daily_no_night_shift_hours_list/daily_no_night_shift_hours_list.module';
import { AbsenceDailyReportModule } from '@views/attendance-maintenance/5_2_9_absence-daily-report/absence-daily-report.module';
import { HRDailyReportModule } from '@views/attendance-maintenance/5_2_10_HR-daily-report/hr-daily-report.module';
import { MonthlyEmployeeStatusChangesSheetByDepartmentModule } from '@views/attendance-maintenance/5_2_11_monthly-employee-status-changes-sheet_by-department/monthly-employee-status-changes-sheet-by-department.module';
import { MonthlyEmployeeStatusChangesSheetByGenderModule } from '@views/attendance-maintenance/5_2_12_monthly-employee-status-changes-sheet_by-gender/monthly-employee-status-changes-sheet-by-gender.module';
import { MonthlyEmployeeStatusChangesSheetByWorkTypeJobModule } from '@views/attendance-maintenance/5_2_13_monthly-employee-status-changes-sheet-by-work-type-job/monthly-employee-status-changes-sheet-by-work-type-job.module';
import { MonthlyEmployeeStatusChangesSheetByReasonForResignationModule } from '@views/attendance-maintenance/5_2_14_monthly-employee-status-changes-sheet-by-reason-for-resignation/monthly-employee-status-changes-sheet-by-reason-for-resignation.module';
import { DepartmentMonthlyWorkingHoursReportModule } from '@views/attendance-maintenance/5_2_15_department_monthly_working_hours_report/department-monthly-working-hours-report.module';
import { IndividualMonthlyWorkingHoursReportModule } from '@views/attendance-maintenance/5_2_16_individual-monthly-working-hours-report/individual-monthly-working-hours-report.module';
import { MonthlyWorkingHoursLeaveHoursReportModule } from '@views/attendance-maintenance/5_2_17_monthly-working-hours-leave-hours-report/monthly-working-hours-leave-hours-report.module';
import { EmployeeOvertimeDataSheetModule } from '@views/attendance-maintenance/5_2_18_employee-over-time-data-sheet/employee-over-time-data-sheet.module';
import { OvertimeHoursReportModule } from '@views/attendance-maintenance/5_2_19_overtime-hours-report/overtime-hours-report.module';
import { AnnualOvertimeHoursReportModule } from '@views/attendance-maintenance/5_2_20_annual-overtime-hours-report/annual-overtime-hours-report.module';
import { EmployeeOvertimeExceedingHoursReportModule } from '@views/attendance-maintenance/5_2_21_employee-overtime-exceeding-hours-report/employee-overtime-exceeding-hours-report.module';
import { AnnualLeaveCalculationModule } from '@views/attendance-maintenance/5_2_22_annual-leave-calculation/annual-leave-calculation.module';
import { FactoryResignationAnalysisReportModule } from '@views/attendance-maintenance/5_2_23_factory_resignation_analysis_report/factory_resignation_analysis_report.module';
import { ResignationAnnualLeaveWorktypeAnalysisReportModule } from '@views/attendance-maintenance/5_2_24_resignation-annual-leave-work-type-analysis-report/resignation-annual-leave-work-type-analysis-report.module';
import { MonthlyFactoryWorkingHoursReportModule } from '@views/attendance-maintenance/5_2_25_monthly-factory-working-hours-report/monthly-factory-working-hours-report.module';

import { CompulsoryInsuranceDataMaintenanceModule } from '@views/compulsory-insurance-management/6_1_1_compulsory_insurance_data_maintenance/compulsory_insurance_data_maintenance.module';
import { ContributeRateSettingModule } from '@views/compulsory-insurance-management/6_1_2_contribution-rate-setting/contribute-rate-setting.module';
import { ApplySocialInsuranceBenefitsMaintenanceModule } from '@views/compulsory-insurance-management/6_1_3_apply_social_insurance_benefits_maintenance/apply_social_insurance_benefits_maintenance.module';
import { NewEmployeesCompulsoryInsurancePremiumModule } from '@views/compulsory-insurance-management/6_1_4_new_employees_compulsory_insurance_premium/new-employees-compulsory-insurance-premium.module';
import { MonthlyCompulsoryInsuranceDetailedReportModule } from '@views/compulsory-insurance-management/6_2_1_monthly-compulsory-insurance-detailed-report/monthly-compulsory-insurance-detailed-report.module';
import { MonthlyCompulsoryInsuranceSummaryReportModule } from '@views/compulsory-insurance-management/6_2_2_monthly-compulsory-insurance-summary-report/monthly-compulsory-insurance-summary-report.module';

import { SalaryItemandAmountSettingsModule } from '@views/salary-maintenance/7_1_1_salary-item-and-amount-settings/salary-item-and-amount-settings.module';
import { MonthlyExchangeRateSettingModule } from '@views/salary-maintenance/7_1_2_monthly-exchange-rate-setting/monthly-exchange-rate-setting.module';
import { LeaveSalaryCalculationMaintenanceModule } from '@views/salary-maintenance/7_1_3_leave_salary_calculation_maintenance/7_3_leave_salary_calculation_maintenance.module';
import { BankAccountMaintenanceModule } from '@views/salary-maintenance/7_1_4_bank_account_maintenance/bank_account_maintenance.module';
import { PayslipDeliverybyEmailMaintenanceModule } from '@views/salary-maintenance/7_1_5_payslip-delivery-by-email-maintenance/payslip-delivery-by-email-maintenance.module';
import { PersonalIncomeTaxNumberMaintenanceModule } from '@views/salary-maintenance/7_1_6_personal-income-tax-number-maintenance/personal-income-tax-number-maintenance.module';
import { ListofChildcareSubsidyRecipientsMaintenanceModule } from '@views/salary-maintenance/7_1_7_list-of-childcare-subsidy-recipients-maintenance/list-of-childcare-subsidy-recipients-maintenance.module';
import { SAPCostCenterSettingModule } from '@views/salary-maintenance/7_1_8_sap-cost-center-setting/sap-cost-center-setting.module';
import { DepartmenttoSAPCostCenterMappingMaintenanceModule } from '@views/salary-maintenance/7_1_9_department-to-sap-cost-center-mapping/department-to-sap-cost-center-mapping.module';
import { SalaryItemtoAccountingCodeMappingMaintenanceModule } from '@views/salary-maintenance/7_1_10_salary-item-to-accounting-code-mapping-maintenance/salary-item-to-accounting-code-mapping-maintenance.module';
import { AdditionDeductionItemtoAccountingCodeMappingMaintenanceModule } from '@views/salary-maintenance/7_1_11_addition-deduction-item-to-accounting-code-mapping-maintenance/addition-deduction-item-to-accounting-code-mapping-maintenance.module';
import { AdditionDeductionItemandAmountSettingsModule } from '@views/salary-maintenance/7_1_12_addition-deduction-item-and-amount-settings/addition-deduction-item-and-amount-settings.module';
import { IncomeTaxBracketSettingModule } from "@views/salary-maintenance/7_1_13_income-tax-bracket-setting/income-tax-bracket-setting.module";
import { IncomeTaxFreeSettingModule } from "@views/salary-maintenance/7_1_14_income-tax-free-setting/income-tax-free-setting.module";
import { ChildcareSubsidyGenerationModule } from '@views/salary-maintenance/7_1_15_childcare-subsidy-generation/childcare-subsidy-generation.module';
import { SalaryMasterFileModule } from '@views/salary-maintenance/7_1_16_salary-master-file/salary-master-file.module';
import { MonthlySalaryMasterFileBackupQueryModule } from '@views/salary-maintenance/7_1_17_monthly-salary-master-file-backup-query/monthly-salary-master-file-backup-query.module';
import { SalaryAdjustmentMaintenanceModule } from '@views/salary-maintenance/7_1_18_salary-adjustment-maintenance/salary-adjustment-maintenance.module';
import { SalaryAdditionsAndDeductionsInputModule } from '@views/salary-maintenance/7_1_19_salary-additions-and-deductions-input/7_19_salary-additions-and-deductions-input.module';
import { NightShiftSubsidyMaintenanceModule } from '@views/salary-maintenance/7_1_20_night-shift-subsidy-maintenance/night-shift-subsidy-maintenance.module';
import { MenstrualLeaveHoursAllowanceModule } from '@views/salary-maintenance/7_1_21_menstrual-leave-hours-allowance/menstrual-leave-hours-allowance.module';
import { MonthlySalaryGenerationModule } from '@views/salary-maintenance/7_1_22_monthly-salary-generation/monthly-salary-generation.module';
import { MonthlySalaryGenerationExitedEmployeesModule } from '@views/salary-maintenance/7_1_23_monthly-salary-generation-exited-employees/monthly-salary-generation-exited-employees.module';
import { MonthlySalaryMaintenanceModule } from '@views/salary-maintenance/7_1_24_monthly-salary-maintenance/7_24_monthly-salary-maintenance.module';
import { MonthlySalaryMaintenanceExitedEmployeesModule } from '@views/salary-maintenance/7_1_25_monthly-salary-maintenance-exited-employees/monthly-salary-maintenance-exited-employees.module';
import { FinSalaryCloseMaintenanceModule } from '@views/salary-maintenance/7_1_26_fin-salary-close-maintenance/fin-salary-close-maintenance.module';
import { FinSalaryAttributionCategoryMaintenanceModule } from '@views/salary-maintenance/7_1_27_fin-salary-attribution-category-maintenance/fin-salary-attribution-category-maintenance.module';
import { SalaryApprovalFormModule } from '@views/salary-report/7_2_1_salary-approval-form/salary-approval-form.module';
import { UtilityWorkersQualificationSeniorityPrintingModule } from '@views/salary-report/7_2_2_utility-workers-qualification-seniority-printing/utility-workers-qualification-seniority-printing-routing.module';
import { MonthlySalarySummaryReportModule } from '@views/salary-report/7_2_4_monthly-salary-summary-report/monthly-salary-summary-report.module';
import { MonthlySalaryDetailReportModule } from '@views/salary-report/7_2_5_monthly-salary-detail-report/monthly-salary-detail-report.module';
import { MonthlyNonTransferSalaryPaymentReportModule } from '@views/salary-report/7_2_6_monthly-non-transfer-salary-payment-report/monthly-non-transfer-salary-payment-report.module';
import { MonthlySalaryAdditionsDeductionsSummaryReportModule } from "@views/salary-report/7_2_7_monthly-salary-additions-deductions-summary-report/monthly-salary-additions-deductions-summary-report.module";
import { SalarySummaryReportExitedEmployeeModule } from "@views/salary-report/7_2_9_salary-summary-report-exited-employee/salary-summary-report-exited-employee.module";
import { SalarySummaryReportExitedEmployeeByDepartmentModule } from "@views/salary-report/7_2_10_salary-summary-report-exited-employee-by-department/salary-summary-report-exited-employee-by-department.module";
import { MonthlySalaryTransferDetailsModule } from '@views/salary-report/7_2_12_monthly-salary-transfer-details/monthly-salary-transfer-details.module';
import { MonthlySalaryTransferDetailsExitedEmployeeModule } from '@views/salary-report/7_2_13_monthly-salary-transfer-details-exited-employee/monthly-salary-transfer-details-exited-employee.module';
import { TaxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayModule } from "@views/salary-report/7_2_14_tax-paying-employee-monthly-night-shift-extra-and-overtime-pay/tax-paying-employee-monthly-night-shift-extra-and-overtime-pay.module";
import { MonthlyUnionDuesSummaryModule } from "@views/salary-report/7_2_15_monthly-union-dues-summary/monthly-union-dues-summary.module";
import { DownloadPersonnelDataToExcelModule } from "@views/salary-report/7_2_16_download-personnel-data-to-excel/download-personnel-data-to-excel.module"
import { MonthlyPersonalIncomeTaxAmountReportModule } from "@views/salary-report/7_2_17_monthly-personal-income-tax-amount-report/monthly-personal-income-tax-amount-report.module";
import { AnnualIncomeTaxDetailReportModule } from '@views/salary-report/7_2_18_annual-income-tax-detail-report/annual-income-tax-detail-report.module';
import { MonthlySalarySummaryReportForFinanceModule } from '@views/salary-report/7_2_19_monthly-salary-summary-report-for-finance/monthly-salary-summary-report-for-finance.module';
import { MonthlySalarySummaryReportForTaxationModule } from '@views/salary-report/7_2_20_monthly-salary-summary-report-for-taxation/monthly-salary-summary-report-for-taxation.module';
import { MonthlyAdditionsAndDeductionsSummaryReportForFinanceModule } from '@views/salary-report/7_2_21_monthly-additions-and-deductions-summary-report-for-finance/monthly-additions-and-deductions-summary-report-for-finance.module';
import { MonthlyAdditionsAndDeductionsSummaryReportModule } from '@views/salary-report/7_2_22_monthly-additions-and-deductions-summary-report/monthly-additions-and-deductions-summary-report.module';

import { RewardAndPenaltyReasonCodeMaintenanceModule } from '@views/reward-and-penalty-maintenance/8_1_1_reward-and-penalty-reason-code-maintenance/reward-and-penalty-reason-code-maintenance.module';
import { EmployeeRewardAndPenaltyRecordsModule } from '@views/reward-and-penalty-maintenance/8_1_2_employee-reward-and-penalty-records/employee-reward-and-penalty-records.module';
import { EmployeeRewardAndPenaltyReportModule } from '@views/reward-and-penalty-report/8_2_1_employee-reward-and-penalty-report/employee-reward-and-penalty-report.module';

@Injectable({
  providedIn: 'root'
})
export class RoutingService {
  defaultRoute: Routes = [
    {
      path: '',
      redirectTo: 'dashboard',
      pathMatch: 'full',
    },
    {
      path: '404',
      component: P404Component,
      data: { title: 'Page 404' }
    },
    {
      path: '500',
      component: P500Component,
      data: { title: 'Page 500' }
    },
    {
      path: 'login',
      component: LoginComponent,
      data: { title: 'Login Page' }
    },
    { path: '**', component: P404Component }
  ];

  //#region Register Modules
  //System Maintenance
  Module_1_1_1 = () => DirectoryMaintenanceModule
  Module_1_1_2 = () => ProgramMaintenanceModule
  Module_1_1_3 = () => SystemLanguageSettingModule
  Module_1_1_4 = () => DirectoryProgramLanguageSettingModule

  //Basic Maintenance
  Module_2_1_1 = () => RoleSettingModule
  Module_2_1_2 = () => AccountAuthorizationSettingModule
  Module_2_1_3 = () => CodeTypeMaintenanceModule
  Module_2_1_4 = () => CodeMaintenanceModule
  Module_2_1_5 = () => CodeLanguageModule
  Module_2_1_6 = () => GradeMaintenanceModule
  Module_2_1_7 = () => FactoryComparisonModule
  Module_2_1_8 = () => ResetPasswordModule

  //Organization Management
  Module_3_1_1 = () => DepartmentMaintenanceModule
  Module_3_1_2 = () => WorkTypeHeadcountMaintenanceModule
  Module_3_1_3 = () => OrganizationChartModule
  Module_3_1_4 = () => DirectDepartmentSettingModule
  Module_3_1_5 = () => DirectWorkTypeAndSectionSettingModule

  //Employee Maintenance
  Module_4_1_1 = () => EmployeeBasicInformationMaintenanceModule
  Module_4_1_6 = () => IdentificationCardHistoryModule
  Module_4_1_7 = () => IdentificationCardtoEmployeeIDHistoryModule
  Module_4_1_8 = () => EmployeeGroupSkillSettingsModule
  Module_4_1_9 = () => DocumentManagementModule
  Module_4_1_10 = () => CertificationsModule
  Module_4_1_11 = () => UnpaidLeaveModule
  Module_4_1_12 = () => ResignationManagementModule
  Module_4_1_13 = () => ExitEmployeesBlacklistModule
  Module_4_1_14 = () => ContractTypeSetupModule
  Module_4_1_15 = () => ContractManagementModule
  Module_4_1_16 = () => ContractManagementReportsModule
  Module_4_1_17 = () => EmployeeTransferHistoryModule
  Module_4_1_18 = () => RehireEvaluationforFormerEmployeesModule
  Module_4_1_19 = () => ExitEmployeeMasterFileHistoricalDataModule
  Module_4_1_20 = () => EmployeeTransferOperationOutboundModule
  Module_4_1_21 = () => EmployeeTransferOperationInboundModule
  Module_4_2_1 = () => EmployeeBasicInformationReportModule
  Module_4_2_2 = () => EmergencyContactsSheetReportModule

  // AttendanceMaintenance
  Module_5_1_1 = () => FactoryCalendarModule
  Module_5_1_2 = () => ShiftScheduleSettingModule
  Module_5_1_3 = () => SpecialWorkTypeAnnualLeaveDaysMaintenanceModule
  Module_5_1_4 = () => OvertimeParameterSettingModule
  Module_5_1_5 = () => PregnancyandMaternityDataMaintenanceModule
  Module_5_1_6 = () => EmployeeLunchBreakTimeSettingModule
  Module_5_1_7 = () => MaintenanceofAnnualLeaveEntitlementModule
  Module_5_1_8 = () => CardSwipingDataFormatSettingModule
  Module_5_1_9 = () => MonthlyAttendanceSettingModule
  Module_5_1_10 = () => ShiftManagementProgramModule
  Module_5_1_11 = () => LeaveApplicationMaintenanceModule
  Module_5_1_12 = () => OvertimeApplicationMaintenanceModule
  Module_5_1_13 = () => SwipeCardDataUploadModule
  Module_5_1_14 = () => EmployeeDailyAttendanceDataGenerationModule
  Module_5_1_15 = () => AttendanceAbnormalityDataMaintenanceModule
  Module_5_1_16 = () => OvertimeTemporaryRecordMaintenanceModule
  Module_5_1_17 = () => DailyAttendancePostingModule
  Module_5_1_18 = () => AttendanceChangeRecordMaintenanceModule
  Module_5_1_19 = () => LeaveRecordModificationMaintenanceModule
  Module_5_1_20 = () => OvertimeModificationMaintenanceModule
  Module_5_1_21 = () => MonthlyAttendanceDataGenerationActiveEmployeesModule
  Module_5_1_22 = () => MonthlyAttendanceDataMaintenanceActiveEmployeesModule
  Module_5_1_23 = () => MonthlyAttendanceDataGenerationResignedEmployeesModule
  Module_5_1_24 = () => MonthlyAttendanceDataMaintenanceResignedEmployeesModule
  Module_5_1_25 = () => LoanedMonthAttendanceDataGenerationModule
  Module_5_1_26 = () => FemaleEmployeeMenstrualLeaveHoursMaintenanceModule
  Module_5_1_27 = () => LoanedMonthlyAttendanceDataMaintenanceModule
  Module_5_2_1 = () => NewResignedEmployeeDataPrintingModule
  Module_5_2_2 = () => WorkingHoursReportModule
  Module_5_2_3 = () => WeeklyWorkingHoursReportModule
  Module_5_2_4 = () => EmployeeAttendanceDataSheetModule
  Module_5_2_5 = () => DailySwipeCardAnomaliesListModule
  Module_5_2_6 = () => DailyDinnerAllowanceListModule
  Module_5_2_7 = () => DailyUnregisteredOvertimeListModule
  Module_5_2_8 = () => DailyNoNightShiftHoursListModule
  Module_5_2_9 = () => AbsenceDailyReportModule
  Module_5_2_10 = () => HRDailyReportModule
  Module_5_2_11 = () => MonthlyEmployeeStatusChangesSheetByDepartmentModule
  Module_5_2_12 = () => MonthlyEmployeeStatusChangesSheetByGenderModule
  Module_5_2_13 = () => MonthlyEmployeeStatusChangesSheetByWorkTypeJobModule
  Module_5_2_14 = () => MonthlyEmployeeStatusChangesSheetByReasonForResignationModule
  Module_5_2_15 = () => DepartmentMonthlyWorkingHoursReportModule
  Module_5_2_16 = () => IndividualMonthlyWorkingHoursReportModule
  Module_5_2_17 = () => MonthlyWorkingHoursLeaveHoursReportModule
  Module_5_2_18 = () => EmployeeOvertimeDataSheetModule
  Module_5_2_19 = () => OvertimeHoursReportModule
  Module_5_2_20 = () => AnnualOvertimeHoursReportModule
  Module_5_2_21 = () => EmployeeOvertimeExceedingHoursReportModule
  Module_5_2_22 = () => AnnualLeaveCalculationModule
  Module_5_2_23 = () => FactoryResignationAnalysisReportModule
  Module_5_2_24 = () => ResignationAnnualLeaveWorktypeAnalysisReportModule
  Module_5_2_25 = () => MonthlyFactoryWorkingHoursReportModule

  //Compulsory Insurance Management
  Module_6_1_1 = () => CompulsoryInsuranceDataMaintenanceModule
  Module_6_1_2 = () => ContributeRateSettingModule
  Module_6_1_3 = () => ApplySocialInsuranceBenefitsMaintenanceModule
  Module_6_1_4 = () => NewEmployeesCompulsoryInsurancePremiumModule
  Module_6_2_1 = () => MonthlyCompulsoryInsuranceDetailedReportModule
  Module_6_2_2 = () => MonthlyCompulsoryInsuranceSummaryReportModule

  //Salary Maintenance
  Module_7_1_1 = () => SalaryItemandAmountSettingsModule
  Module_7_1_2 = () => MonthlyExchangeRateSettingModule
  Module_7_1_3 = () => LeaveSalaryCalculationMaintenanceModule
  Module_7_1_4 = () => BankAccountMaintenanceModule
  Module_7_1_5 = () => PayslipDeliverybyEmailMaintenanceModule
  Module_7_1_6 = () => PersonalIncomeTaxNumberMaintenanceModule
  Module_7_1_7 = () => ListofChildcareSubsidyRecipientsMaintenanceModule
  Module_7_1_8 = () => SAPCostCenterSettingModule
  Module_7_1_9 = () => DepartmenttoSAPCostCenterMappingMaintenanceModule
  Module_7_1_10 = () => SalaryItemtoAccountingCodeMappingMaintenanceModule
  Module_7_1_11 = () => AdditionDeductionItemtoAccountingCodeMappingMaintenanceModule
  Module_7_1_12 = () => AdditionDeductionItemandAmountSettingsModule
  Module_7_1_13 = () => IncomeTaxBracketSettingModule
  Module_7_1_14 = () => IncomeTaxFreeSettingModule
  Module_7_1_15 = () => ChildcareSubsidyGenerationModule
  Module_7_1_16 = () => SalaryMasterFileModule
  Module_7_1_17 = () => MonthlySalaryMasterFileBackupQueryModule
  Module_7_1_18 = () => SalaryAdjustmentMaintenanceModule
  Module_7_1_19 = () => SalaryAdditionsAndDeductionsInputModule
  Module_7_1_20 = () => NightShiftSubsidyMaintenanceModule
  Module_7_1_21 = () => MenstrualLeaveHoursAllowanceModule
  Module_7_1_22 = () => MonthlySalaryGenerationModule
  Module_7_1_23 = () => MonthlySalaryGenerationExitedEmployeesModule
  Module_7_1_24 = () => MonthlySalaryMaintenanceModule
  Module_7_1_25 = () => MonthlySalaryMaintenanceExitedEmployeesModule
  Module_7_1_26 = () => FinSalaryCloseMaintenanceModule
  Module_7_1_27 = () => FinSalaryAttributionCategoryMaintenanceModule
  Module_7_2_1 = () => SalaryApprovalFormModule
  Module_7_2_2 = () => UtilityWorkersQualificationSeniorityPrintingModule
  Module_7_2_4 = () => MonthlySalarySummaryReportModule
  Module_7_2_5 = () => MonthlySalaryDetailReportModule
  Module_7_2_6 = () => MonthlyNonTransferSalaryPaymentReportModule
  Module_7_2_7 = () => MonthlySalaryAdditionsDeductionsSummaryReportModule
  Module_7_2_9 = () => SalarySummaryReportExitedEmployeeModule
  Module_7_2_10 = () => SalarySummaryReportExitedEmployeeByDepartmentModule
  Module_7_2_12 = () => MonthlySalaryTransferDetailsModule
  Module_7_2_13 = () => MonthlySalaryTransferDetailsExitedEmployeeModule
  Module_7_2_14 = () => TaxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayModule
  Module_7_2_15 = () => MonthlyUnionDuesSummaryModule
  Module_7_2_16 = () => DownloadPersonnelDataToExcelModule
  Module_7_2_17 = () => MonthlyPersonalIncomeTaxAmountReportModule
  Module_7_2_18 = () => AnnualIncomeTaxDetailReportModule
  Module_7_2_19 = () => MonthlySalarySummaryReportForFinanceModule
  Module_7_2_20 = () => MonthlySalarySummaryReportForTaxationModule
  Module_7_2_21 = () => MonthlyAdditionsAndDeductionsSummaryReportForFinanceModule
  Module_7_2_22 = () => MonthlyAdditionsAndDeductionsSummaryReportModule

  //Reward and Penalty Maintenance
  Module_8_1_1 = () => RewardAndPenaltyReasonCodeMaintenanceModule
  Module_8_1_2 = () => EmployeeRewardAndPenaltyRecordsModule
  Module_8_2_1 = () => EmployeeRewardAndPenaltyReportModule

  //#endregion

  constructor(
    private service: AuthService,
    private router: Router,
  ) { }

  async loadRouting(): Promise<any> {
    try {
      const directions = await lastValueFrom(this.service.getDirection());
      const directionRouting = await this.getDirectionModule(directions);
      localStorage.setItem(LocalStorageConstants.ROUTING, JSON.stringify(directionRouting.map(x => `/${x.path}`)))
      this.defaultRoute.splice(1, 0, {
        path: '',
        canActivate: [AuthGuard],
        component: DefaultLayoutComponent,
        data: { title: 'Home' },
        children: [
          {
            path: 'dashboard',
            component: DashboardComponent
          },
          ...directionRouting
        ]
      });
      this.router.resetConfig(this.defaultRoute);
      if (window.location.href.endsWith('/500') || window.location.href.endsWith('/404'))
        this.router.navigate(["/dashboard"]);
      return true;
    } catch (error) {
      this.router.resetConfig(this.defaultRoute);
      this.router.navigate(["/500"]);
      return true;
    }
  }
  private async getDirectionModule(directions: KeyValuePair[]): Promise<Route[]> {
    const result = await Promise.all(
      directions.map(async (direction): Promise<Route[]> => {
        const routes = await this.getProgramsRouting(direction);
        return routes;
      })
    );
    return result.flat();
  }
  private async getProgramsRouting(direction: KeyValuePair): Promise<Route[]> {
    const programs = await lastValueFrom(this.service.getProgram(direction.key));
    return programs.flatMap(program => {
      const module = this["Module_" + program.key.replace(/[^a-zA-Z0-9]/g, ' ').replace(/\s/g, '_') as string];
      return !module ? [] : {
        path: (direction.value as string)?.toUrl() + '/' + (program.value as string)?.toUrl(),
        canMatch: [appGuard],
        loadChildren: module,
        data: { program: program.key }
      };
    });
  }
}
