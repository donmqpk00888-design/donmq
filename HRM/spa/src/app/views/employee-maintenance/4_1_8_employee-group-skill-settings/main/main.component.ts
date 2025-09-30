import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { EmployeeGroupSkillSettings_Main, EmployeeGroupSkillSettings_MainMemory, EmployeeGroupSkillSettings_Param, EmployeeGroupSkillSettings_SkillDetail } from '@models/employee-maintenance/4_1_8_employee-group-skill-settings';
import { S_4_1_8_EmployeeGroupSkillSettings } from '@services/employee-maintenance/s_4_1_8_employee-group-skill-settings.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable, Observer, map, mergeMap, tap } from 'rxjs';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  pagination: Pagination = <Pagination>{};

  iconButton = IconButton;
  classButton = ClassButton;

  data: EmployeeGroupSkillSettings_Main[] = [];
  param: EmployeeGroupSkillSettings_Param = <EmployeeGroupSkillSettings_Param>{};

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  productionList: KeyValuePair[] = [];
  performanceList: KeyValuePair[] = [];
  technicalList: KeyValuePair[] = [];
  expertiseList: KeyValuePair[] = [];
  skillList: KeyValuePair[] = [];

  employeeList$: Observable<KeyValuePair[]>;
  title: string

  constructor(
    private service: S_4_1_8_EmployeeGroupSkillSettings,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data
      this.loadData()
    });
  }
  private loadData() {
    this.retryGetDropDownList()
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getData(false)
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    let result = !this.functionUtility.checkEmpty(this.param.division) && !this.functionUtility.checkEmpty(this.param.factory)
    return result
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.filterList(role.dataResolved)
      });
    this.employeeList$ = new Observable((observer: Observer<any>) => {
      observer.next({
        factory: this.param.factory,
        employee_Id: this.param.employee_Id
      });
    }).pipe(mergeMap((_param: any) =>
      this.service.getEmployeeList(_param.factory, _param.employee_Id)
        .pipe(
          map((data: KeyValuePair[]) => data || []),
          tap(res => this.param.local_Full_Name = res.length == 1 && _param.employee_Id == res[0].key ? res[0].value : null)
        ))
    );
  }
  onTypehead(e: TypeaheadMatch): void {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, `System.Message.InvalidEmployeeIDLength`)
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<EmployeeGroupSkillSettings_MainMemory>{ param: this.param, pagination: this.pagination, data: this.data });
  }
  retryGetDropDownList() {
    this.service.getDropDownList(this.param.division)
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.divisionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.productionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "PR")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.performanceList = structuredClone(keys.filter((x: { key: string; }) => x.key == "PE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.technicalList = structuredClone(keys.filter((x: { key: string; }) => x.key == "TE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.expertiseList = structuredClone(keys.filter((x: { key: string; }) => x.key == "EX")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.skillList = structuredClone(keys.filter((x: { key: string; }) => x.key == "SK")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getData = (isSearch: boolean) => {
    this.spinnerService.show();
    this.service
      .getSearchDetail(this.pagination, this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.pagination = res.pagination;
          this.data = res.result;
          this.data.map((val: EmployeeGroupSkillSettings_Main) => {
            val.skill_Detail_List.map((skill: EmployeeGroupSkillSettings_SkillDetail) => {
              skill.passing_Date = new Date(skill.passing_Date);
            })
          })
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.SearchOKMsg')
        }
      });
  };
  search = () => {
    this.pagination.pageNumber == 1
      ? this.getData(true)
      : (this.pagination.pageNumber = 1);
  };
  clear() {
    this.param = <EmployeeGroupSkillSettings_Param>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(e: EmployeeGroupSkillSettings_Main) {
    this.spinnerService.show();
    this.service.checkExistedData(e.division, e.factory, e.employee_Id)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.service.setParamForm(e);
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
          }
          else {
            this.getData(false)
            this.functionUtility.snotifySuccessError(true,'EmployeeInformationModule.EmployeeGroupSkillSettings.NotExitedData')
          }
        }
      });

  }
  remove(e: EmployeeGroupSkillSettings_Main) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.deleteData(e.division, e.factory, e.employee_Id).subscribe({
        next: (res) => {
          this.functionUtility.snotifySuccessError(res.isSuccess, `System.Message.${res.isSuccess ? 'DeleteOKMsg' : `EmployeeInformationModule.EmployeeGroupSkillSettings.${res.error}`}`)
          if (res.isSuccess)
            this.getData(false);
          this.spinnerService.hide();
        }
      });
    });
  }
  skillDetail(item: EmployeeGroupSkillSettings_Main) {
    this.modalService.open(item, 'view-modal');
  }
  onEmployeeChange() {
    if (this.functionUtility.checkEmpty(this.param.employee_Id))
      this.deleteProperty('local_Full_Name')
  }
  onFactoryChange() {
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
  }
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
