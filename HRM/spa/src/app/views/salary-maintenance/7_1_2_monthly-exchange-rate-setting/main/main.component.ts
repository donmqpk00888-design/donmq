import { MonthlyExchangeRateSetting_Update } from '@models/salary-maintenance/7_1_2_monthly-exchange-rate-setting';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { MonthlyExchangeRateSetting_Main, MonthlyExchangeRateSetting_Memory, MonthlyExchangeRateSetting_Param } from '@models/salary-maintenance/7_1_2_monthly-exchange-rate-setting';
import { Pagination } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { S_7_1_2_MonthlyExchangeRateSetting } from '@services/salary-maintenance/s_7_1_2_monthly-exchange-rate-setting.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  tempUrl: string = this.router.url

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};
  minMode: BsDatepickerViewMode = 'month';
  pagination: Pagination = <Pagination>{};

  iconButton = IconButton;
  classButton = ClassButton;

  param: MonthlyExchangeRateSetting_Param = <MonthlyExchangeRateSetting_Param>{};
  data: MonthlyExchangeRateSetting_Main[] = [];
  selectedData: MonthlyExchangeRateSetting_Update = <MonthlyExchangeRateSetting_Update>{};

  kindList: KeyValuePair[] = [];
  currencyList: KeyValuePair[] = [];

  title: string = ''
  formType: string = ''

  constructor(
    private service: S_7_1_2_MonthlyExchangeRateSetting
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
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM',
        minMode: this.minMode
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.formType = role.title
        this.filterList(role.dataResolved)
        this.getSource()
      });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<MonthlyExchangeRateSetting_Memory>{
      param: this.param,
      selectedData: this.selectedData,
      pagination: this.pagination,
      data: this.data,
      tempUrl: this.tempUrl
    });
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.data = this.service.paramSearch().data;
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
    this.service.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.rate_Month_Str)
  }
  clear() {
    this.param = <MonthlyExchangeRateSetting_Param>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  filterList(keys: KeyValuePair[]) {
    this.kindList = structuredClone(keys.filter((x: { key: string; }) => x.key == "KI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.currencyList = structuredClone(keys.filter((x: { key: string; }) => x.key == "CR")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(item: MonthlyExchangeRateSetting_Main) {
    this.spinnerService.show()
    this.service.isExistedData(item)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.selectedData = res.data
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
          }
          else {
            this.getData(false)
            this.snotifyService.error(
              this.translateService.instant('SalaryMaintenance.MonthlyExchangeRateSetting.NotExitedData'),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  remove(item: MonthlyExchangeRateSetting_Main) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show();
      this.service.deleteData(item).subscribe({
        next: async (res) => {
          if (res.isSuccess) {
            await this.getData(false)
            this.snotifyService.success(
              this.translateService.instant('System.Message.DeleteOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.LeaveApplicationMaintenance.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
          this.spinnerService.hide();
        }
      });
    });
  }
  search = () => {
    this.pagination.pageNumber == 1
      ? this.getData(true)
      : this.pagination.pageNumber = 1;
  };
  getData = (isSearch: boolean) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.service
        .getSearchDetail(this.pagination, this.param)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.pagination = res.pagination;
            this.data = res.result;
            if (isSearch)
              this.snotifyService.success(
                this.translateService.instant('System.Message.SearchOKMsg'),
                this.translateService.instant('System.Caption.Success')
              );
            resolve()
          },
          error: () => { reject() }
        });
    })
  };
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  };
  onDateChange() {
    this.param.rate_Month_Str = this.functionUtility.isValidDate(new Date(this.param.rate_Month)) ? this.functionUtility.getDateFormat(new Date(this.param.rate_Month)) : '';
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
