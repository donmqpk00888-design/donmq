import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import {
  DocumentManagement_ModalInputModel,
  DocumentManagement_SubData,
  DocumentManagement_SubMemory,
  DocumentManagement_SubParam,
  DocumentManagement_TypeheadKeyValue
} from '@models/employee-maintenance/4_1_9_document-management';
import { S_4_1_9_DocumentManagement } from '@services/employee-maintenance/s_4_1_9_document-management.service';
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
  title: string = ''
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;

  param: DocumentManagement_SubParam = <DocumentManagement_SubParam>{}
  data: DocumentManagement_SubData[] = []
  selectedData: DocumentManagement_SubData = <DocumentManagement_SubData>{ file_List: [] }

  url: string = '';
  action: string = '';

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  documentList: KeyValuePair[] = [];
  employeeList$: Observable<DocumentManagement_TypeheadKeyValue[]>;

  isAllow: boolean = false

  constructor(
    private service: S_4_1_9_DocumentManagement,
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
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
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
          map((data: DocumentManagement_TypeheadKeyValue[]) => data || []),
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
      return this.functionUtility.snotifySuccessError(false, 'System.Message.InvalidEmployeeIDLength')
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
    this.documentList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DT")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getSubDetail = () => {
    this.spinnerService.show();
    this.service
      .getSubDetail(this.param)
      .subscribe({
        next: (res) => {
          this.data = res.data;
          this.data.map((val: DocumentManagement_SubData) => {
            val.validity_Date_From = new Date(val.validity_Date_From);
            val.validity_Date_To = new Date(val.validity_Date_To);
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
  onDocumentTypeChange(item: DocumentManagement_SubData) {
    delete item['passport_Full_Name']
    this.editData(item)
  }
  save() {
    this.spinnerService.show();
    this.data.map(x => {
      x.validity_Date_From_Str = x.validity_Date_From.toStringDateTime()
      x.validity_Date_To_Str = x.validity_Date_To.toStringDateTime()
      x.update_Time_Str = x.update_Time.toStringDateTime()
    })
    const _data: DocumentManagement_SubMemory = <DocumentManagement_SubMemory>{
      param: this.param,
      data: this.data
    }
    if (this.action == 'Add') {
      this.service
        .postData(_data)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide()
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? `System.Message.CreateOKMsg` : `EmployeeInformationModule.DocumentManagement.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
    else {
      this.service
        .putData(_data)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide()
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? `System.Message.UpdateOKMsg` : `EmployeeInformationModule.DocumentManagement.${res.error}`)
            if (res.isSuccess) this.back()
          }
        })
    }
  }
  add() {
    this.checkData(true)
    if (this.isAllow) {
      this.data.push(<DocumentManagement_SubData>{
        file_List: []
      })
      this.calculateSeq()
    }
    this.isAllow = false
  }
  remove(item: DocumentManagement_SubData) {
    this.data = this.data.filter(val => val.seq != item.seq)
    this.calculateSeq()
    this.checkData(false)
  }
  calculateSeq() {
    const now = new Date
    this.data.map((val, ind) => {
      if (val.seq != ind + this.param.max_Seq + 1) {
        val.seq = ind + this.param.max_Seq + 1
        val.update_By = this.user.id
        val.update_Time = now
      }
    })
  }
  editData(item: DocumentManagement_SubData) {
    item.update_By = this.user.id
    item.update_Time = new Date
    this.checkData(false)
  }
  checkData(onAdd: boolean) {
    if (this.data.length > 0 && this.data.filter(val =>
      !val.document_Type ||
      (val.document_Type == '01' ? !val.passport_Full_Name : false) ||
      !val.document_Number ||
      !val.validity_Date_From ||
      !val.validity_Date_To
    ).length > 0) {
      this.isAllow = false
      if (onAdd)
        return this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.DocumentManagement.AddEmptyError')
    }
    else this.isAllow = true
  }
  back = () => this.router.navigate([this.url]);

  attachment(e: DocumentManagement_SubData) {
    this.selectedData = e
    const data: DocumentManagement_ModalInputModel = <DocumentManagement_ModalInputModel>{
      division: this.param.division,
      factory: this.param.factory,
      ser_Num: e.ser_Num,
      file_List: this.selectedData.file_List
    }
    this.modalService.open(data, 'modify-modal')
  }
}
