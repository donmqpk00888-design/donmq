import { ClassButton, IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { AfterViewChecked, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { S_7_1_27_FinSalaryAttributionCategoryMaintenance } from '@services/salary-maintenance/s_7_1_27_fin-salary-attribution-category-maintenance.service';
import { Pagination } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import {
  FinSalaryAttributionCategoryMaintenance_Data,
  FinSalaryAttributionCategoryMaintenance_Memory,
  FinSalaryAttributionCategoryMaintenance_Param,
} from '@models/salary-maintenance/7_1_27_fin-salary-attribution-category-maintenance';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { FormGroup, NgForm } from '@angular/forms';
import { ModalService } from '@services/modal.service';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  @ViewChild('mainForm') public mainForm: NgForm;

  acceptFormat: string = '.xls, .xlsx, .xlsm';

  pagination: Pagination = <Pagination>{};

  iconButton = IconButton;
  classButton = ClassButton;

  param: FinSalaryAttributionCategoryMaintenance_Param = <FinSalaryAttributionCategoryMaintenance_Param>{};
  data: FinSalaryAttributionCategoryMaintenance_Data[] = [];

  factoryList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  kindList: KeyValuePair[] = [];
  salaryCategoryList: KeyValuePair[] = [];
  kindCodeList: KeyValuePair[] = [];

  title: string = ''
  formType: string = ''
  programCode: string = ''

  allowGetData: boolean = false

  constructor(
    private service: S_7_1_27_FinSalaryAttributionCategoryMaintenance,
    private modalService: ModalService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
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
  ngAfterViewChecked() {
    if (this.allowGetData && this.mainForm) {
      const form: FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getData(false);
        this.allowGetData = false
      }
    }
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<FinSalaryAttributionCategoryMaintenance_Memory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
    });
  }
  private getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.data = this.service.paramSearch().data;
    this.processData()
  }
  private processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search'))
        this.allowGetData = true
      else {
        this.clear()
        this.allowGetData = false
      }
    }
    this.getDepartment()
    this.getKindCode()
  }

  private getDropDownList() {
    this.service.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  private getDepartment(onFactoryChange?: boolean) {
    if (this.param.factory) {
      this.service
        .getDepartmentList(this.param)
        .subscribe({
          next: (res) => {
            this.departmentList = res;
            if (onFactoryChange)
              this.deleteProperty('department')
          }
        });
    }
  }
  private getKindCode(onKindChange?: boolean) {
    if (this.param.kind) {
      this.service
        .getKindCodeList(this.param)
        .subscribe({
          next: (res) => {
            this.kindCodeList = res;
            this.functionUtility.getNgSelectAllCheckbox(this.kindCodeList)
            if (onKindChange)
              this.deleteProperty('kind_Code_List')
          }
        });
    }
  }
  private filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.kindList = structuredClone(keys.filter((x: { key: string; }) => x.key == "ME")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryCategoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "SA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  clear() {
    this.param = <FinSalaryAttributionCategoryMaintenance_Param>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  add = () => this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  edit(item: FinSalaryAttributionCategoryMaintenance_Data) {
    this.spinnerService.show()
    this.service.isExistedData(item.factory, item.kind, item.department, item.kind_Code)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res)
            this.modalService.open(item);
          else {
            this.getData(false)
            this.snotifyService.error(
              this.translateService.instant('System.Message.NotExitedData'),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  remove(item: FinSalaryAttributionCategoryMaintenance_Data) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'), () => {
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
                this.translateService.instant(`System.Message.${res.error}`),
                this.translateService.instant('System.Caption.Error'));
            }
            this.spinnerService.hide();
          }
        });
      });
  }
  search = () => {
    this.pagination.pageNumber == 1
      ? this.getData(true)
      : this.pagination.pageNumber = 1;
  };
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
              this.translateService.instant(`System.Message.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  download() {
    this.spinnerService.show();
    this.service
      .downloadExcel(this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            if (res.error) {
              this.snotifyService.warning(
                this.translateService.instant(`System.Message.${res.error}`),
                this.translateService.instant('System.Caption.Warning')
              );
            } else {
              const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
              this.functionUtility.exportExcel(res.data, fileName);
            }
          } else {
            this.snotifyService.error(
              this.translateService.instant(`System.Message.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
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
          if (this.functionUtility.checkFunction('Search') && this.param.factory && this.param.kind)
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
  private getData = (isSearch: boolean = false) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.service
        .getSearchDetail(this.pagination, this.param)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              this.pagination = res.data.pagination;
              this.data = res.data.result;
              if (isSearch)
                this.snotifyService.success(
                  this.translateService.instant('System.Message.SearchOKMsg'),
                  this.translateService.instant('System.Caption.Success')
                );
            } else {
              this.snotifyService.error(
                this.translateService.instant(`System.Message.${res.error}`),
                this.translateService.instant('System.Caption.Error'));
            }
            resolve()
          },
          error: () => { reject() }
        });
    })
  };
  onFactoryChange() {
    this.getDepartment(true)
    this.deleteProperty('department')
  }
  onKindChange() {
    this.getKindCode(true)
    this.deleteProperty('kind_Code_List')
  }
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
