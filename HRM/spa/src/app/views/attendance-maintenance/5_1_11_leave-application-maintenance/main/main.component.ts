import { S_5_1_11_Leave_Application_Maintenance } from '@services/attendance-maintenance/s_5_1_11_leave-application-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { Pagination } from '@utilities/pagination-utility';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import {
  LeaveApplicationMaintenance_Main,
  LeaveApplicationMaintenance_MainMemory,
  LeaveApplicationMaintenance_Param
} from '@models/attendance-maintenance/5_1_11_leave-application-maintenance';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ModalService } from '@services/modal.service';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
  };
  acceptFormat: string = '.xls, .xlsx, .xlsm';

  pagination: Pagination = <Pagination>{};

  iconButton = IconButton;
  classButton = ClassButton;

  param: LeaveApplicationMaintenance_Param = <LeaveApplicationMaintenance_Param>{};
  data: LeaveApplicationMaintenance_Main[] = [];
  selectedData: LeaveApplicationMaintenance_Main = <LeaveApplicationMaintenance_Main>{}

  factoryList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  leaveList: KeyValuePair[] = [];

  title: string = '';
  programCode: string = '';

  constructor(
    private service: S_5_1_11_Leave_Application_Maintenance,
    private modalService: ModalService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(async () => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
      this.processData()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.getData(false)
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.filterList(role.dataResolved)
        this.getSource()
      });
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.data = this.service.paramSearch().data;
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
        this.getData(false)
      else
        this.clear()
    }
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<LeaveApplicationMaintenance_MainMemory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data
    });
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory)
  }
  // #region Dropdown List
  retryGetDropDownList() {
    if (this.param.factory)
      this.service.getDropDownList(this.param.factory)
        .subscribe({
          next: (res) => {
            this.filterList(res)
          }
        });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.departmentList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.leaveList = structuredClone(keys.filter((x: { key: string; }) => x.key == "LE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  // #endregion

  // #region On Change Functions
  onFactoryChange() {
    this.retryGetDropDownList()
    this.deleteProperty('department')
  }
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.param[name] ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }
  // #endregion

  // #region Query
  getData = (isSearch: boolean = false) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.param.lang = localStorage.getItem(LocalStorageConstants.LANG)
      this.service
        .getSearchDetail(this.pagination, this.param)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.pagination = res.pagination;
            this.data = res.result;
            this.data.map((val: LeaveApplicationMaintenance_Main) => {
              val.update_Time = new Date(val.update_Time)
              const startDate = `${val.leave_Start} ${val.min_Start.substring(0, 2)}:${val.min_Start.substring(2, 4)}:00`
              const endDate = `${val.leave_End} ${val.min_End.substring(0, 2)}:${val.min_End.substring(2, 4)}:00`
              val.leave_Start_Date = new Date(startDate)
              val.leave_End_Date = new Date(endDate)
              val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time))
            })
            if (isSearch)
              this.snotifyService.success(
                this.translateService.instant('System.Message.SearchOKMsg'),
                this.translateService.instant('System.Caption.Success')
              );
            resolve()
          },
          error: () => { reject() }
        });
    })
  };
  search = () => {
    this.pagination.pageNumber == 1
      ? this.getData(true)
      : (this.pagination.pageNumber = 1);
  };
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  // #endregion

  // #region Main Actions
  downloadTemplate() {
    this.spinnerService.show();
    this.service
      .downloadExcelTemplate()
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            var link = document.createElement('a');
            document.body.appendChild(link);
            link.setAttribute("href", res.data);
            link.setAttribute("download", `${this.functionUtility.getFileNameExport(this.programCode, 'Template')}.xlsx`);
            link.click();
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.LeaveApplicationMaintenance.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      });
  }
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this.service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.getData()
          this.functionUtility.snotifySuccessError(true, 'System.Message.UploadOKMsg')
        } else {
          if (!this.functionUtility.checkEmpty(res.data)) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Report')
            this.functionUtility.exportExcel(res.data, fileName);
          }
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
        }
      }
    });
  }
  download() {
    this.spinnerService.show();
    this.param.lang = localStorage.getItem(LocalStorageConstants.LANG)
    this.service.export(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        else this.functionUtility.snotifySuccessError(false, res.error)
        this.spinnerService.hide();
      }
    });
  }
  remove(item: LeaveApplicationMaintenance_Main) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show();
      this.service.deleteData(item).subscribe({
        next: async (res) => {
          if (res.isSuccess) {
            await this.getData(false)
            this.snotifyService.success(
              this.translateService.instant('System.Message.DeleteOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.LeaveApplicationMaintenance.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
          this.spinnerService.hide();
        },
      });
    });
  }
  add() {
    const data = <LeaveApplicationMaintenance_Main>{ days: '0' }
    this.modalService.open(data, 'Add');
  }
  async edit(item: LeaveApplicationMaintenance_Main) {
    const isExisted = await this.checkDataExisted(item)
    if (!isExisted) {
      return this.snotifyService.error(
        this.translateService.instant(`AttendanceMaintenance.LeaveApplicationMaintenance.NotExitedData`),
        this.translateService.instant('System.Caption.Error'));
    }
    this.modalService.open(item, 'Edit');
  }
  clear() {
    this.param = <LeaveApplicationMaintenance_Param>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  // #endregion

  // #region Check Data
  checkDataExisted(item: LeaveApplicationMaintenance_Main) {
    return new Promise((resolve) => {
      this.spinnerService.show()
      this.service.isExistedData(item)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            resolve(res.isSuccess)
          }
        });
    })
  }
  // #endregion

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
