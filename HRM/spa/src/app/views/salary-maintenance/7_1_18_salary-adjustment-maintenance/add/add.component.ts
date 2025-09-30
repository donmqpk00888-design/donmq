import { Event } from '@angular/router';
import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { HistoryDetail, SalaryAdjustmentMaintenanceMain, SalaryAdjustmentMaintenance_SalaryItem } from '@models/salary-maintenance/7_1_18_salary-adjustment-maintenance';
import { S_7_1_18_salaryAdjustmentMaintenanceService } from '@services/salary-maintenance/s_7_1_18_salary-adjustment-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-add',
  templateUrl: './add.component.html',
  styleUrls: ['./add.component.scss']
})
export class AddComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  title: string = '';
  url: string = '';
  action: string = '';
  effectiveDate: Date = new Date();
  probationSalaryMonth: Date = null;
  dataTypeaHead: string[];
  listFactory: KeyValuePair[] = [];
  listReasonForChange: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listCurrency: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = []
  listPositionTitle: KeyValuePair[] = []
  listPermissionGroup: KeyValuePair[] = []
  listTechnicalType: KeyValuePair[] = []
  listExpertiseCategory: KeyValuePair[] = []
  before: HistoryDetail = <HistoryDetail>{};
  after: HistoryDetail = <HistoryDetail>{
    salary_Grade: 0,
    salary_Level: 0
  };
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Onjob' },
    { key: 'N', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Resigned' },
    { key: 'U', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Unpaid' },
  ];
  listSalaryItem: SalaryAdjustmentMaintenance_SalaryItem[] = []

  data: SalaryAdjustmentMaintenanceMain = <SalaryAdjustmentMaintenanceMain>{ seq: 1 };
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  bsConfigMonth: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  isCheckEffectiveDate: boolean = false
  isReasonForChange: boolean = false

  constructor(private service: S_7_1_18_salaryAdjustmentMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropdownList();
      if (!this.functionUtility.checkEmpty(this.data.employee_ID))
        this.getDetailPersonal();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getDropdownList();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
    });
  }

  getDropdownList() {
    this.getlistFactory();
    this.getlistReasonForChange();
    this.getlistSalaryType();
    this.getlistCurrency();
    this.getlistDepartment();
    this.getlistPositionTitle();
    this.getlistPermissionGroup();
    this.getlistTechnicalType();
    this.getlistExpertiseCategory();
  }

  getlistEmployeeID(factory: string) {
    this.service.getlistEmployeeID(factory).subscribe({
      next: (res) => {
        this.dataTypeaHead = res;
      },
    });
  }

  onTypehead(e: TypeaheadMatch) {
    this.data.employee_ID = e.value;
    this.getDetailPersonal();
  }
  getDetailPersonal() {
    if (this.data.factory && this.data.employee_ID) {
      this.service.getDetailPersonal(this.data.factory, this.data.employee_ID).subscribe({
        next: res => {
          if (res) {
            if (res.resign_Date != null) {
              this.deleteEmpInfo()
              this.before = this.after = <HistoryDetail>{}
              return this.functionUtility.snotifySuccessError(false, this.translateService.instant('SalaryMaintenance.SalaryAdjustmentMaintenance.EmployeeHasLeft'))
            }
            this.data.useR_GUID = res?.useR_GUID;
            this.data.division = res?.division;
            this.data.local_Full_Name = res?.local_Full_Name;
            this.data.onboard_Date = res?.onboard_Date;
            this.data.employment_Status = res?.employment_Status;
            this.before = res.before != null ? res.before : <HistoryDetail>{};
            this.after = res?.after;
            this.checkEffectiveDate()
          } else {
            this.deleteEmpInfo()
            this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
          }
        }
      })
    } else this.deleteEmpInfo()
    this.onDataChange()
  }
  deleteEmpInfo() {
    this.deleteProperty('local_Full_Name');
    this.deleteProperty('onboard_Date');
    this.deleteProperty('employment_Status');
  }
  onFactoryChange() {
    this.deleteProperty('employee_ID');
    this.deleteProperty('local_Full_Name');
    this.deleteProperty('onboard_Date');
    this.deleteProperty('employment_Status');
    this.getlistEmployeeID(this.data.factory)
    this.getlistDepartment()
    this.onDataChange()
  }
  onSalaryTypeChange() {
    this.getSalaryItemsAsync()
    this.onDataChange()
  }
  getlistFactory() {
    this.service.getlistFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getlistDepartment() {
    if (this.functionUtility.checkEmpty(this.data.factory)) return
    this.service.getlistDepartment(this.data.factory,).subscribe({
      next: (res: KeyValuePair[]) => {
        this.listDepartment = res

      }
    });
  }
  getlistPositionTitle() {
    this.service.getlistPositionTitle().subscribe({
      next: (res: KeyValuePair[]) => this.listPositionTitle = res
    });
  }
  getlistPermissionGroup() {
    this.service.getlistPermissionGroup().subscribe({
      next: (res: KeyValuePair[]) => this.listPermissionGroup = res
    });
  }
  getlistTechnicalType() {
    this.service.getlistTechnicalType().subscribe({
      next: (res: KeyValuePair[]) => this.listTechnicalType = res
    });
  }
  getlistExpertiseCategory() {
    this.service.getlistExpertiseCategory().subscribe({
      next: (res: KeyValuePair[]) => this.listExpertiseCategory = res
    });
  }
  checkEffectiveDate() {
    this.data.effective_Date = this.formatDate(this.effectiveDate)
    if (!this.functionUtility.checkEmpty(this.data.employee_ID) && !this.functionUtility.checkEmpty(this.data.effective_Date))
      this.service.checkEffectiveDate(this.data.factory, this.data.employee_ID, this.data.effective_Date).subscribe({
        next: (res) => {
          this.data.seq = res.maxSeq
          this.isCheckEffectiveDate = !res.checkEffectiveDate
          this.snotifyService.clear()
          if (!res.checkEffectiveDate)
            this.functionUtility.snotifySuccessError(false, "The effective date cannot be earlier than already stored effective dates in the transaction table.")
        }
      });
    this.onDataChange()
  }
  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }
  getlistReasonForChange() {
    this.service.getlistReasonForChange().subscribe({
      next: (res: KeyValuePair[]) => {
        this.listReasonForChange = res
      }
    });
  }
  getlistSalaryType() {
    this.service.getlistSalaryType().subscribe({
      next: (res: KeyValuePair[]) => this.listSalaryType = res
    });
  }
  getlistCurrency() {
    this.service.getlistCurrency().subscribe({
      next: (res: KeyValuePair[]) => this.listCurrency = res
    });
  }
  getlistSalaryItem() {
    if (this.data?.history_GUID) {
      this.service.getListSalaryItem(this.data.history_GUID,).subscribe({
        next: (res) => {
          this.listSalaryItem = res
        }
      })
    }
  }
  getSalaryItemsAsync() {
    this.service.getSalaryItemsAsync(this.data.factory, this.after.permission_Group, this.after.salary_Type, 'after', this.data.employee_ID).subscribe({
      next: (res) => this.after.item = res
    });
  }
  save() {
    this.data.effective_Date = this.formatDate(this.effectiveDate)
    this.data.probation_Salary_Month = this.formatDate(this.probationSalaryMonth)
    this.data.salary_Item = this.after.item
    this.data.after = this.after
    this.spinnerService.show();
    this.service.create(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
          this.back();
        }
        else
          this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
      }
    })
  }
  validateDecimal(event: KeyboardEvent, maxValue: number): boolean {
    const inputChar = event.key;
    const maxValueLength = maxValue.toString().length;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.includes(inputChar))
      return true;

    if (!/^\d$/.test(inputChar) && inputChar !== '.') {
      event.preventDefault();
      return false;
    }

    const input = event.target as HTMLInputElement;
    const currentValue = input.value;
    const newValue = currentValue.substring(0, input.selectionStart!) + inputChar + currentValue.substring(input.selectionEnd!);

    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (integerPartLength > maxValueLength ||
      (integerPartLength == maxValueLength && parts.length == 2 && parseInt(parts[0]) == maxValue) ||
      (integerPartLength === maxValueLength && parseInt(parts[0]) > maxValue) ||
      decimalPartLength > 1) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^(0|[1-9][0-9]?)(\.[0-9]{0,1})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    return true;
  }
  onDataChange() {
    this.data.update_By = this.user.id;
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date());
  }
  onReasonForChange() {
    this.service.checkReasonForChange(this.data.reason_For_Change).subscribe({
      next: (res: boolean) => this.isReasonForChange = res
    });
  }
  isValidProbationSalaryMonth(): boolean {
    if (!this.isReasonForChange)
      return true;

    return this.probationSalaryMonth != null &&
      this.data.probation_Salary_Month !== ''
  }
  deleteProperty = (name: string) => delete this.data[name]
  back = () => this.router.navigate([this.url]);
  changeValue = (max: number) => this.after.salary_Grade = this.after.salary_Grade > max ? Math.min(this.after.salary_Grade, max) : this.after.salary_Grade
}
