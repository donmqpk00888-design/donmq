import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { CaptionConstants } from '@constants/message.enum';
import {
  ActiveEmployeeParam,
  MaintenanceActiveEmployeesDetail,
  MaintenanceActiveEmployeesDetailParam,
} from '@models/attendance-maintenance/5_1_22_monthly-attendance-data-maintenance-active-employees';
import { S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_22_monthly-attendance-data-maintenance-active-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss',
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: MaintenanceActiveEmployeesDetailParam = <MaintenanceActiveEmployeesDetailParam>{};
  title: string = '';
  url: string = '';
  action: string = '';
  invalid: boolean = true;
  data: MaintenanceActiveEmployeesDetail = <MaintenanceActiveEmployeesDetail>{
    factory: '',
    pass: 'N',
    resign_Status: 'N',
    probation: 'N',
    isProbation: false,
    salary_Type: '10',

    delay_Early: 0,
    no_Swip_Card: 0,
    food_Expenses: 0,
    night_Eat_Times: 0,
    dayShift_Food: 0,
    nightShift_Food: 0

  };
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  employeeIDs: KeyValuePair[] = [];
  resignStatusCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];
  probationCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];
  passStatusCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];

  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getListFactoryAdd();
        this.getEmpInfo(this.data.employee_ID);
        this.getLeaveAllowance();
        this.getListPermissionGroup();
        this.getListSalaryType();
      });

    let state = this.router.getCurrentNavigation()?.extras?.state;

    if (!state) this.back();
    else {
      this.param = state['param'];
      this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
    }
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    if (this.param && Object.keys(this.param).length !== 0) {
      if (this.param.action !== 'add') {
        this.getDetail();
      }
      this.getListFactoryAdd();
      this.getListPermissionGroup();
      this.getListSalaryType();
    }

    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
    });
  }

  getDetail() {
    this.spinnerService.show();
    this._service.getDetail(this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res) {
            this.data = res;
            // Set default value
            if (res.dayShift_Food == null) this.data.dayShift_Food = 0;
            if (res.nightShift_Food == null) this.data.nightShift_Food = 0;

            this.getEmpInfo(this.data.employee_ID, this.param.action == "edit" ? true : false);
            this.invalid = false;
          }
        }
      });
  }

  onTypehead(e: TypeaheadMatch) {
    this.data.employee_ID = e.value;
    this.getEmpInfo(e.value)
  }

  onEmployeeIDChange(value: string) {
    this.data.employee_ID = value;
  }

  onBlur() {
    setTimeout(() => {
      this.getEmpInfo(this.data.employee_ID);
    }, 200);
  }

  getEmpInfo(employee_ID: string, isMonthly: boolean = false) {
    if (!employee_ID) {
      this.data.department = '';
      this.data.local_Full_Name = '';
      this.data.permission_Group = '';
      this.data.leaves = [];
      this.data.allowances = [];
      return;
    }
    const params = <ActiveEmployeeParam>{
      factory: this.data.factory,
      employee_ID: employee_ID,
      att_Month: this.data.att_Month.toDate().toStringYearMonth(),
      language: this.param.language,
      isAdd: this.param.action !== "add" ? false : true,
      isMonthly: isMonthly
    }

    this.spinnerService.show();
    this._service
      .getEmpInfo(params)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.invalid = false;
            this.data.useR_GUID = res.data.useR_GUID;
            this.data.department = res.data.department;
            this.data.department_Code = res.data.department_Code;
            this.data.local_Full_Name = res.data.local_Full_Name;
            this.data.division = res.data.division;
            this.data.permission_Group = res.data.permission_Group;
            this.getLeaveAllowance();
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(res.error),
              this.translateService.instant('System.Caption.Error'));
            this.data.local_Full_Name = '';
            this.data.department = '';
            this.data.department_Code = '';
            this.data.permission_Group = '';
            this.data.leaves = [];
            this.data.allowances = [];
            this.invalid = true;
          }
        },
      });
  }

  getLeaveAllowance() {
    this.data.att_Month_Str = this.data.att_Month ? (new Date(this.data.att_Month).toStringYearMonth()) : '';
    if (!this.data.att_Month_Str || !this.data.employee_ID) {
      this.data.leaves = []
      this.data.allowances = []
      return;
    }
    const param = <MaintenanceActiveEmployeesDetailParam>{
      factory: this.data.factory,
      att_month: this.data.att_Month_Str.toDate().toStringYearMonth(),
      employee_ID: this.data.employee_ID,
      isProbation: this.data.isProbation,
      language: this.param.language
    }

    this.spinnerService.show();
    this._service
      .getLeaveAllowance(param)
      .subscribe({
        next: (res: any) => {
          this.spinnerService.hide();
          if (res) {
            this.data.leaves = res.leave;
            this.data.allowances = res.allowance;
          } else {
            this.data.leaves = [];
            this.data.allowances = [];
          }
        },
      });
  }

  save() {
    let success: string = this.param.action === 'add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg';
    let error: string = this.param.action === 'add' ? 'System.Message.CreateErrorMsg' : 'System.Message.UpdateErrorMsg';

    // Convert Data before save
    if (this.functionUtility.checkEmpty(this.data.dayShift_Food)) this.data.dayShift_Food = 0;
    if (this.functionUtility.checkEmpty(this.data.nightShift_Food)) this.data.nightShift_Food = 0;

    this.execute(success, error);
  }

  execute(success: string, error: string) {
    this.spinnerService.show();
    // if (this.param.action === 'edit') {
    //   this.data.department = this.param.department.split(' - ')[0];
    // }
    this._service[this.param.action](this.data)
      .subscribe({
        next: (res: OperationResult) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant(success),
              this.translateService.instant('System.Caption.Success')
            );
            this.back();
          } else {
            this.snotifyService.error(
              this.translateService.instant(res.error ?? error),
              this.translateService.instant('System.Caption.Error')
            );
          }
        }
      })
  }

  back = () => this.router.navigate([this.url]);

  disableDetail() {
    return this.param.action === 'query';
  }

  disableEditOrDetail() {
    return this.param.action === 'edit' || this.param.action === 'query';
  }

  close() {
    this.back();
  }

  onFactoryChange() {
    this.data.employee_ID = this.data.local_Full_Name =
      this.data.department = this.data.permission_Group = '';
    this.data.leaves = [];
    this.data.allowances = [];
    this.getListDepartment();
    this._service.getEmployeeIDByFactorys(this.data.factory).subscribe({
      next: res => {
        this.employeeIDs = res;
      }
    })
  }

  //#region Get List
  getListFactoryAdd() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListPermissionGroup() {
    this._service.getListPermissionGroup().subscribe({
      next: (res) => {
        this.listPermissionGroup = res;
      },
    });
  }

  getListSalaryType() {
    this._service.getListSalaryType().subscribe({
      next: (res) => {
        this.listSalaryType = res;
      },
    });
  }

  getListDepartment() {
    this._service
      .getListDepartment(this.data.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }
  //#endregion
}
