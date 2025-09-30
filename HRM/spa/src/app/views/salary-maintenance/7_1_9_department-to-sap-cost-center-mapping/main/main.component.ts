import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { S_7_1_9_departmentToSapCostCenterMappingService } from '@services/salary-maintenance/s_7_1_9_department-to-sap-cost-center-mapping.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { Pagination } from '@utilities/pagination-utility';
import {
  Sal_Dept_SAPCostCenter_MappingParam,
  Sal_Dept_SAPCostCenter_MappingDTO,
  Sal_Dept_SAPCostCenter_MappingSource
} from '@models/salary-maintenance/7_1_9_sal_dept_sapcostcenter_mapping';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  pagination: Pagination = <Pagination>{};
  param: Sal_Dept_SAPCostCenter_MappingParam = <Sal_Dept_SAPCostCenter_MappingParam>{
  }
  data: Sal_Dept_SAPCostCenter_MappingDTO[] = [];
  selectedData: Sal_Dept_SAPCostCenter_MappingDTO;
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY',
    minMode: 'year',
  };
  year: string;
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  acceptFormat: string = '.xls, .xlsx, .xlsm';
  title: string;
  programCode: string = '';
  listFactory: KeyValuePair[] = []
  listDepartment: KeyValuePair[] = []
  listCostCenter: KeyValuePair[] = []
  constructor(private service: S_7_1_9_departmentToSapCostCenterMappingService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
      this.processData()
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  ngOnDestroy(): void {
    this.service.setSource(<Sal_Dept_SAPCostCenter_MappingSource>{
      selectedData: this.selectedData,
      param: this.param,
      data: this.data,
      pagination: this.pagination,
    });
  }

  getDropDownList() {
    this.getListFactory();
    this.getListDepartment()
    this.getListCostCenter()
  }
  getSource() {
    this.param = this.service.programSource().param;
    this.pagination = this.service.programSource().pagination;
    this.data = this.service.programSource().data;
    this.getDropDownList()
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
    return !this.functionUtility.checkEmpty(this.param.factory);
  }
  getData(isSearch: boolean = false) {
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

  onSelectFactory() {
    this.deleteProperty('department')
    this.deleteProperty('costCenter')
    this.getListDepartment()
    this.getListCostCenter()
  }
  onDateChange() {
    this.deleteProperty('costCenter')
    this.param.year_Str = this.functionUtility.isValidDate(new Date(this.param.year)) ? new Date(this.param.year).getFullYear().toString() : '';
    this.getListCostCenter()
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }

  getListDepartment() {
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.service.getListDepartment(this.param.factory).subscribe({
        next: (res: KeyValuePair[]) => this.listDepartment = res
      });
    }
  }
  getListCostCenter() {
    if (!this.functionUtility.checkEmpty(this.param.factory) && !this.functionUtility.checkEmpty(this.param.year_Str)) {
      this.service.getListCostCenter(this.param).subscribe({
        next: (res: KeyValuePair[]) => this.listCostCenter = res
      });
    }
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }
  clear() {
    this.data = []
    this.param = <Sal_Dept_SAPCostCenter_MappingParam>{}
    this.pagination.totalCount = 0
    this.pagination.pageNumber = 1
    this.getListFactory()
  }


  onForm(item: Sal_Dept_SAPCostCenter_MappingDTO = null) {
    this.selectedData = item
    this.redirectToForm(item != null);
  }
  redirectToForm = (isEdit: boolean = false) => this.router.navigate([`${this.router.routerState.snapshot.url}/${isEdit ? 'edit' : 'add'}`]);

  delete(item: Sal_Dept_SAPCostCenter_MappingDTO) {
    item.cost_Year = item.cost_Year.toDate().getFullYear().toString()
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show()
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          if (res.isSuccess) {
            this.getData();
            this.snotifyService.success(this.translateService.instant('System.Message.DeleteOKMsg'), this.translateService.instant('System.Caption.Success'));
          }
          else {
            this.snotifyService.error(this.translateService.instant('System.Message.DeleteErrorMsg'), this.translateService.instant('System.Caption.Error'));
          }
        }
      })
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
  downloadExcel() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
          this.spinnerService.hide()
        if (result.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else {
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }

  downloadTemplate() {
    this.spinnerService.show();
    this.service.downloadTemplate().subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Template')
          var link = document.createElement('a');
          document.body.appendChild(link);
          link.setAttribute("href", res.data);
          link.setAttribute("download", `${fileName}.xlsx`);
          link.click();
        }
        else {
          this.snotifyService.warning(res.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty = (name: string) => delete this.param[name]
}
