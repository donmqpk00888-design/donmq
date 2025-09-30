import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton, ClassButton, Placeholder } from '@constants/common.constants';
import {
  D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain,
  D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam,
  MonthlySalaryMaintenanceExitedEmployeesSource
} from '@models/salary-maintenance/7_1_25-monthly-salary-maintenance-exited-employees';
import { S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService } from '@services/salary-maintenance/s_7_1_25_monthly-salary-maintenance-exited-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder

  pagination: Pagination = <Pagination>{};
  param: D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam = <D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam>{}
  data: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain[] = [];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  title: string = '';
  year_Month: Date
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  totalPermissionGroup: number = 0;
  constructor(private service: S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService) {
    super();
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropdownList()
      this.processData()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  ngOnDestroy(): void {
    this.service.setSource(<MonthlySalaryMaintenanceExitedEmployeesSource>{
      dataItem: this.sourceItem,
      paramSearch: this.param,
      dataMain: this.data,
      pagination: this.pagination,
    });
  }

  getSource() {
    this.pagination = this.service.paramSource().pagination;
    this.param = this.service.paramSource().paramSearch;
    this.data = this.service.paramSource().dataMain;
    if (this.functionUtility.isValidDate(new Date(this.param.year_Month)))
      this.year_Month = new Date(this.param.year_Month);
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

  getDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory) &&
      this.functionUtility.isValidDate(new Date(this.param.year_Month)) &&
      this.param.permission_Group.length > 0
  }
  getData(isSearch?: boolean) {
    this.param.year_Month = this.formatDate(this.year_Month);
    this.spinnerService.show();
    this.service.search(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }
  getDropdownList() {
    this.getListFactory();
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.getListDepartment();
      this.getListPermissionGroup();
    }
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  onSelectFactory() {
    this.deleteProperty('department');
    this.deleteProperty('permission_Group');
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getListDepartment() {
    if (this.param.factory)
      this.service.getListDepartment(this.param.factory).subscribe({
        next: (res: KeyValuePair[]) => this.listDepartment = res
      });
  }

  getListPermissionGroup() {
    if (this.param.factory)
      this.service.getListPermissionGroup(this.param.factory).subscribe({
        next: res => {
          this.listPermissionGroup = res
          this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
        }
      })
  }

  redirectToForm(action: string) {
    this.router.navigate([`${this.router.routerState.snapshot.url}/${action}`]);
  };

  delete(item: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain) {
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
          },

        });
      }
    );
  }

  sourceItem: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain;
  onForm(action: string, item: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain = null) {
    this.sourceItem = item != null ? { ...item } : <D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain>{};
    this.redirectToForm(action);
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }

  onYearMonthChange() {
    this.param.year_Month = this.functionUtility.isValidDate(this.year_Month) ? this.year_Month.toStringYearMonth() : ''
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  clear() {
    this.data = []
    this.param = <D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam>{}
    this.pagination.totalCount = 0
    this.pagination.pageNumber = 1
    this.year_Month = null
  }

  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  deleteProperty = (name: string) => delete this.param[name]
}

