import { Component, EventEmitter, OnDestroy, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import {
  Certifications_MainData,
  Certifications_MainMemory,
  Certifications_MainParam,
  Certifications_ModalInputModel,
  Certifications_SubModel,
  Certifications_SubParam
} from '@models/employee-maintenance/4_1_10_certifications';
import { S_4_1_10_Certifications } from '@services/employee-maintenance/s_4_1_10_certifications.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };

  iconButton = IconButton;
  classButton = ClassButton;

  param: Certifications_MainParam = <Certifications_MainParam>{};
  data: Certifications_MainData[] = [];

  filesPassingEmitter: EventEmitter<Certifications_ModalInputModel> = new EventEmitter<Certifications_ModalInputModel>();

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  categoryList: KeyValuePair[] = [];

  title: string = '';
  programCode: string = '';

  constructor(
    private service: S_4_1_10_Certifications,
    private modalService: ModalService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
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
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM/DD',
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.filterList(role.dataResolved)
      });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<Certifications_MainMemory>{ param: this.param, pagination: this.pagination, data: this.data });
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
  getData = (isSearch: boolean) => {
    this.spinnerService.show();
    this.service
      .getSearchDetail(this.pagination, this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.pagination = res.pagination;
          this.data = res.result;
          this.data.map((val: Certifications_MainData) => {
            val.passing_Date = new Date(val.passing_Date);
            val.certification_Valid_Period = val.certification_Valid_Period != null ? new Date(val.certification_Valid_Period) : null;
            val.update_Time = new Date(val.update_Time);
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
    this.param = <Certifications_MainParam>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  edit(e: Certifications_MainData) {
    this.spinnerService.show();
    const data: Certifications_SubModel = <Certifications_SubModel>{
      division: e.division,
      factory: e.factory,
      employee_Id: e.employee_Id,
      seq: e.seq
    }
    this.service.checkExistedData(data)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            const subParam: Certifications_SubParam = <Certifications_SubParam>{
              division: e.division,
              factory: e.factory,
              employee_Id: e.employee_Id,
              local_Full_Name: e.local_Full_Name
            }
            this.service.setParamForm(subParam);
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
          }
          else {
            this.getData(false)
            this.functionUtility.snotifySuccessError(false, `EmployeeInformationModule.Certifications.${res.error}`)
          }
        }
      });

  }
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  remove(e: Certifications_MainData) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      const data: Certifications_SubModel = <Certifications_SubModel>{
        division: e.division,
        factory: e.factory,
        employee_Id: e.employee_Id,
        seq: e.seq
      }
      this.service.deleteData(data).subscribe({
        next: (res) => {
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : `EmployeeInformationModule.Certifications.${res.error}`)
          if (res.isSuccess) this.getData(false);
          this.spinnerService.hide();
        }
      });
    });
  }
  excel() {
    this.spinnerService.show();
    this.service
      .downloadExcel(this.param)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  attachment(e: Certifications_MainData) {
    const data: Certifications_ModalInputModel = <Certifications_ModalInputModel>{
      division: e.division,
      factory: e.factory,
      ser_Num: e.ser_Num,
      employee_Id: e.employee_Id,
      file_List: e.file_List
    }
    this.modalService.open(data, 'view-modal')
  }
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.param[name] ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
}
