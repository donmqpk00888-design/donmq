import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton, Placeholder } from '@constants/common.constants';
import {
  MonthlySalaryMaintenance_Basic,
  MonthlySalaryMaintenance_Delete,
  MonthlySalaryMaintenanceDto,
  MonthlySalaryMaintenanceParam
} from '@models/salary-maintenance/7_1_24_monthly-salary-maintenance';
import { S_7_1_24_MonthlySalaryMaintenanceService } from '@services/salary-maintenance/s_7_1_24_monthly-salary-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  sal_Month_Value: Date;
  param: MonthlySalaryMaintenanceParam = <MonthlySalaryMaintenanceParam>{};
  data: MonthlySalaryMaintenanceDto[] = [];
  selectedData: MonthlySalaryMaintenanceDto = <MonthlySalaryMaintenanceDto>{};
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  placeholder = Placeholder

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  source: MonthlySalaryMaintenance_Basic;
  sourceItem: MonthlySalaryMaintenanceDto;
  constructor(
    private service: S_7_1_24_MonthlySalaryMaintenanceService,
  ) {
    super();
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
    this.service.setSource(<MonthlySalaryMaintenance_Basic>{
      param: this.param,
      selectedData: this.selectedData,
      pagination: this.pagination,
      data: this.data,
    });
  }

  getSource() {
    this.param = this.service.paramSource().param;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
    if (this.functionUtility.isValidDate(new Date(this.param.salMonth)))
      this.sal_Month_Value = new Date(this.param.salMonth)
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
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }

  getListDepartment() {
    this.listDepartment = []
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.service.getListDepartment(this.param.factory).subscribe({
        next: (res) => {
          this.listDepartment = res
        },
      });
    }
  }

  getListPermissionGroup() {
    this.listPermissionGroup = []
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.service.getListPermissionGroup(this.param.factory).subscribe({
        next: res => {
          this.listPermissionGroup = res
          this.selectAllForDropdownItems(this.listPermissionGroup)
        }
      })
    }
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory) &&
      this.functionUtility.isValidDate(new Date(this.param.salMonth)) &&
      this.param.permission_Group.length > 0
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show();
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

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clear() {
    this.param = <MonthlySalaryMaintenanceParam>{}
    this.sal_Month_Value = null;
    this.param.permission_Group = []
    this.pagination.pageNumber = 1;
    this.data = [];
    this.pagination.totalCount = 0;
  }
  deleteProperty = (name: string) => delete this.param[name]



  onChangeSalMonth() {
    this.param.salMonth = !this.functionUtility.checkEmpty(this.sal_Month_Value)
      ? this.functionUtility.getDateFormat(this.sal_Month_Value)
      : "";
  }

  onFactoryChange() {
    this.deleteProperty('department')
    this.deleteProperty('permission_Group')
    this.getListDepartment();
    this.getListPermissionGroup();
  }

  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }
  onForm(action: string, item: MonthlySalaryMaintenanceDto = null) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${action}`]);
  }
  delete(item: MonthlySalaryMaintenanceDto) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        let deleteItem = <MonthlySalaryMaintenance_Delete>{
          sal_Month: this.functionUtility.getDateFormat(item.sal_Month.toDate()),
          employee_ID: item.employee_ID,
          factory: item.factory,
        }
        this.service.delete(deleteItem).subscribe({
          next: (result) => {
            this.spinnerService.hide()
            this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
            if (result.isSuccess)
              this.getData(false);
          },
        });
      }
    );
  }
}
