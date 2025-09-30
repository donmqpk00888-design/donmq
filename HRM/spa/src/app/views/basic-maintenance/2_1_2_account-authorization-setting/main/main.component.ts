import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  AccountAuthorizationSetting_Param,
  AccountAuthorizationSetting_Memory,
  AccountAuthorizationSetting_Data
} from '@models/basic-maintenance/2_1_2_account-authorization-setting';
import { S_2_1_2_AccountAuthorizationSettingService } from '@services/basic-maintenance/s_2_1_2_account-authorization-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{};
  departmentList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  factoryList: KeyValuePair[] = [];
  roleList: KeyValuePair[] = [];
  iconButton = IconButton;
  data: AccountAuthorizationSetting_Data[] = [];
  selectedData: AccountAuthorizationSetting_Data = <AccountAuthorizationSetting_Data>{};
  param: AccountAuthorizationSetting_Param = <AccountAuthorizationSetting_Param>{};

  constructor(
    private service: S_2_1_2_AccountAuthorizationSettingService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListDepartment();
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear()
        else {
          this.getData()
        }
      }
      this.getListDivision();
      this.getListFactory();
      this.getListDepartment();
    });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<AccountAuthorizationSetting_Memory>{
      param: this.param,
      pagination: this.pagination,
      selectedData: this.selectedData,
      data: this.data
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListRole();
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.param.listRole_Str = this.param.listRole.length > 0 ? this.param.listRole.join('/ ') : '';
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

  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory,).subscribe({
      next: (res) => {
        this.departmentList = res;
      },
    });
  }


  getListRole() {
    this.service.getListRole().subscribe({
      next: (res) => {
        this.roleList = res;
        this.functionUtility.getNgSelectAllCheckbox(this.roleList)
      },
    });
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.divisionList = res;
      },
    });
  }
  getListFactory() {
    this.service.getListListFactory().subscribe({
      next: (res) => {
        this.factoryList = res;
      },
    });
  }

  onChange() {
    this.departmentList = [];
    this.deleteProperty('department_ID')
    if (!this.functionUtility.checkEmpty(this.param.factory) && !this.functionUtility.checkEmpty(this.param.division))
      this.getListDepartment();
  }

  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  onEdit(item: AccountAuthorizationSetting_Data) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  download() {
    this.spinnerService.show();
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
  clear() {
    this.param = <AccountAuthorizationSetting_Param>{
      isActive: 1,
      listRole: []
    };
    this.departmentList = [];
    this.pagination.pageNumber = 1;
    this.data = [];
  }
  deleteProperty = (name: string) => delete this.param[name]
}
