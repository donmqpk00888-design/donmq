import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { CaptionConstants } from '@constants/message.enum';
import { SalaryAdjustmentMaintenanceMain, SalaryAdjustmentMaintenanceParam, SalaryAdjustmentMaintenanceSource } from '@models/salary-maintenance/7_1_18_salary-adjustment-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_7_1_18_salaryAdjustmentMaintenanceService } from '@services/salary-maintenance/s_7_1_18_salary-adjustment-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  pagination: Pagination = <Pagination>{
    totalCount: 0
  };
  param: SalaryAdjustmentMaintenanceParam = <SalaryAdjustmentMaintenanceParam>{}
  data: SalaryAdjustmentMaintenanceMain[] = [];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  title: string = '';
  programCode: string = '';
  start_Date: Date;
  end_Date: Date;
  onboard_Date: Date;
  listFactory: KeyValuePair[] = []
  listDepartment: KeyValuePair[] = []
  listReasonForChange: KeyValuePair[] = []
  acceptFormat: string = '.xls, .xlsm, .xlsx';
  sourceItem: SalaryAdjustmentMaintenanceMain;

  constructor(private service: S_7_1_18_salaryAdjustmentMaintenanceService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropdownList()
      this.processData()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  ngOnDestroy(): void {
    this.service.setSource(<SalaryAdjustmentMaintenanceSource>{
      selectedData: this.sourceItem,
      paramQuery: this.param,
      dataMain: this.data,
      pagination: this.pagination,
    });
  }
  getSource() {
    this.param = this.service.programSource().paramQuery;
    this.pagination = this.service.programSource().pagination;
    this.data = this.service.programSource().dataMain;
    if (this.functionUtility.isValidDate(new Date(this.param.onboard_Date)))
      this.onboard_Date = new Date(this.param.onboard_Date);
    if (this.functionUtility.isValidDate(new Date(this.param.effective_Date_Start)))
      this.start_Date = new Date(this.param.effective_Date_Start);
    if (this.functionUtility.isValidDate(new Date(this.param.effective_Date_End)))
      this.end_Date = new Date(this.param.effective_Date_End);
    this.getDropdownList()
    this.processData()
  }

  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData(false)
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    var result =
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }

  getData(isSearch: boolean = false) {
    this.param.onboard_Date = this.formatDate(this.onboard_Date);
    this.param.effective_Date_Start = this.formatDate(this.start_Date);
    this.param.effective_Date_End = this.formatDate(this.end_Date);

    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }
  getDropdownList() {
    this.getlistFactory();
    this.getlistReasonForChange();
    this.getlistDepartment();
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  onSelectFactory() {
    this.deleteProperty('department');
    this.getlistDepartment();
    this.onChangeDate('onboard_Date')
  }
  getlistFactory() {
    this.service.getlistFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getlistDepartment() {
    if (this.functionUtility.checkEmpty(this.param.factory)) return
    this.service.getlistDepartment(this.param.factory,).subscribe({
      next: (res: KeyValuePair[]) => this.listDepartment = res
    });
  }
  getlistReasonForChange() {
    this.service.getlistReasonForChange().subscribe({
      next: (res: KeyValuePair[]) => {
        this.listReasonForChange = res
      }
    });
  }

  redirectToForm(action: string) {
    this.router.navigate([`${this.router.routerState.snapshot.url}/${action}`]);
  };
  onForm(action: string, item: SalaryAdjustmentMaintenanceMain = null) {
    this.sourceItem = item;
    this.redirectToForm(action);
  }
  downloadExcel() {
    this.param.onboard_Date = this.formatDate(this.onboard_Date);
    this.param.effective_Date_Start = this.formatDate(this.start_Date);
    this.param.effective_Date_End = this.formatDate(this.end_Date);

    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        if (result.isSuccess) {
          this.spinnerService.hide()
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else {
          this.spinnerService.hide()
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this.service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.getData();
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
  downloadTemplate() {
    this.spinnerService.show();
    this.service.downloadTemplate(this.param.factory).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Template')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        else
          this.snotifyService.warning(res.error, this.translateService.instant('System.Caption.Warning'));
      }
    });
  }

  search(isSearch: boolean) {
    if (this.start_Date && !this.end_Date || !this.start_Date && this.end_Date) {
      return this.snotifyService.warning('Please enter Effective Date Start and End', CaptionConstants.WARNING);
    }
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }

  clear() {
    this.data = []
    this.param = <SalaryAdjustmentMaintenanceParam>{}
    this.pagination.totalCount = 0
    this.pagination.pageNumber = 0
    this.start_Date = null
    this.end_Date = null
    this.onboard_Date = null
    this.getlistFactory()
  }
  onChangeDate(name: string) {
    this.param[name] = this[name] != null ? this[name].toStringDate() : null
  }
  pageChanged(event: any) {
    if (this.pagination.pageNumber !== event.page) {
      this.pagination.pageNumber = event.page;
      if (this.pagination.pageNumber !== 0)
        this.getData();
    }
  }
  deleteProperty = (name: string) => delete this.param[name]

}
