import { Component, OnDestroy, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService } from '@services/salary-maintenance/s_7_1_10_salary-item-to-accounting-code-mapping-maintenance.service';
import { IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import {
  SalaryItemToAccountingCodeMappingMaintenanceDto,
  SalaryItemToAccountingCodeMappingMaintenanceParam,
  SalaryItemToAccountingCodeMappingMaintenance_Main_Memory
} from '@models/salary-maintenance/7_1_10_salary-item-to-accounting-code-mapping-maintenance';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  iconButton = IconButton;
  factoryList: KeyValuePair[] = [];
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  param: SalaryItemToAccountingCodeMappingMaintenanceParam = <SalaryItemToAccountingCodeMappingMaintenanceParam>{}
  data: SalaryItemToAccountingCodeMappingMaintenanceDto[] = [];
  selectedData: SalaryItemToAccountingCodeMappingMaintenanceDto;

  constructor(private service: S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getFactory();
      this.processData();
    });
  }

  ngOnDestroy(): void {
    this.service.signalDataMain.set(<SalaryItemToAccountingCodeMappingMaintenance_Main_Memory>{
      data: this.data,
      pagination: this.pagination,
      param: this.param,
      selectedData: this.selectedData
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }
  getSource() {
    this.param = this.service.signalDataMain().param;
    this.pagination = this.service.signalDataMain().pagination;
    this.data = this.service.signalDataMain().data;
    this.getFactory();
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory);
  }
  getFactory() {
    this.service.getFactory().subscribe({
      next: res => {
        this.factoryList = res;
      }
    })
  }

  clear() {
    this.param = <SalaryItemToAccountingCodeMappingMaintenanceParam>{}
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getDataPagination(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch) {
          this.functionUtility.snotifySuccessError(isSearch, 'System.Message.QuerySuccess');
        }
      }
    });
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  onForm(item: SalaryItemToAccountingCodeMappingMaintenanceDto = null) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }

  onDelete(item: SalaryItemToAccountingCodeMappingMaintenanceDto) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item.factory, item.salary_Item, item.dC_Code).subscribe({
        next: res => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg');
          if (res.isSuccess) this.getData();
        }
      });
    });
  }
}
