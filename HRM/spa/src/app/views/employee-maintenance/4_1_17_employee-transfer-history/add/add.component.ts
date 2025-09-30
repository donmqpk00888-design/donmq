import { Component, OnInit, ViewChild, effect } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeTransferHistoryDTO } from '@models/employee-maintenance/4_1_17_employee-transfer-history';
import { S_4_1_17_EmployeeTransferHistoryService } from '@services/employee-maintenance/s_4_1_17_employee-transfer-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-add',
  templateUrl: './add.component.html',
  styleUrls: ['./add.component.css'],
})
export class AddComponent extends InjectBase implements OnInit {
  @ViewChild('addForm') public addForm: NgForm;
  title: string = '';
  url: string = '';
  param: EmployeeTransferHistoryDTO = <EmployeeTransferHistoryDTO>{ effective_Status: false };
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedFactoryAfter: KeyValuePair[] = [];
  listAssignedDepartmentAfter: KeyValuePair[] = [];
  listAssignedDivisionAfter: KeyValuePair[] = [];
  listPositionTitleAfter: KeyValuePair[] = [];
  listPositionTitleBefore: KeyValuePair[] = [];
  listReasonforChange: KeyValuePair[] = [];
  listPositionGrade: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listDataSources: KeyValuePair[] = [];
  dataTypeaHead: string[] = [];
  maxDateEffective: Date = new Date();

