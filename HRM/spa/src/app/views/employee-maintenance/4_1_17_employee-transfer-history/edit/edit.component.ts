import { Component, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { EmployeeTransferHistoryDTO } from '@models/employee-maintenance/4_1_17_employee-transfer-history';
import { S_4_1_17_EmployeeTransferHistoryService } from '@services/employee-maintenance/s_4_1_17_employee-transfer-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html',
  styleUrls: ['./edit.component.css'],
})
export class EditComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  param: EmployeeTransferHistoryDTO = <EmployeeTransferHistoryDTO>{};
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedFactoryAfter: KeyValuePair[] = [];
  listAssignedDepartmentAfter: KeyValuePair[] = [];
  listAssignedDepartmentBefore: KeyValuePair[] = [];
  listPositionTitleAfter: KeyValuePair[] = [];
  listPositionTitleBefore: KeyValuePair[] = [];
  listAssignedDivisionAfter: KeyValuePair[] = [];
  listPositionTitle: KeyValuePair[] = [];
  listReasonforChange: KeyValuePair[] = [];
  listPositionGrade: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listDataSources: KeyValuePair[] = [];
  maxDateEffective: Date = new Date();
  iconButton = IconButton;
  classButton = ClassButton;
  isCheckedEffectiveDate: boolean = false
  constructor(
    private service: S_4_1_17_EmployeeTransferHistoryService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    this.getDataFromSource();
  }
  getDataFromSource() {
    effect(() => {
      let source = this.service.basicCodeSource();
      this.param = { ...source.basicCode };
      if (Object.keys(this.param).length == 0)
        this.back();
      else {
        this.transferData()
        this.loadData()
      }
    })
  }
  private loadData() {
    this.getDataSources();
    this.getListFactory()
    this.getListDepartment()
    this.getListAssignedFactoryAfter()
    this.getListPositionTitleBefore();
    this.getListPositionTitleAfter()
    this.getListAssignedDepartmentAfter(true)
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
    this.getListData('listAssignedDivisionAfter', this.service.getListAssignedDivisionAfter.bind(this.service));
    this.getListData('listPositionGrade', this.service.getListPositionGrade.bind(this.service));
    this.getListData('listWorkType', this.service.getListWorkType.bind(this.service));
    this.getListData('listReasonforChange', this.service.getListReasonforChange.bind(this.service));
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      },
    });
  }

  getDataSources() {
    this.service.getListDataSource().subscribe({
      next: res => {
        this.listDataSources = res;
      },
    })
  }

  //#region  get value param
  getListFactory() {
    this.service.getListFactory(this.param.factory_After).subscribe({
      next: (res) => {
        this.listFactory = res;
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

  getListAssignedDepartmentAfter(isCheck?: boolean) {
    let assigned_Factory: string;
    let assigned_Division: string;
    if (isCheck) {
      assigned_Factory = this.param.assigned_Factory_After != null ? this.param.assigned_Factory_After.split('-')[0].trim() : "";
      assigned_Division = this.param.assigned_Division_After != null ? this.param.assigned_Division_After.split('-')[0].trim() : "";
    }
    else {
      assigned_Factory = this.param.assigned_Factory_After
      assigned_Division = this.param.assigned_Division_After
    }

    this.service.getListDepartmentAfter(assigned_Factory, assigned_Division).subscribe({
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
  onFactoryChange() {
    this.getListFactory()
    this.deleteProperty('factory_After')
    this.deleteProperty('department_After')
  }

  onDepartmentChange() {
    this.getListDepartment();
    this.deleteProperty('department_After')
  }

  onAssignedSupportedFactoryChange() {
    this.getListAssignedFactoryAfter()
  }

  onAssignedSupportedDepartmentChange() {
    this.getListAssignedDepartmentAfter(false)
  }

  onPositionTitle() {
    this.getListPositionTitleAfter()
    this.deleteProperty('position_Title_After')
  }
  //#endregion

  back() {
    this.router.navigate([this.url]);
  }
  onDateChange(name: string, value: Date) {
    this.param[name] = value
    this.param[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.param[name])) && this.param[name] != null
      ? this.functionUtility.getDateFormat(new Date(this.param[name]))
      : '';
    if (name == 'effective_Date')
      this.isCheckedEffectiveDate = this.param.data_Source == '01' ? this.param.effective_Date?.getTime() >= this.maxDateEffective.getTime() : true
  }

  transferData() {
    this.param.assigned_Department_After = this.param.assigned_Department_After?.split('-')[0].trim()
    this.param.assigned_Department_Before = this.param.assigned_Department_Before?.split('-')[0].trim()
    this.param.assigned_Division_After = this.param.assigned_Division_After?.split('-')[0].trim()
    this.param.assigned_Division_Before = this.param.assigned_Division_Before?.split('-')[0].trim()
    this.param.assigned_Factory_After = this.param.assigned_Factory_After?.split('-')[0].trim()
    this.param.assigned_Factory_Before = this.param.assigned_Factory_Before?.split('-')[0].trim()
    this.param.department_After = this.param.department_After?.split('-')[0].trim()
    this.param.department_Before = this.param.department_Before?.split('-')[0].trim()
    this.param.division_After = this.param.division_After?.split('-')[0].trim()
    this.param.division_Before = this.param.division_Before?.split('-')[0].trim()
    this.param.factory_After = this.param.factory_After?.split('-')[0].trim()
    this.param.factory_Before = this.param.factory_Before?.split('-')[0].trim()
    this.param.position_Title_After = this.param.position_Title_After?.split('-')[0].trim()
    this.param.position_Title_Before = this.param.position_Title_Before?.split('-')[0].trim()
    this.param.reason_for_Change = this.param.reason_for_Change?.split('-')[0].trim()
    this.param.work_Type_After = this.param.work_Type_After?.split('-')[0].trim()
    this.param.work_Type_Before = this.param.work_Type_Before?.split('-')[0].trim()
  }
  update() {
    this.spinnerService.show();
    this.service.update(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess)
          this.back();
      },

    })
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
  isValidDate(d: Date) {
    return d instanceof Date && !isNaN(d.getTime());
  }
}
