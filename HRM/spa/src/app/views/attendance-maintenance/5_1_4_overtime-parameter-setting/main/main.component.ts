import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_1_4_OvertimeParameterSettingService } from '@services/attendance-maintenance/s_5_1_4_overtime-parameter-setting.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  HRMS_Att_Overtime_ParameterDTO,
  HRMS_Att_Overtime_ParameterParam,
  HRMS_Att_Overtime_Parameter_Basic
} from '@models/attendance-maintenance/5_1_4_overtime-parameter-setting';
import { Pagination } from '@utilities/pagination-utility';
import { CaptionConstants, MessageConstants } from '@constants/message.enum';
import { KeyValuePair } from '@utilities/key-value-pair';
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
  param: HRMS_Att_Overtime_ParameterParam = <HRMS_Att_Overtime_ParameterParam>{};
  data: HRMS_Att_Overtime_ParameterDTO[] = [];
  pagination: Pagination = <Pagination>{};
  source: HRMS_Att_Overtime_Parameter_Basic
  iconButton = IconButton;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  effective_Month_Value: Date;
  extensions: string = '.xls, .xlsm, .xlsx';
  constructor(
    private service: S_5_1_4_OvertimeParameterSettingService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.getDataFromSource();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListWorkShiftType();
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
        this.getData()
    });

  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSource().param;
      this.pagination = this.service.paramSource().pagination;
      this.getListFactory();
      this.getListDivision();
      if (this.param.effective_Month)
        this.effective_Month_Value = this.param.effective_Month.toDate();
      if (this.param.factory != undefined && this.functionUtility.checkFunction('Search'))
        this.getData(false);
    });
  }

  checkRequiredParams() {
    if (this.param.division != null && this.param.factory != null)
      return true
    else return false
  }
  ngOnDestroy(): void {
    this.checkDate()
    if (!this.source)
      this.source = <HRMS_Att_Overtime_Parameter_Basic>{
        pagination: this.pagination,
        param: { ...this.param }
      };
    this.service.setSource(this.source);
  }
  checkDate() {
    if (this.effective_Month_Value != null)
      this.param.effective_Month = this.effective_Month_Value.toDate().toStringYearMonth();
    else this.deleteProperty('effective_Month');
  }

  getData(isSearch: boolean = false) {
    this.spinnerService.show();
    this.param.effective_Month = (!this.functionUtility.checkEmpty(this.effective_Month_Value)
      && (this.effective_Month_Value.toString() != 'Invalid Date' && this.effective_Month_Value.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.effective_Month_Value)
      : "";

    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.data.map((val: HRMS_Att_Overtime_ParameterDTO) => {
          val.effective_Month_Date = new Date(val.effective_Month)
        })
        this.pagination = res.pagination;
        if (isSearch)
          this.snotifyService.success(
            this.translateService.instant('System.Message.SearchOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
      },
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListFactory();
    this.getListWorkShiftType();
  }
  clear(isClear: boolean) {
    this.deleteProperty('division')
    this.deleteProperty('factory')
    this.deleteProperty('work_Shift_Type')
    this.deleteProperty('effective_Month')
    this.listFactory = []
    this.effective_Month_Value = null;
    if (isClear) {
      this.functionUtility.snotifySuccessError(true, 'System.Message.ClearSuccess');
      this.pagination.pageNumber = 1;
      this.data = [];
      this.pagination.totalCount = 0;
    }
    else this.functionUtility.checkFunction('Search') ? this.getData() : this.data = [];
  }
  search(isSearch: boolean) {
    this.checkDate()
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: HRMS_Att_Overtime_ParameterDTO) {
    this.source = <HRMS_Att_Overtime_Parameter_Basic>{
      pagination: this.pagination,
      param: { ...this.param },
      data: item
    };
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  download() {
    this.spinnerService.show();
    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
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
        this.spinnerService.hide()
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.search(false);
          this.functionUtility.snotifySuccessError(true, 'System.Message.UploadOKMsg')

          this.snotifyService.success(MessageConstants.UPLOADED_OK_MSG, CaptionConstants.SUCCESS);
        } else {
          this.functionUtility.snotifySuccessError(false, 'System.Message.UploadOKMsg')

          this.snotifyService.error(res.error, CaptionConstants.ERROR);
        }
      }
    });
  }
  //#region
  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: (res) => {
        this.listWorkShiftType = res;
      },
    });
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res
      },
    });
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  //#endregion

  onFactoryChange() {
    this.listFactory = []
    this.deleteProperty('factory')
    this.getListFactory()
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

}
