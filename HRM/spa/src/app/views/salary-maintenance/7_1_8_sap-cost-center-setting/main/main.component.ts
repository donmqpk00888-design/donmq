import { Component, OnInit } from '@angular/core';
import { IconButton, ClassButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  SAPCostCenterSettingParam,
  SAPCostCenterSettingDto,
  SAPCostCenterSettingSource,
  ExistedDataParam
} from '@models/salary-maintenance/7_1_8_sap-cost-center-setting';
import { S_7_1_8_SapCostCenterSettingService } from '@services/salary-maintenance/s_7_1_8_sap-cost-center-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  params: SAPCostCenterSettingParam = <SAPCostCenterSettingParam>{
    costYear: ''
  };
  pagination: Pagination = <Pagination>{};
  dataMain: SAPCostCenterSettingDto[] = [];
  selectedData: SAPCostCenterSettingDto;

  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  factories: KeyValuePair[] = [];
  accept = '.xls, .xlsx, .xlsm';
  source: SAPCostCenterSettingSource;
  bsConfigYear: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY',
    minMode: 'year',
  };

  // #region  constructor
  constructor(
    private _service: S_7_1_8_SapCostCenterSettingService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.processData()
    });
  }

  // #region ngOnInit
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }

  // #region ngOnDestroy
  ngOnDestroy(): void {
    this._service.setSource(<SAPCostCenterSettingSource>{
      pagination: this.pagination,
      param: { ...this.params },
      selectedData: this.selectedData,
      data: this.dataMain
    });
  }
  getSource() {
    this.params = this._service.paramSearch().param;
    this.pagination = this._service.paramSearch().pagination;
    this.dataMain = this._service.paramSearch().data;
    this.getListFactory();
    this.processData()
  }
  processData() {
    if (this.dataMain.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData(false)
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.params.factory);
  }

  getListFactory() {
    this._service.getListFactory()
      .subscribe({
        next: (res) => {
          this.factories = res;
        }
      });
  }

  //#region getData
  getData = (isSearch: boolean) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.params.costYear = !this.params.costYear ? '' : new Date(this.params.costYear).getFullYear().toString()
      this._service.getData(this.pagination, this.params)
        .subscribe({
          next: (res) => {
            this.dataMain = res.result;
            this.pagination = res.pagination;
            if (isSearch)
              this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
            this.spinnerService.hide();
            resolve()
          },
          error: () => { reject() }
        });
    })
  };

  search = (isSearch?: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch ?? true) : this.pagination.pageNumber = 1;
  };

  isNumber(event: KeyboardEvent): boolean {
    const charCode = event.which ? event.which : event.keyCode;
    // Kiểm tra nếu không phải là số (0-9)
    if (charCode < 48 || charCode > 57) {
      event.preventDefault();
      return false;
    }
    return true;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData(false);
  }
  //#endregion

  //#region add
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  //#region edit
  edit(item: SAPCostCenterSettingDto) {
    this.spinnerService.show();
    const _data: ExistedDataParam = <ExistedDataParam>{
      company_Code: item.company_Code,
      cost_Code: item.cost_Code,
      cost_Year: item.year
    }
    this._service.checkExistedData(_data)
      .subscribe({
        next: (res: any) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.selectedData = item
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
          }
          else {
            this.functionUtility.snotifySuccessError(res.isSuccess, "System.Message.NotExitedData");
          }
        }
      });
  }

  // #region delete item
  onDelete(item: SAPCostCenterSettingDto) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {
      const _data: ExistedDataParam = <ExistedDataParam>{
        company_Code: item.company_Code,
        cost_Code: item.cost_Code,
        cost_Year: item.year
      }
      this.spinnerService.show();
      this._service.delete(_data).subscribe({
        next: res => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
          if (res.isSuccess)
            this.getData(false);
        }
      })
    });
  }

  //#region deleteProperty
  deleteProperty(name: string) {
    delete this.params[name]
  }

  //#region download
  download() {
    this.spinnerService.show();
    this._service.downloadExcel().subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Template')
        this.functionUtility.exportExcel(result.data, fileName)
      }
    });
  }

  //#region excelExport
  excelExport() {
    this.params.costYear = !this.params.costYear ? '' : new Date(this.params.costYear).getFullYear().toString();
    this.spinnerService.show();
    this._service.excelExport(this.params)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          if (!result.isSuccess)
            this.snotifyService.warning(this.translateService.instant('System.Message.NoData'), this.translateService.instant('System.Caption.Warning'));
          else {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
            this.functionUtility.exportExcel(result.data, fileName);
          }
        }
      });
  }

  //#region uploadExcel
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this._service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.getData(false);
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
  //#region clear
  clear() {
    this.params = <SAPCostCenterSettingParam>{ language: localStorage.getItem(LocalStorageConstants.LANG), costYear: '', company_Code: '' }
    this.dataMain = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0
    };
  }
  //#endregion
}
