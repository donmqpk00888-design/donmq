import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_4_Bank_Account_MaintenanceService } from '@services/salary-maintenance/s_7_1_4_bank_account_maintenance.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { BankAccountMaintenance_Basic, BankAccountMaintenanceDto, BankAccountMaintenanceParam } from '@models/salary-maintenance/7_1_4_bank_account_maintenance';
import { Pagination } from '@utilities/pagination-utility';
import { IconButton } from '@constants/common.constants';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { KeyValuePair } from '@utilities/key-value-pair';
import { EmployeeCommonInfo } from '@models/common';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
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
  param: BankAccountMaintenanceParam = <BankAccountMaintenanceParam>{};
  data: BankAccountMaintenanceDto[] = [];
  selectedData: BankAccountMaintenanceDto = <BankAccountMaintenanceDto>{};
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  source: BankAccountMaintenance_Basic;
  dataTypeaHead: string[];
  extensions: string = '.xls, .xlsm, .xlsx';

  constructor(
    private service: S_7_1_4_Bank_Account_MaintenanceService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.processData()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  getSource() {
    this.param = this.service.paramSource().param;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
    this.getListFactory();
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.param.factory) {
        this.getData(false)
      }
      else
        this.clear()
    }
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
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
    this.service.setSource(<BankAccountMaintenance_Basic>{
      param: this.param,
      pagination: this.pagination,
      selectedData: this.selectedData,
      data: this.data
    });
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clear() {
    this.param = <BankAccountMaintenanceParam>{}
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }
  deleteProperty = (name: string) => delete this.param[name]

  onForm(item: BankAccountMaintenanceDto = null) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }
  delete(item: BankAccountMaintenanceDto) {
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
          }
        });
      }
    );
  }

  download() {
    if (this.data.length == 0)
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess ? this.functionUtility.exportExcel(result.data, fileName)
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
          if (this.functionUtility.checkFunction('Search') && this.param.factory)
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
}
