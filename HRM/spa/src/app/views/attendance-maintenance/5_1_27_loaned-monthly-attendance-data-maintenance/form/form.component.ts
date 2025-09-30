import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LoanedMonthlyAttendanceDataMaintenanceDto } from '@models/attendance-maintenance/5_1_27_loaned-monthly-attendance-data-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_27_loaned-monthly-attendance-data-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  action: string = '';
  listFactory: KeyValuePair[] = [];
  data: LoanedMonthlyAttendanceDataMaintenanceDto = <LoanedMonthlyAttendanceDataMaintenanceDto>{
    update_By: this.getCurrentUser(),
    update_Time: new Date().toStringDateTime()
  };
  minMode: BsDatepickerViewMode = 'month';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  };
  att_Month: Date;
  iconButton = IconButton;
  classButton = ClassButton;
  isEdit: boolean = false;
  isQuery: boolean = false;
  employee_ID: string[] = [];

  pass = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];

  resign_Status = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ];

  constructor(
    private service: S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.onChange();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListFactory();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
    });
  }

  getCurrentUser() {
    const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
    return user ? user.account : '';
  }

  //#region getDataFromSource
  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSearch();
      this.isEdit = source.isEdit
      this.isQuery = source.isQuery
      if (source && source.data.length > 0 || this.action == "Add") {
        if (!this.isEdit && !this.isQuery) {
          this.data = <LoanedMonthlyAttendanceDataMaintenanceDto>{
            pass: false,
            resign_Status: 'N',
            salary_Days: 0,
            actual_Days: 0,
            delay_Early: 0,
            no_Swip_Card: 0,
            food_Expenses: 0,
            night_Eat_Times: 0,
            update_By: this.getCurrentUser(),
            update_Time: new Date().toStringDateTime()
          }
        } else {
          this.data = { ...source.source };
          this.att_Month = this.data.att_Month.toDate();
          this.data.update_By = this.getCurrentUser();
          this.data.update_Time = new Date().toStringDateTime();
          this.onChange();
          this.data.permission_Group = source.source.permission_Group;
        }
      }
      else
        this.back();
    })
  }
  //#endregion

  //#region getEmployeeData
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getEmployeeID() {
    this.service.getEmployeeID(this.data.factory).subscribe({
      next: (res) => {
        this.employee_ID = res
      }
    })
  }

  onFactoryChange() {
    this.deleteProperty('employee_ID');
    this.onChange();
  }
  //#endregion

  //#region onChange
  onChange() {
    this.resetEmployeeData();
    this.getEmployeeID();
    if (this.data.factory && this.data.employee_ID) {
      this.spinnerService.show();
      this.service.getEmployeeData(this.data.factory, this.att_Month ? new Date(this.att_Month).toFirstDateOfMonth().toStringDate() : '', this.data.employee_ID).subscribe({
        next: (res) => {
          if (res.data) {
            this.data.useR_GUID = res.data.useR_GUID;
            this.data.division = res.data.division;
            this.data.local_Full_Name = res.data.local_Full_Name;
            this.data.department = res.data.department;
            this.data.leaves = res.data.leaves;
            this.data.allowances = res.data.allowances;
            this.data.salary_Type = res.data.salary_Type;
            if (!this.isQuery && !this.isEdit)
              this.data.permission_Group = res.data.permission_Group;
          } else {
            this.resetEmployeeData();
            this.functionUtility.snotifySuccessError(false, res.error);
            if (this.isEdit || this.isQuery) this.back();
          }
          this.spinnerService.hide();
        }
      });
    }
  }

  resetEmployeeData() {
    this.data.useR_GUID = '';
    this.data.local_Full_Name = '';
    this.data.department = '';
    this.data.permission_Group = '';
    this.data.salary_Type = '';
    this.data.leaves = [];
    this.data.allowances = [];
  }

  onTypehead(e: TypeaheadMatch) {
    this.data.employee_ID = e.value;
  }

  onEmployeeIDChange(value: string) {
    this.data.employee_ID = value;
  }

  onBlur() {
    setTimeout(() => {
      this.onChange();
    }, 200);
  }
  //#endregion

  //#region save
  save() {
    this.data.att_Month = this.att_Month ? new Date(this.att_Month).toFirstDateOfMonth().toStringDate() : '';
    const observable = this.isEdit ? this.service.edit(this.data) : this.service.add(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? message : result.error)
        if (result.isSuccess) {
          this.back();
        }
      }
    });
  }
  //#endregion

  back = () => this.router.navigate([this.url]);

  deleteProperty(name: string) {
    delete this.data[name]
  }

  checkEmpty() {
    const { data, functionUtility } = this;
    const commonChecks = [
      functionUtility.checkEmpty(data.factory),
      functionUtility.checkEmpty(this.att_Month),
      functionUtility.checkEmpty(data.employee_ID),
      functionUtility.checkEmpty(data.local_Full_Name),
      functionUtility.checkEmpty(data.resign_Status),
      functionUtility.checkEmpty(data.update_By),
      functionUtility.checkEmpty(data.update_Time),
    ];

    return commonChecks.some(check => check);
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  //#region validate
  validateNumber(event: KeyboardEvent) {
    const inputChar = event.key;
    const numberRegex = /^[0-9]$/;
    const inputValue = (event.target as HTMLInputElement).value + inputChar;

    if (!numberRegex.test(inputChar) ||
      (parseInt(inputValue) > 2147483647)) {
      event.preventDefault();
    }
  }

  validateDecimal(event: any): boolean {
    const inputChar = event.key;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.indexOf(inputChar) !== -1)
      return true;

    const currentValue = event.target.value;
    if (currentValue === '' && inputChar === '.') {
      event.preventDefault();
      return false;
    }
    const newValue = currentValue.substring(0, event.target.selectionStart) + inputChar + currentValue.substring(event.target.selectionEnd);
    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (integerPartLength > 5 || decimalPartLength > 5) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^[0-9]*(\.[0-9]{0,5})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    return true;
  }

  onFocusNumber(field: string, item?: any) {
    if (item) {
      if (item[field] === 0)
        item[field] = '';
    } else {
      if (this.data[field] === 0)
        this.data[field] = '';
    }
  }

  onBlurNumber(field: string, item?: any) {
    if (item) {
      let value = parseFloat(item[field]);
      if (isNaN(value))
        item[field] = 0;
    } else {
      let value = parseFloat(this.data[field]);
      if (isNaN(value))
        this.data[field] = 0;
    }
  }

  validateDate(dateString: Date) {
    const date = new Date(dateString);
    if (isNaN(date.getTime()))
      if (dateString === this.att_Month)
        this.att_Month = null;
  }
  //#endregion
}
