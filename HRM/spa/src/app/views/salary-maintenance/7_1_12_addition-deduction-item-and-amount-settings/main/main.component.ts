import { Component, OnInit } from '@angular/core';
import { S_7_1_12_AdditionDeductionItemAndAmountSettingsService } from '@services/salary-maintenance/s_7_1_12_addition-deduction-item-and-amount-settings.service';
import { InjectBase } from '@utilities/inject-base-app';
import {
  AdditionDeductionItemAndAmountSettings_MainData,
  AdditionDeductionItemAndAmountSettings_MainMemory,
  AdditionDeductionItemAndAmountSettings_MainParam
} from "@models/salary-maintenance/7_1_12_addition-deduction-item-and-amount-settings";
import { KeyValuePair } from '@utilities/key-value-pair';
import { IconButton, Placeholder } from '@constants/common.constants';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  param: AdditionDeductionItemAndAmountSettings_MainParam = <AdditionDeductionItemAndAmountSettings_MainParam>{};

  pagination: Pagination = <Pagination>{}

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };
  effective_Month: Date

  dataMain: AdditionDeductionItemAndAmountSettings_MainData[] = [];
  selectedData: AdditionDeductionItemAndAmountSettings_MainData;

  iconButton = IconButton;
  placeholder = Placeholder;

  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listAdditionsAndDeductionsType: KeyValuePair[] = [];
  listAdditionsAndDeductionsItem: KeyValuePair[] = [];

  constructor(
    private service: S_7_1_12_AdditionDeductionItemAndAmountSettingsService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
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

  ngOnDestroy(): void {
    this.service.setParamSearch(<AdditionDeductionItemAndAmountSettings_MainMemory>{
      pagination: this.pagination,
      param: { ...this.param },
      data: this.dataMain,
      selectedData: this.selectedData
    });
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    if (this.functionUtility.isValidDate(new Date(this.param.effective_Month)))
      this.effective_Month = new Date(this.param.effective_Month);
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
    return !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.param.addDed_Type) &&
      this.param.permission_Group.length > 0;
  }


  search = (isSearch: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch, false) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }
  getData = (isSearch?: boolean, isDelete?: boolean) => {
    this.spinnerService.show();
    this.service.getDataPagination(this.pagination, this.param).subscribe({
      next: (res) => {
        this.dataMain = res.result;
        this.dataMain.map(x => {
          x.effective_Month = new Date(x.effective_Month),
            x.effective_Month_Str = (new Date(x.effective_Month)).toStringYearMonth(),
            x.update_Time = new Date(x.update_Time);
          x.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(x.update_Time))
        })
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        if (isDelete)
          this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
        this.spinnerService.hide()
      }
    })
  };


  clear() {
    this.dataMain = [];
    this.param = <AdditionDeductionItemAndAmountSettings_MainParam>{
      permission_Group: []
    };
    this.effective_Month = null;
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  //#region Add, Edit, Delete
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: AdditionDeductionItemAndAmountSettings_MainData) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(item: AdditionDeductionItemAndAmountSettings_MainData, isDelete: boolean) {
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
  download() {
    if (this.dataMain.length == 0 && this.functionUtility.checkFunction('Search'))
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download');
        result.isSuccess ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }

  //#endregion

  //#region OnChange
  onChangeFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onChangeEffectiveMonth() {
    this.param.effective_Month = this.functionUtility.isValidDate(this.effective_Month) ? this.effective_Month.toStringYearMonth() : ''
  }
  //#endregion

  //#region Load & get list
  loadDropdownList() {
    this.getListFactory();
    this.getListPermissionGroup();
    this.getListSalaryType();
    this.getListAdditionsAndDeductionsType();
    this.getListAdditionsAndDeductionsItem();
  }

  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroupByFactory(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res;
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    });
  }

  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }

  getListAdditionsAndDeductionsType() {
    this.service.getListAdditionsAndDeductionsType().subscribe({
      next: res => {
        this.listAdditionsAndDeductionsType = res;
      }
    });
  }

  getListAdditionsAndDeductionsItem() {
    this.service.getListAdditionsAndDeductionsItem().subscribe({
      next: res => {
        this.listAdditionsAndDeductionsItem = res;
      }
    });
  }
  //#endregion

}
