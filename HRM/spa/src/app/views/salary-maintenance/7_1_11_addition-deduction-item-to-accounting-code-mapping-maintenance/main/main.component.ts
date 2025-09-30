import { Component, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService } from '@services/salary-maintenance/s_7_1_11_addition-deduction-item-to-accounting-code-mapping-maintenance.service';
import {
  AdditionDeductionItemToAccountingCodeMappingMaintenanceDto,
  AdditionDeductionItemToAccountingCodeMappingMaintenanceMemory,
  AdditionDeductionItemToAccountingCodeMappingMaintenanceParam,
} from '@models/salary-maintenance/7_1_11_addition-deduction-item-to-accouting-code-mapping-maintenance';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { IconButton } from '@constants/common.constants';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  param: AdditionDeductionItemToAccountingCodeMappingMaintenanceParam = <AdditionDeductionItemToAccountingCodeMappingMaintenanceParam>{};

  pagination: Pagination = <Pagination>{}

  dataMain: AdditionDeductionItemToAccountingCodeMappingMaintenanceDto[] = [];
  selectedData: AdditionDeductionItemToAccountingCodeMappingMaintenanceDto;

  iconButton = IconButton;

  listFactory: KeyValuePair[] = [];
  constructor(
    private service: S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
        this.processData()
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    this.loadDropdownList();
    this.processData()
  }
  processData() {
    if (this.dataMain.length > 0) {
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
  ngOnDestroy(): void {
    this.service.setParamSearch(<AdditionDeductionItemToAccountingCodeMappingMaintenanceMemory>{
      pagination: this.pagination,
      param: this.param,
      data: this.dataMain,
      selectedData: this.selectedData
    });
  }

  search = (isSearch: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch, false) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }
  getData = (isSearch?: boolean, isDelete?: boolean) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.service.getDataPagination(this.pagination, this.param).subscribe({
        next: (res) => {
          this.dataMain = res.result;
          this.dataMain.map(x => {
            x.update_Time = new Date(x.update_Time);
            x.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(x.update_Time))
          })
          this.pagination = res.pagination;
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
          if (isDelete)
            this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
          this.spinnerService.hide()
          resolve()
        },
        error: () => { reject() }
      })
    })
  };

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: AdditionDeductionItemToAccountingCodeMappingMaintenanceDto) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(item: AdditionDeductionItemToAccountingCodeMappingMaintenanceDto, isDelete: boolean) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: res => {
          if (res.isSuccess) {
            this.getData(false, isDelete);
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
          }
          this.spinnerService.hide();
        }
      });
    });
  }

  clear() {
    this.dataMain = [];
    this.param = <AdditionDeductionItemToAccountingCodeMappingMaintenanceParam>{};
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
  }

  loadDropdownList() {
    this.getListFactory();
  }

  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
