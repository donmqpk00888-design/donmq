import { Component, OnDestroy, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_3_Leave_Salary_Calculation_MaintenanceService } from '@services/salary-maintenance/s_7_1_3_leave_salary_calculation_maintenance.service';
import { IconButton } from '@constants/common.constants';
import { LeaveSalaryCalculationMaintenance_Basic, LeaveSalaryCalculationMaintenanceDTO, LeaveSalaryCalculationMaintenanceParam } from '@models/salary-maintenance/7_1_3_leave_salary_calculation_maintenance';
import { Pagination } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  listLeaveCode: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  param: LeaveSalaryCalculationMaintenanceParam = <LeaveSalaryCalculationMaintenanceParam>{};
  data: LeaveSalaryCalculationMaintenanceDTO[] = [];
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  selectedData: LeaveSalaryCalculationMaintenanceDTO = <LeaveSalaryCalculationMaintenanceDTO>{};
  constructor(
    private service: S_7_1_3_Leave_Salary_Calculation_MaintenanceService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.processData()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
    this.getDropDownList()
  }
  ngOnDestroy(): void {
    this.service.setSource(<LeaveSalaryCalculationMaintenance_Basic>{
      pagination: this.pagination,
      param: this.param,
      data: this.data,
      selectedData: this.selectedData
    });
  }
  getSource() {
    this.param = this.service.paramSource().param;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
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
    return !this.functionUtility.checkEmpty(this.param.factory)
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
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
  getDropDownList() {
    this.getListFactory();
    this.getListLeaveCode();
  }
  getListLeaveCode() {
    this.service.getListLeaveCode().subscribe({
      next: (res) => {
        this.listLeaveCode = res
      },
    });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  clear() {
    this.param = <LeaveSalaryCalculationMaintenanceParam>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  deleteProperty = (name: string) => delete this.param[name]
  onForm(item: LeaveSalaryCalculationMaintenanceDTO = null) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }

  delete(item: LeaveSalaryCalculationMaintenanceDTO) {
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

}
