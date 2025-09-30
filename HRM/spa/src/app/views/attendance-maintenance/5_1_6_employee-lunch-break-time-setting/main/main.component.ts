import { S_5_1_6_EmployeeLunchBreakTimeSettingService } from '@services/attendance-maintenance/s_5_1_6_employee-lunch-break-time-setting.service';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { EmployeeLunchBreakTimeSettingParam, EmployeeLunchBreakTimeSettingSource, EmployeeLunchBreakTimeSettingUpload, HRMS_Att_LunchtimeDto } from '@models/attendance-maintenance/5_1_6_employee-lunch-break-time-setting';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  accept = '.xls, .xlsx, .xlsm';
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  iconButton = IconButton;
  data: HRMS_Att_LunchtimeDto[] = [];
  param: EmployeeLunchBreakTimeSettingParam = <EmployeeLunchBreakTimeSettingParam>{
    factory: '',
    in_Service: '',
    employee_ID: '',
    department_From: '',
    department_To: ''
  }
  uploadParam = { file: null } as EmployeeLunchBreakTimeSettingUpload;
  selectedKey: string = 'Y';
  key: KeyValuePair[] = [
    {
      'key': 'Y',
      'value': 'Y'
    },
    {
      'key': 'N',
      'value': 'N'
    },
  ];

  constructor(private service: S_5_1_6_EmployeeLunchBreakTimeSettingService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListDepartment();
      if (this.data.length > 0)
        this.getData();
    });
    effect(() => {
      const { param, selectedKey, data, pagination } = this.service.paramSearch();
      this.param = param;
      this.selectedKey = selectedKey;
      this.pagination = pagination;
      this.data = data;

      if (!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getListFactory();
        this.getListDepartment();
      }
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search') && !this.functionUtility.checkEmpty(this.param.factory))
          this.getData()
        else
          this.clear()
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<EmployeeLunchBreakTimeSettingSource>{
      param: this.param,
      selectedKey: this.selectedKey,
      pagination: this.pagination,
      data: this.data
    });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }

  onFactoryChange() {
    this.param.department_From = '';
    this.param.department_To = '';
    this.getListDepartment();
  }

  getData(isSearch?: boolean, isDelete?: boolean) {
    this.spinnerService.show();
    this.param.in_Service = this.selectedKey;
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.handleSuccess('System.Message.QuerySuccess');
        if (isDelete)
          this.handleSuccess('System.Message.DeleteOKMsg');
      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this.service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.param.factory)
            this.getData();
          this.handleSuccess('System.Message.UploadOKMsg');
        } else {
          if (!this.functionUtility.checkEmpty(res.data)) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Report')
            this.functionUtility.exportExcel(res.data, fileName);
          }
          this.handleError(res.error);
        }
      }
    });
  }
  downloadTemplate() {
    this.spinnerService.show();
    this.service.downloadTemplate().subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Template')
        this.functionUtility.exportExcel(result.data, fileName)
      },
    });
  }
  download() {
    this.param.in_Service = this.selectedKey;
    this.spinnerService.show();
    this.service
      .downloadExcel(this.param)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, this.functionUtility.getFileName(fileName));
        }
      });
  }

  delete(item: HRMS_Att_LunchtimeDto, isDelete: boolean) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show()
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          if (res.isSuccess)
            this.getData(false, isDelete);
          else {
            this.handleError('System.Message.DeleteErrorMsg')
          }
        }
      })
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.listDepartment = [];
    this.param = <EmployeeLunchBreakTimeSettingParam>{
      factory: '',
      in_Service: '',
      employee_ID: '',
      department_From: '',
      department_To: ''
    };
    this.selectedKey = 'Y'
    this.data = []
  }

  handleSuccess(message: string) {
    this.spinnerService.hide()
    this.snotifyService.success(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Success')
    )
  }

  handleError(message: string) {
    this.spinnerService.hide()
    this.snotifyService.error(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Error')
    )
  }
}
