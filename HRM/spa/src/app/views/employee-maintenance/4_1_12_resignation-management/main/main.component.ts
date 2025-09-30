import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Emp_ResignationDto, ResignationManagementParam, ResignationManagementSource } from '@models/employee-maintenance/4_1_12_resignation-management';
import { S_4_1_12_ResignationManagementService } from '@services/employee-maintenance/s_4_1_12_resignation-management.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string  = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  nameFunction: string[] = [];
  iconButton = IconButton;
  data: HRMS_Emp_ResignationDto[] = [];
  param: ResignationManagementParam = <ResignationManagementParam>{}
  sourceItem: HRMS_Emp_ResignationDto
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };
  startDate: Date;
  endDate: Date;

  constructor(
    private service: S_4_1_12_ResignationManagementService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      if (this.data.length > 0)
        this.getData();
    });
    effect(() => {
      const { param, startDate, endDate, data, source, pagination } = this.service.paramSearch();
      this.param = param;
      this.startDate = startDate;
      this.endDate = endDate;
      this.pagination = pagination;
      this.sourceItem = source;
      this.data = data;
      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getListDivision();
        this.getListFactory();
      }
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear()
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListFactory();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<ResignationManagementSource>{
      param: this.param,
      startDate: this.startDate,
      endDate: this.endDate,
      pagination: this.pagination,
      source: this.sourceItem,
      data: this.data
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.param.startDate = this.formatDate(this.startDate);
    this.param.endDate = this.formatDate(this.endDate);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true,'System.Message.QuerySuccess')
      }
    });
  }

  download() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        this.spinnerService.hide();
      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <ResignationManagementParam>{};
    this.startDate = null;
    this.endDate = null;
    this.data = [];
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      }
    });
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  onDivisionChange() {
    this.deleteProperty('factory');
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: HRMS_Emp_ResignationDto) {
    this.sourceItem = { ...item }
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  query(item: HRMS_Emp_ResignationDto) {
    this.sourceItem = { ...item }
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  delete(item: HRMS_Emp_ResignationDto) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) this.getData();
        }
      })
    });
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
