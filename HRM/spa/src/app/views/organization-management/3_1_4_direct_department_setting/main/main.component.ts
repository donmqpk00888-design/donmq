import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LangConstants } from '@constants/lang-constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  Org_Direct_DepartmentParam,
  Org_Direct_DepartmentResult,
  Org_Direct_DepartmentSource,
} from '@models/organization-management/3_1_4-direct-department-setting';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_3_1_4_DirectDepartmentSettingService } from '@services/organization-management/s_3_1_4_direct-department-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  data: Org_Direct_DepartmentResult[] = [];
  selectedData: Org_Direct_DepartmentResult = <Org_Direct_DepartmentResult>{};
  pagination: Pagination = <Pagination>{};
  title: string = '';
  programCode: string = '';
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  param: Org_Direct_DepartmentParam = <Org_Direct_DepartmentParam>{};
  isChange: boolean;
  constructor(private service: S_3_1_4_DirectDepartmentSettingService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.param.lang = event.lang
        this.isChange = false;
        this.getListDivision();
      });
    this.getDataFromSource();
  }
  ngOnDestroy(): void {
    this.service.setSearchSource(<Org_Direct_DepartmentSource>{
      pagination: this.pagination,
      param: this.param,
      data: this.data,
      selectedData: this.selectedData
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListDepartment();
  }
  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      this.isChange = false;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear();
        else
          this.getData();
      }
    });
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1
      ? this.getData(isSearch)
      : (this.pagination.pageNumber = 1);
  }
  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      }

    });
    this.getListFactory();
  }
  getListDepartment() {
    this.service
      .getListDepartment(
    )
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        }
      });
  }
  getListFactory() {
    if (this.isChange)
      this.deleteProperty('factory')
    this.isChange = true;
    this.service
      .getListFactory(this.param.division)
      .subscribe({
        next: (res) => {
          this.listFactory = res;
        }
      });
  }
  clear() {
    this.deleteProperty('division')
    this.deleteProperty('department_Code')
    this.getListFactory();
    this.getListDivision();
    this.pagination.pageNumber = 1;
    this.data = [];
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.data = res.result;
        this.pagination = res.pagination;
        this.spinnerService.hide();
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  onExcel() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);

        }
      }
    });
  }

  onForm(item: Org_Direct_DepartmentResult = null) {
    this.selectedData = item != null ? { ...item } : <Org_Direct_DepartmentResult>{}
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }

  deleteItem(item: Org_Direct_DepartmentResult) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (result) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (result.isSuccess) this.getData();
        }
      })
    }
    );
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
