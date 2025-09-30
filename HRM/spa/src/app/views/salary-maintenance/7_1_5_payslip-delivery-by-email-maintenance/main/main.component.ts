import { S_7_1_5_PayslipDeliveryByEmailMaintenanceService } from '@services/salary-maintenance/s_7_1_5_payslip-delivery-by-email-maintenance.service';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  PayslipDeliveryByEmailMaintenanceDto,
  PayslipDeliveryByEmailMaintenanceParam,
  PayslipDeliveryByEmailMaintenanceSource
} from '@models/salary-maintenance/7_1_5_payslip-delivery-by-email-maintenance';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{};
  listFactory: KeyValuePair[] = [];
  param: PayslipDeliveryByEmailMaintenanceParam = <PayslipDeliveryByEmailMaintenanceParam>{};
  data: PayslipDeliveryByEmailMaintenanceDto[] = [];
  selectedData: PayslipDeliveryByEmailMaintenanceDto = <PayslipDeliveryByEmailMaintenanceDto>{};
  iconButton = IconButton;
  accept = '.xls, .xlsx, .xlsm';

  constructor(
    private service: S_7_1_5_PayslipDeliveryByEmailMaintenanceService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.processData()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<PayslipDeliveryByEmailMaintenanceSource>{
      param: this.param,
      pagination: this.pagination,
      selectedData: this.selectedData,
      data: this.data
    });
  }
  getSource() {
    const { param, pagination, data } = this.service.paramSearch();
    this.param = param;
    this.pagination = pagination;
    this.data = data;
    this.getListFactory();
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }

  //#region getEmployeeData
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }
  //#endregion

  //#region getData
  getData(isSearch?: boolean, isDelete?: boolean, isUpload?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        if (isDelete)
          this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
        if (isUpload)
          this.functionUtility.snotifySuccessError(true, 'System.Message.UploadOKMsg')
      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  //#endregion

  //#region clear
  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <PayslipDeliveryByEmailMaintenanceParam>{};
    this.data = [];
  }
  //#endregion

  //#region download
  download(isTemplate: boolean) {
    this.spinnerService.show();
    this.service.downloadExcel(this.param, isTemplate).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = !isTemplate
            ? this.functionUtility.getFileNameExport(this.programCode, 'Download')
            : this.functionUtility.getFileNameExport(this.programCode, 'Template')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        else this.functionUtility.snotifySuccessError(false, res.error)
        this.spinnerService.hide();
      }
    });
  }
  //#endregion

  //#region upload
  upload(event: FileResultModel, isUpload: boolean) {
    this.spinnerService.show();
    this.service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.getData(false, false, isUpload);
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
  //#endregion

  //#region add-edit-delete
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: PayslipDeliveryByEmailMaintenanceDto) {
    this.selectedData = item;
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  delete(item: PayslipDeliveryByEmailMaintenanceDto, isDelete: boolean) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) this.getData(false, isDelete);
          else
            this.functionUtility.snotifySuccessError(false, 'System.Message.DeleteErrorMsg')
        }
      })
    });
  }
  //#endregion

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name];
  }
}
