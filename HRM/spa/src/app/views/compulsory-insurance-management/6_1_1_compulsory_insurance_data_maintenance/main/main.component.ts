import { Component, effect, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import { CompulsoryInsuranceDataMaintenance_Basic, CompulsoryInsuranceDataMaintenanceDto, CompulsoryInsuranceDataMaintenanceParam } from '@models/compulsory-insurance-management/6_1_1_compulsory_insurance_data_maintenance';
import { S_6_1_1_Compulsory_Insurance_Data_MaintenanceService } from '@services/compulsory-insurance-management/s_6_1_1_compulsory_insurance_data_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  title: string = '';
  programCode: string = '';
  param: CompulsoryInsuranceDataMaintenanceParam = <CompulsoryInsuranceDataMaintenanceParam>{};
  data: CompulsoryInsuranceDataMaintenanceDto[] = [];
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listInsuranceType: KeyValuePair[] = [];
  source: CompulsoryInsuranceDataMaintenance_Basic;
  employeeList: EmployeeCommonInfo[] = [];
  insurance_Start_Date: Date;
  insurance_End_Date: Date;
  extensions: string = '.xls, .xlsm, .xlsx';
  isEmployeeID: boolean = false;
  constructor(
    private service: S_6_1_1_Compulsory_Insurance_Data_MaintenanceService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListInsuranceType();
      if (this.checkRequiredParams() && this.functionUtility.checkFunction('Search'))
        this.getData(false);
    });
    this.getDataFromSource();
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSource().param;
      this.pagination = this.service.paramSource().pagination;
      this.getListFactory();
      this.getListInsuranceType();
      if (!this.functionUtility.checkEmpty(this.param.insurance_Start))
        this.insurance_Start_Date = new Date(this.param.insurance_Start);
      if (!this.functionUtility.checkEmpty(this.param.insurance_End))
        this.insurance_End_Date = new Date(this.param.insurance_End);
      if (this.checkRequiredParams() && this.functionUtility.checkFunction('Search'))
        this.getData(false);
    });
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  getListInsuranceType() {
    this.service.getListInsuranceType().subscribe({
      next: (res) => {
        this.listInsuranceType = res
      },
    });
  }

  getData(isSearch: boolean = false) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      },
    });
  }

  ngOnDestroy(): void {
    if (!this.source)
      this.source = <CompulsoryInsuranceDataMaintenance_Basic>{
        pagination: this.pagination,
        param: { ...this.param },
      };
    this.service.setSource(this.source);
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clear(isClear: boolean) {
    this.deleteProperty('factory')
    this.deleteProperty('employee_ID')
    this.deleteProperty('insurance_Type')
    this.deleteProperty('insurance_Start')
    this.deleteProperty('insurance_End')
    this.param.searchMethod = 'InsuranceDate';
    this.insurance_Start_Date = null;
    this.insurance_End_Date = null
    this.isEmployeeID = false;
    if (isClear) {
      this.pagination.pageNumber = 1;
      this.data = [];
      this.pagination.totalCount = 0;
    }
    else this.functionUtility.checkFunction('Search') ? this.getData() : this.data = [];
  }
  deleteProperty = (name: string) => delete this.param[name]
  setParamSource(item: CompulsoryInsuranceDataMaintenanceDto) {
    this.source = <CompulsoryInsuranceDataMaintenance_Basic>{
      param: this.param,
      pagination: this.pagination,
      data: item,
    }
    return this.source
  }
  onForm(item: CompulsoryInsuranceDataMaintenanceDto = null) {
    this.service.setSource(this.setParamSource(item ?? <CompulsoryInsuranceDataMaintenanceDto>{}));
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }
  delete(item: CompulsoryInsuranceDataMaintenanceDto) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service.delete(item).subscribe({
          next: (result) => {
            this.spinnerService.hide();
            if (result.isSuccess) {
              this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
              this.getData(false);
            }
            else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          },
        });
      }
    );
  }

  getListEmployee() {
    if (this.param.factory) {
      this.commonService.getListEmployeeAdd(this.param.factory).subscribe({
        next: res => {
          this.employeeList = res
        }
      })
    }
  }

  checkRequiredParams() {
    if (this.param.factory != null && this.param.searchMethod != null) {
      if (this.param.searchMethod === 'EmployeeID') {
        return this.param.employee_ID != null && this.param.employee_ID != "";
      }
      if (this.param.searchMethod === 'InsuranceDate') {
        return (
          this.functionUtility.isValidDate(this.insurance_Start_Date) &&
          this.functionUtility.isValidDate(this.insurance_End_Date)
        );
      }
      return true;
    }
    return false;
  }

  download() {
    if (this.data.length == 0)
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
    this.spinnerService.show();
    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }
  downloadExcelTemplate() {
    this.spinnerService.show();
    this.service.downloadTemplate().subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Template')
        this.functionUtility.exportExcel(result.data, fileName)
      },
    });
  }
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this.service.upload(event.formData).subscribe({
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
  onChangeInsurance() {
    this.param.insurance_Start = this.insurance_Start_Date == null ? null : this.functionUtility.getDateFormat(this.insurance_Start_Date);
    this.param.insurance_End = this.insurance_End_Date == null ? null : this.functionUtility.getDateFormat(this.insurance_End_Date);
  }
  onChangeMethod() {
    this.isEmployeeID = this.param.searchMethod === 'EmployeeID';
  }
}
