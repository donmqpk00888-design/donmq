import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { EmployeeGroupSkillSettings_Main, EmployeeGroupSkillSettings_SkillDetail } from '@models/employee-maintenance/4_1_8_employee-group-skill-settings';
import { S_4_1_8_EmployeeGroupSkillSettings } from '@services/employee-maintenance/s_4_1_8_employee-group-skill-settings.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable, Observer, map, mergeMap, tap } from 'rxjs';
import { ModalService } from '@services/modal.service';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = ''
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;
  selectedData: EmployeeGroupSkillSettings_Main = <EmployeeGroupSkillSettings_Main>{ skill_Detail_List: [] }

  url: string = '';
  action: string = '';

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  productionList: KeyValuePair[] = [];
  performanceList: KeyValuePair[] = [];
  technicalList: KeyValuePair[] = [];
  expertiseList: KeyValuePair[] = [];
  employeeList$: Observable<KeyValuePair[]>;

  constructor(
    private service: S_4_1_8_EmployeeGroupSkillSettings,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave)
        this.selectedData.skill_Detail_List = res.data as EmployeeGroupSkillSettings_SkillDetail[]
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.action = role.title
        this.filterList(role.dataResolved)
      })
    this.service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.action == 'Edit') {
        res == null
          ? this.back()
          : this.selectedData = res
      }
    })
    this.employeeList$ = new Observable((observer: Observer<any>) => {
      observer.next({
        factory: this.selectedData.factory,
        employee_Id: this.selectedData.employee_Id
      });
    }).pipe(mergeMap((_param: any) =>
      this.service.getEmployeeList(_param.factory, _param.employee_Id)
        .pipe(
          map((data: KeyValuePair[]) => data || []),
          tap(res => this.selectedData.local_Full_Name = res.length == 1 && _param.employee_Id == res[0].key ? res[0].value : null)
        ))
    );
  }
  onTypehead(e: TypeaheadMatch): void {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, 'System.Message.InvalidEmployeeIDLength')
    this.selectedData.local_Full_Name = e.item.value
  }
  retryGetDropDownList() {
    this.service.getDropDownList(this.selectedData.division)
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
  }
  skillDetail() {
    this.modalService.open(this.selectedData, 'modify-modal');
  }

  onEmployeeChange() {
    if (this.functionUtility.checkEmpty(this.selectedData.employee_Id))
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
  deleteProperty(name: string) {
    delete this.selectedData[name]
  }
  save() {
    this.spinnerService.show();
    if (this.action == 'Add') {
      this.service
        .postData(this.selectedData)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess,
              res.isSuccess ? 'System.Message.CreateOKMsg' : `EmployeeInformationModule.EmployeeGroupSkillSettings.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
    else {
      this.service
        .putData(this.selectedData)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess,
              res.isSuccess ? 'System.Message.UpdateOKMsg' : `EmployeeInformationModule.EmployeeGroupSkillSettings.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
  }
  back = () => this.router.navigate([this.url]);
}
