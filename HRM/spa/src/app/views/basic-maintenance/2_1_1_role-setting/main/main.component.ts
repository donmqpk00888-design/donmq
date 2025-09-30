import { Component, OnDestroy, OnInit, TrackByFunction, effect, inject } from '@angular/core';
import { TreeviewItem } from '@ash-mezdo/ngx-treeview';
import { ClassButton, IconButton } from '@constants/common.constants';
import { RoleSettingParam, RoleSettingDetail, RoleSetting_MainMemory } from '@models/basic-maintenance/2_1_1_role-setting';
import { S_2_1_1_RoleSetting } from '@services/basic-maintenance/s_2_1_1_role-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {

  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  classButton = ClassButton;

  roleList: TreeviewItem[] = [];
  dataMain: RoleSettingDetail[] = [];
  searchingParam: RoleSettingParam = <RoleSettingParam>{};

  priorityList: KeyValuePair[] = [];
  acceptFormat: string = '.xls, .xlsx, .xlsm';
  factoryList: KeyValuePair[] = [];
  salaryCodeList: KeyValuePair[] = [];
  title: string
  programCode: string = '';
  selectedRole: string = ''
  directList: KeyValuePair[] = [
    { key: '1', value: 'BasicMaintenance.RoleSetting.Direct', optional: false },
    { key: '2', value: 'BasicMaintenance.RoleSetting.Indirect', optional: false },
    { key: '3', value: 'BasicMaintenance.RoleSetting.All', optional: false }
  ];
  trackByFn: TrackByFunction<RoleSettingDetail> = (_, item) => item.role;
  constructor(
    private roleSettingService: S_2_1_1_RoleSetting,
    private modalService: ModalService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    effect(() => {
      this.searchingParam = this.roleSettingService.paramSearch().param;
      this.pagination = this.roleSettingService.paramSearch().pagination;
      this.dataMain = this.roleSettingService.paramSearch().data;
      this.directList.map(x => x.optional = this.roleSettingService.getRadioChecked(x.key)())
      this.loadData()
    });
  }
  private loadData() {
    this.retryGetDropDownList()
    if (this.dataMain.length > 0) {
      if (this.functionUtility.checkFunction('Search'))
        this.getData(false)
      else
        this.clear()
    }
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (res) => {
        this.filterList(res.dataResolved)
      });
  }
  ngOnDestroy(): void {
    this.roleSettingService.setParamSearch(<RoleSetting_MainMemory>{ param: this.searchingParam, pagination: this.pagination, data: this.dataMain });
  }
  retryGetDropDownList() {
    this.roleSettingService.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "F")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryCodeList = structuredClone(keys.filter((x: { key: string; }) => x.key == "S")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getData = (isSearch: boolean) => {
    this.spinnerService.show();
    this.roleSettingService
      .getSearchDetail(this.pagination, this.searchingParam)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.pagination = res.pagination;
          this.dataMain = res.result;
          this.dataMain.map(x => {
            x.update_Time != null
              ? x.update_Time_Str = x.update_Time.toDate().toStringDateTime()
              : x.update_Time_Str = ''
          })
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.SearchOKMsg')
        }
      });
  };
  onDirectChange(event: any) {
    this.directList.forEach(x => x.optional = x.key === event.srcElement.value && event.srcElement.checked);
    this.searchingParam.direct = this.directList.find((x) => x.optional)?.key ?? '';
  }
  search = () => {
    this.pagination.pageNumber == 1
      ? this.getData(true)
      : (this.pagination.pageNumber = 1);
  };
  clear() {
    this.searchingParam = <RoleSettingParam>{};
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
    this.dataMain = []
  }
  watch(item: RoleSettingDetail) {
    this.selectedRole = item.role
    this.getProgramGroup(item)
  }
  getProgramGroup = (roleSetting: RoleSettingDetail) => {
    this.spinnerService.show();
    let para: RoleSettingParam = <RoleSettingParam>{ role: roleSetting.role };
    this.roleSettingService
      .getProgramGroupDetail(para)
      .subscribe({
        next: (res: TreeviewItem[]) => {
          this.spinnerService.hide();
          this.modalService.open( res);
        }
      });
  };
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(e: RoleSettingDetail) {
    this.roleSettingService.setParamForm(e.role);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  remove(e: RoleSettingDetail) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.roleSettingService.deleteRole(e.role, e.factory).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : res.error, res.isSuccess)
          if (res.isSuccess)
            this.getData(false);
        }
      });
    });
  }
  copy(e: RoleSettingDetail) {
    this.roleSettingService.setParamForm(e.role);
    this.roleSettingService.setParamSearch(<RoleSetting_MainMemory>{ param: this.searchingParam, pagination: this.pagination, data: this.dataMain });
    this.router.navigate([`${this.router.routerState.snapshot.url}/copy`]);
  }
  download() {
    this.spinnerService.show();
    this.roleSettingService
      .downloadExcel(this.searchingParam)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  deleteProperty = (name: string) => delete this.searchingParam[name]
}