  method: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  isCheckedEmp: boolean = false
  //Get control name list by disabled status
  get necessaryData(): string[] {
    const formControls = this.addForm.form.controls
    const result = Object.keys(formControls)
      .map((x) => { return { key: x, control: formControls[x] } })
      .filter(x => x.control.disabled)
    return result.map(x => x.key);
  }
  constructor(
    private service: S_4_1_17_EmployeeTransferHistoryService,
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDepartment()
      this.getListFactory()
      this.getListAssignedFactoryAfter()
      this.getListAssignedDepartmentAfter()
      this.getDataSources();
      if (this.param.position_Grade_Before != undefined)
        this.getListPositionTitleBefore();

      if (this.param.position_Grade_After != undefined)
        this.getListPositionTitleAfter();

      this.getListData('listDivision', this.service.getListDivision.bind(this.service));
      this.getListData('listAssignedDivisionAfter', this.service.getListAssignedDivisionAfter.bind(this.service));
      this.getListData('listPositionGrade', this.service.getListPositionGrade.bind(this.service));
      this.getListData('listWorkType', this.service.getListWorkType.bind(this.service));
      this.getListData('listReasonforChange', this.service.getListReasonforChange.bind(this.service));
    });
    effect(() => {
      let method = this.service.method();
      if (!this.functionUtility.checkEmpty(method)) {
        this.method = method;
        this.param.data_Source = method == "add" ? "01" : "02";
      } else
        this.back();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListDepartment()
    this.getListFactory()
    this.getListAssignedFactoryAfter()
    this.getListTypeHeadEmployeeID()
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
    this.getListData('listAssignedDivisionAfter', this.service.getListAssignedDivisionAfter.bind(this.service));
    this.getListData('listPositionGrade', this.service.getListPositionGrade.bind(this.service));
    this.getListData('listWorkType', this.service.getListWorkType.bind(this.service));
    this.getListData('listReasonforChange', this.service.getListReasonforChange.bind(this.service));
    this.getDataSources();
  }

  getDataSources() {
    this.service.getListDataSource().subscribe({
      next: res => {
        this.listDataSources = res;
      },
    })
  }

  checkEmployeeID() {
    if (!this.functionUtility.checkEmpty(this.param.employee_ID_After))
      setTimeout(() => {
        this.onDataDetail(true)
      }, 1000);
    else {
      this.isCheckedEmp = false
      this.clearNecessaryData()
    }
  }
  getListData(dataProperty: string, serviceMethod: (language?: string) => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      },
    });
  }
  //#region  get value param
  getListFactory() {
    this.service.getListFactory(this.param.division_After).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  getListTypeHeadEmployeeID() {
    this.service.getListTypeHeadEmployeeID(this.param.factory_After, this.param.division_After).subscribe({
      next: (res) => {
        this.dataTypeaHead = res;
      },
    });
  }
  getListAssignedFactoryAfter() {
    this.service.getListAssignedFactoryAfter(this.param.assigned_Factory_After).subscribe({
      next: (res) => {
        this.listAssignedFactoryAfter = res;
      },
    });
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.factory_After, this.param.division_After).subscribe({
      next: (res) => {
        this.listDepartment = res;
      },
    });
  }
  getListAssignedDepartmentAfter() {
    this.service.getListDepartmentAfter(this.param.assigned_Factory_After, this.param.assigned_Division_After).subscribe({
      next: (res) => {
        this.listAssignedDepartmentAfter = res;
      },
    });
  }
  getListPositionTitleAfter() {
    this.service.getListPositionTitle(this.param.position_Grade_After ?? 0).subscribe({
      next: (res) => {
        this.listPositionTitleAfter = res;
      },
    });
  }

  getListPositionTitleBefore() {
    this.service.getListPositionTitle(this.param.position_Grade_Before).subscribe({
      next: (res) => {
        this.listPositionTitleBefore = res;
      },
    });
  }
  //#endregion

  //#region on change value input
  onDivisionChange() {
    this.getListFactory()
    this.deleteProperty('factory_After')
    this.deleteProperty('department_After')
    this.deleteProperty('employee_ID_After')
    this.clearNecessaryData()
  }

  onFactoryChange() {
    this.getListDepartment();
    this.getListTypeHeadEmployeeID();
    this.deleteProperty('department_After')
    this.deleteProperty('employee_ID_After')
    this.clearNecessaryData()
  }

  onAssignedSupportedFactoryChange() {
    this.getListAssignedFactoryAfter()
  }

  onAssignedSupportedDepartmentChange() {
    this.getListAssignedDepartmentAfter()
  }

  onPositionTitle() {
    this.getListPositionTitleAfter()
    this.deleteProperty('position_Title_After')
  }

  onDataDetail(isCheck?: boolean) {
    this.spinnerService.show();
    this.service.getDataDetail(this.param.division_After, this.param.employee_ID_After, this.param.factory_After)
      .subscribe({
        next: (res) => {
          if (this.functionUtility.checkEmpty(res.useR_GUID)) {
            this.isCheckedEmp = false
            this.clearNecessaryData()
            if (isCheck)
              this.functionUtility.snotifySuccessError(false, 'System.Message.NoData')
          }
          else {
            this.isCheckedEmp = true
            this.getNecessaryData(res);
            this.param.actingPosition_Start_Before = res.actingPosition_Start_Before != null
              ? new Date(res.actingPosition_Start_Before) : null
            this.param.actingPosition_End_Before = res.actingPosition_End_Before != null
              ? new Date(res.actingPosition_End_Before) : null
            this.param.actingPosition_Start_After = res.actingPosition_Start_After != null
              ? new Date(res.actingPosition_Start_After) : null
            this.param.actingPosition_End_After = res.actingPosition_End_After != null
              ? new Date(res.actingPosition_End_After) : null
            this.getListAssignedDepartmentAfter();
            this.getListDepartment();
            this.getListAssignedFactoryAfter();
            this.getListPositionTitleBefore()
          }
          this.spinnerService.hide();
        },
        error: () => {
          this.isCheckedEmp = false
          this.clearNecessaryData()
        },
      })
  }
  //#endregion

  back() {
    this.router.navigate([this.url]);
  }

  save() {
    this.spinnerService.show();
    this.service.create(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
        if (result.isSuccess) this.back();
      },

    })
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
  isValidDate(d: Date) {
    return d instanceof Date && !isNaN(d.getTime());
  }
  onDateChange(name: string, value: Date) {
    this.param[name] = value
    this.param[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.param[name])) ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }
  //Set value to info controls
  getNecessaryData = (input: EmployeeTransferHistoryDTO) => this.necessaryData.map(x => this.param[x] = input[x])
  //Clear value in info controls
  clearNecessaryData = () => this.necessaryData.map(x => this.deleteProperty(x))
}
