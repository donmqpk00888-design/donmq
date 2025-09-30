import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { CaptionConstants } from '@constants/message.enum';
import { ResignedEmployeeDetail, ResignedEmployeeDetailParam, ResignedEmployeeParam }
  from '@models/attendance-maintenance/5_1_24_monthly-attendance-resigned-employees';
import { S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService }
  from '@services/attendance-maintenance/s_5_1_24_monthly-attendance-data-maintenance-resigned-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  data: ResignedEmployeeDetail = <ResignedEmployeeDetail>{
    factory: '',
    pass: 'N',
    department: '',
    permission_Group: '',
    salary_Type: '',
    resign_Status: 'N',
    probation: 'N',
    isProbation: false,
    division: ''
  };
  DepartmentNameCode: string = '';
  param: ResignedEmployeeParam = <ResignedEmployeeParam>{};

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  salaryTypes: KeyValuePair[] = [];

  iconButton = IconButton;
  classButton = ClassButton;

  title: string = '';
  url: string = '';
  action: string = '';
  language: string = localStorage.getItem(LocalStorageConstants.LANG);
  employeeIDs: KeyValuePair[] = [];
  passCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ]
  probationCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];
  resignStatusCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ]

  invalid: boolean = false;

  constructor(
    private _service: S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      if (this.param.action === 'add')
        this.getListFactoryAdd();
      this.getEmpInfo(true);
      this.getListPermissionGroup();
      this.getListSalaryType();
    });

    let state = this.router.getCurrentNavigation()?.extras?.state;

    if (!state)
      this.back();

    else {
      this.param = state['param'];
      this.param.language = this.language;

      this.factories = state['factories'];
      this.bsConfig = state['bsConfig'];
    }
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    if (this.param && Object.keys(this.param).length !== 0) {
      if (this.param.action !== 'add') {
        this.query();
      }
      else {
        this.getListFactoryAdd();
        this.data.salary_Type = '10';

        this.data.delay_Early = 0;
        this.data.no_Swip_Card = 0;
        this.data.food_Expenses = 0;
        this.data.night_Eat_Times = 0;

        this.data.dayShift_Food = 0;
        this.data.nightShift_Food = 0;
      }

      this.getListPermissionGroup();
      this.getListSalaryType();
    }

    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
    });
  }

  back = () => this.router.navigate([this.url]);

  onOpenCalendar(container) {
    container.monthSelectHandler = (event: any): void => {
      container._store.dispatch(container._actions.select(event.date));
    };

    container.setViewMode('month');
  }
  onEmployeeIDChange(value: string) {
    this.data.employee_ID = value;
  }
  onTypehead(e: TypeaheadMatch) {
    this.data.employee_ID = e.value;
    this.getEmpInfo()
  }
  onBlur() {
    setTimeout(() => {
      this.getEmpInfo();
    }, 200);
  }
  getEmpInfo(isMonthly: boolean = false) {
    if (this.functionUtility.checkEmpty(this.data.employee_ID) || this.functionUtility.checkEmpty(this.data.factory)) {
      this.DepartmentNameCode = '';
      this.data.local_Full_Name = '';
      this.data.permission_Group = '';
      this.data.leaves = [];
      this.data.allowances = []
      return;
    }

    let param: ResignedEmployeeParam = <ResignedEmployeeParam>{
      factory: this.data.factory,
      employee_ID: this.data.employee_ID,
      att_Month_Start: new Date(this.data.att_Month).toStringDate(),
      language: this.language,
      isMonthly: isMonthly
    }

    this.spinnerService.show();
    this._service.getEmpInfo(param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.invalid = false;
            this.data.useR_GUID = res.data.useR_GUID;
            this.DepartmentNameCode = res.data.department_Name;
            this.data.department = res.data.department_Code;
            this.data.local_Full_Name = res.data.local_Full_Name;
            this.data.permission_Group = res.data.permission_Group;
            this.data.division = res.data.division;
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(res.error),
              this.translateService.instant('System.Caption.Error'));
            this.data.useR_GUID = '';
            this.data.local_Full_Name = '';
            this.DepartmentNameCode = '';
            this.data.department = '';
            this.data.permission_Group = '';
            this.data.division = '';
            this.data.leaves = [];
            this.data.allowances = []
            this.invalid = true;
          }
          this.getResignedDetail()
        }
      });
  }

  query() {
    this.spinnerService.show();
    this.param.isMonthly = this.param.action == "edit" ? true : false;
    this._service.query(this.param).subscribe({
      next: (res) => {
        if (res) {
          this.data = res;
          if (res.dayShift_Food == null) this.data.dayShift_Food = 0;
          if (res.nightShift_Food == null) this.data.nightShift_Food = 0;

          this.DepartmentNameCode = res.department_Name;

          if (this.data.department || this.data.useR_GUID)
            this.invalid = false;
        }
        this.spinnerService.hide();
      }
    });
  }

  getResignedDetail() {
    if (this.functionUtility.checkEmpty(this.data.employee_ID) || this.functionUtility.checkEmpty(this.data.factory)
      || this.functionUtility.checkEmpty(this.data.useR_GUID) || this.functionUtility.checkEmpty(this.data.att_Month)) {
      this.data.leaves = []
      this.data.allowances = []
      return;
    }
    let params: ResignedEmployeeDetailParam = <ResignedEmployeeDetailParam>{
      factory: this.data.factory,
      att_Month: new Date(this.data.att_Month).toStringDate(),
      language: this.language,
      employee_ID: this.data.employee_ID,
      uSER_GUID: this.data.useR_GUID
    }

    this.spinnerService.show();
    this._service.getResignedDetail(params)
      .subscribe({
        next: (res: any) => {
          if (res) {
            this.data.leaves = res.leaves;
            this.data.allowances = res.allowances;
          }

          this.spinnerService.hide();
        }
      });
  }

  getListFactoryAdd() {
    this.factories = [];
    this._service.getListFactoryAdd()
      .subscribe({
        next: (res) => {
          this.factories = res;
        }
      });
  }

  getListPermissionGroup() {
    this._service.getListPermissionGroup().subscribe({
      next: (res) => {
        this.permissionGroups = res;
      }
    });
  }
  onFactoryChange() {
    this.data.employee_ID = '';
    this.data.local_Full_Name = '';
    this.DepartmentNameCode = '';
    this.data.permission_Group = '';
    this.data.leaves = [];
    this.data.allowances = [];
    this._service.getEmployeeIDByFactorys(this.data.factory).subscribe({
      next: res => {
        this.employeeIDs = res;
      }
    })
  }
  getListSalaryType() {
    this._service.getListSalaryType().subscribe({
      next: (res) => {
        this.salaryTypes = res;
      }
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
    this._service[this.param.action](this.data)
      .subscribe({
        next: (res: OperationResult) => {
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
          this.spinnerService.hide();
        }
      })
  }

  disableDetail() {
    return this.param.action === 'query';
  }

  disableEditOrDetail() {
    return this.param.action === 'edit' || this.param.action === 'query';
  }

  close() {
    this.back();
  }
}
