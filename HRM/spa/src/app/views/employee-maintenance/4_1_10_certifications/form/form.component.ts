import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import {
  Certifications_ModalInputModel,
  Certifications_SubData,
  Certifications_SubMemory,
  Certifications_SubParam,
  Certifications_TypeheadKeyValue
} from '@models/employee-maintenance/4_1_10_certifications';
import { S_4_1_10_Certifications } from '@services/employee-maintenance/s_4_1_10_certifications.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable, Observer, map, mergeMap, tap } from 'rxjs';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;

  param: Certifications_SubParam = <Certifications_SubParam>{}
  data: Certifications_SubData[] = []
  selectedData: Certifications_SubData = <Certifications_SubData>{ file_List: [] }

  url: string = '';
  action: string = '';

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  categoryList: KeyValuePair[] = [];
  employeeList$: Observable<Certifications_TypeheadKeyValue[]>;

  isAllow: boolean = false

  constructor(
    private activatedRoute: ActivatedRoute,
    private service: S_4_1_10_Certifications,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((resp) => {
      if (resp.isSave) {
        this.selectedData.file_List = resp.data.file_List
        this.selectedData.update_By = this.user.id
        this.selectedData.update_Time = new Date
        this.checkData(false)
      }
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM/DD',
      }
    );
    this.activatedRoute.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.action = role.title
        this.filterList(role.dataResolved)
      })
    this.service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.action == 'Edit') {
        if (res == null)
          this.back()
        else {
          this.param = res
          this.getSubDetail()
        }
      }
    })
    this.employeeList$ = new Observable((observer: Observer<any>) => {
      observer.next({
        division: this.param.division,
        factory: this.param.factory,
        employee_Id: this.param.employee_Id
      });
    }).pipe(mergeMap((_param: any) =>
      this.service.getEmployeeList(_param.division, _param.factory, _param.employee_Id)
        .pipe(
          map((data: Certifications_TypeheadKeyValue[]) => data || []),
          tap(res => {
            if (res.length == 1 && _param.employee_Id == res[0].key) {
              this.param.local_Full_Name = res[0].local_Full_Name
              this.param.max_Seq = res[0].max_Seq
              this.calculateSeq()
            }
            else {
              this.param.local_Full_Name = null
              this.param.max_Seq = 0
            }
          })
        ))
    );
  }
  onTypehead(e: TypeaheadMatch): void {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, `System.Message.InvalidEmployeeIDLength`)
    this.param.local_Full_Name = e.item.local_Full_Name
    this.param.max_Seq = e.item.max_Seq
    this.calculateSeq()
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
    this.categoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "CA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getSubDetail = () => {
    this.spinnerService.show();
    this.service
      .getSubDetail(this.param)
      .subscribe({
        next: (res) => {
          this.data = res.data;
          this.data.map((val: Certifications_SubData) => {
            val.passing_Date = new Date(val.passing_Date);
            val.certification_Valid_Period = val.certification_Valid_Period != null ? new Date(val.certification_Valid_Period) : null;
            val.update_Time = new Date(val.update_Time);
          })
          this.checkData(false)
          this.spinnerService.hide();
        }
      });
  };
  deleteProperty(name: string) {
    delete this.param[name]
  }
  onEmployeeChange() {
    if (this.functionUtility.checkEmpty(this.param.employee_Id))
      this.deleteProperty('local_Full_Name')
  }
  onFactoryChange() {
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
    this.data = []
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
    this.data = []
  }
  save() {
    this.spinnerService.show();
    this.data.map(x => {
      x.passing_Date_Str = x.passing_Date.toStringDateTime()
      x.certification_Valid_Period_Str = x.certification_Valid_Period != undefined ? x.certification_Valid_Period.toStringDateTime() : ''
      x.update_Time_Str = x.update_Time.toStringDateTime()
    })
    const _data: Certifications_SubMemory = <Certifications_SubMemory>{
      param: this.param,
      data: this.data
    }
    if (this.action == 'Add') {
      this.service
        .postData(_data)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.CreateOKMsg' : `EmployeeInformationModule.Certifications.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
    else {
      this.service
        .putData(_data)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : `EmployeeInformationModule.Certifications.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
  }
  add() {
    this.checkData(true)
    if (this.isAllow) {
      this.data.push(<Certifications_SubData>{
        result: true,
        file_List: []
      })
      this.calculateSeq()
    }
    this.isAllow = false
  }
  remove(item: Certifications_SubData) {
    this.data = this.data.filter(val => val.seq != item.seq)
    this.calculateSeq()
    this.checkData(false)
  }
  calculateSeq() {
    const now = new Date
    const maxSeq = this.param.max_Seq || 0;
    this.data.forEach((val, ind) => {
      const newSeq = ind + maxSeq + 1;
      if (val.seq !== newSeq) {
        val.seq = newSeq;
        val.update_By = this.user.id;
        val.update_Time = now;
      }
    });
  }
  editData(item: Certifications_SubData) {
    item.update_By = this.user.id
    item.update_Time = new Date
    this.checkData(false)
  }
  checkData(onAdd: boolean) {
    if (this.data.length > 0 && this.data.filter(val =>
      !val.category_Of_Certification ||
      !val.name_Of_Certification ||
      !val.passing_Date
    ).length > 0) {
      this.isAllow = false
      if (onAdd)
        return this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.Certifications.AddEmptyError')
    }
    else this.isAllow = true
  }
  back = () => this.router.navigate([this.url]);

  attachment(e: Certifications_SubData) {
    this.selectedData = e
    const data: Certifications_ModalInputModel = <Certifications_ModalInputModel>{
      division: this.param.division,
      factory: this.param.factory,
      ser_Num: e.ser_Num,
      file_List: this.selectedData.file_List
    }
    this.modalService.open(data, 'modify-modal')
  }
}
