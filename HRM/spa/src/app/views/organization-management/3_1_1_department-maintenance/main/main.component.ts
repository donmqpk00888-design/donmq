import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Org_Department, HRMS_Org_DepartmentParamSource, HRMS_Org_Department_Param } from '@models/organization-management/3_1_1-department-maintenance';
import { S_3_1_1_DepartmentMaintenanceService } from '@services/organization-management/s_3_1_1_department-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{};
  department: KeyValuePair[] = [];
  division: KeyValuePair[] = [];
  factory: KeyValuePair[] = [];
  data: HRMS_Org_Department[] = []
  param: HRMS_Org_Department_Param = <HRMS_Org_Department_Param>{}
  sourceItem: HRMS_Org_Department= <HRMS_Org_Department>{}
  key: KeyValuePair[] = [
    { key: 'All', value: 'OrganizationManagement.DepartmentMaintenance.All' },
    { key: 'Y', value: 'OrganizationManagement.DepartmentMaintenance.Enabled' },
    { key: 'N', value: 'OrganizationManagement.DepartmentMaintenance.Disabled' }
  ];

  constructor(
    private service: S_3_1_1_DepartmentMaintenanceService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListDepartment();
      this.getDataFromSource();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListDepartment();
    this.getListFactory();
    this.getDataFromSource();
  }
  ngOnDestroy(): void {
    this.service.setSource(<HRMS_Org_DepartmentParamSource>{
      param: this.param,
      currentPage: this.pagination,
      dataMain: this.data,
      selectedData: this.sourceItem,
      status: null
    });
  }

  getDataFromSource() {
    this.service.programSource$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(source => {
      if (source && source != null) {
        this.pagination = source.currentPage;
        this.param = source.param;
        if (source.dataMain.length > 0) {
          if (this.functionUtility.checkFunction('Search'))
            this.getData();
          else
            this.clear()
        }
      }
    })
  }

  getData(isSearch?: boolean) {
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

  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory).subscribe({
      next: (res) => this.department = res
    });
  }
  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => this.division = res
    });
  }

  onSelectDivision() {
    this.deleteProperty('factory')
    this.deleteProperty('department_Code')
    this.getListFactory()
    this.getListDepartment()
  }

  onSelectFactory() {
    this.deleteProperty('department_Code')
    this.getListDepartment()
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => this.factory = res
    });
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }
  clear() {
    this.data = []
    this.param = <HRMS_Org_Department_Param>{ status: 'Y' }
    this.pagination.totalCount = 0
    this.pagination.pageNumber = 0
    this.onSelectDivision()
  }


  onForm(item: HRMS_Org_Department = null) {
    if (item != null)
      this.sourceItem = item
    this.redirectToForm(item != null);
  }
  redirectToForm = (isEdit: boolean = false) =>
    this.router.navigate([`${this.router.routerState.snapshot.url}/${isEdit ? 'edit' : 'add'}`]);

  excel() {
    this.spinnerService.show();
    this.param.lang = localStorage.getItem(LocalStorageConstants.LANG);
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
      }
    });
  }
  pageChanged(event: any) {
    if (this.pagination.pageNumber !== event.page) {
      this.pagination.pageNumber = event.page;
      if (this.pagination.pageNumber !== 0)
        this.getData();
    }
  }
  deleteProperty = (name: string) => delete this.param[name]

}
