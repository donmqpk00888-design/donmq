
import { Component, OnInit, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { NgForm } from '@angular/forms';
import { S_7_1_2_MonthlyExchangeRateSetting } from '@services/salary-maintenance/s_7_1_2_monthly-exchange-rate-setting.service';
import { MonthlyExchangeRateSetting_Main, MonthlyExchangeRateSetting_Param } from '@models/salary-maintenance/7_1_2_monthly-exchange-rate-setting';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  @ViewChild('subForm') public subForm: NgForm;
  tempUrl: string = ''

  title: string = ''
  bsConfigMonth: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  bsConfigDay: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD'
  };

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  iconButton = IconButton;
  classButton = ClassButton;

  param: MonthlyExchangeRateSetting_Param = <MonthlyExchangeRateSetting_Param>{}
  data: MonthlyExchangeRateSetting_Main[] = []

  formType: string = '';

  kindList: KeyValuePair[] = [];
  currencyList: KeyValuePair[] = [];

  isDuplicated: boolean = false

  constructor(private service: S_7_1_2_MonthlyExchangeRateSetting) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.formType = role.title
        this.filterList(role.dataResolved)
        this.getSource()
      })
  }
  getSource() {
    if (this.formType == 'Edit') {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        let selectedData = structuredClone(source.selectedData);
        this.param = selectedData.param
        this.param.rate_Month_Str = this.functionUtility.getDateFormat(new Date(this.param.rate_Month))
        this.data = selectedData.data
        this.data.map((val: MonthlyExchangeRateSetting_Main) => {
          val.rate_Month_Str = this.functionUtility.getDateFormat(new Date(val.rate_Month))
          val.rate_Date_Str = this.functionUtility.getDateFormat(new Date(val.rate_Date))
          val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time))
        })
        this.checkData()
      }
      else
        this.back()
    }
  }
  filterList(keys: KeyValuePair[]) {
    this.kindList = structuredClone(keys.filter((x: { key: string; }) => x.key == "KI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.currencyList = structuredClone(keys.filter((x: { key: string; }) => x.key == "CR")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  add() {
    this.data.push(<MonthlyExchangeRateSetting_Main>{
      rate_Month: this.param.rate_Month,
      rate_Month_Str: this.param.rate_Month_Str,
      update_By: this.user.id,
      update_Time_Str: this.functionUtility.getDateTimeFormat(new Date())
    })
  }
  remove(item: MonthlyExchangeRateSetting_Main) {
    this.data = this.data.filter(val => val != item)
    this.checkData()
  }
  deleteItemProperty(item: MonthlyExchangeRateSetting_Main, name: string) {
    delete item[name]
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  getDropDownList() {
    this.service.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }

  save() {
    this.spinnerService.show();
    this.service[this.formType == 'Add' ? 'postData' : 'putData'](this.param, this.data)
      .subscribe({
        next: async (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.back()
            this.snotifyService.success(
              this.translateService.instant(this.formType == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(`SalaryMaintenance.MonthlyExchangeRateSetting.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      })
  }
  back = () => this.router.navigate([this.tempUrl]);

  onKeyChange(item: MonthlyExchangeRateSetting_Main) {
    this.onValueChange(item)
    this.checkData()
  }
  isDuplicatedData(item: MonthlyExchangeRateSetting_Main): Promise<boolean> {
    return new Promise((resolve) => {
      this.service.isDuplicatedData(item)
        .subscribe({
          next: (res) => {
            resolve(res.isSuccess)
          }
        });
    })
  }
  onValueChange(item: MonthlyExchangeRateSetting_Main) {
    item.update_By = this.user.id
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date())
  }
  async checkData() {
    this.isDuplicated = false
    if (this.data.length > 0) {
      this.data.map(x => x.is_Duplicate = false)
      const lookup = this.data.reduce((a, e) => {
        a[e.kind + e.currency + e.exchange_Currency] = ++a[e.kind + e.currency + e.exchange_Currency] || 0;
        return a;
      }, {});
      const deplicateValues = this.data.filter(e => lookup[e.kind + e.currency + e.exchange_Currency])
      if (deplicateValues.length > 1) {
        this.isDuplicated = true
        deplicateValues.map(x => x.is_Duplicate = true)
      }
      await Promise.all(this.data.map(async (item) => {
        if (item.kind && item.currency && item.exchange_Currency && await this.isDuplicatedData(item)) this.isDuplicated = item.is_Duplicate = true;
      }))
      if (this.isDuplicated) {
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.MonthlyExchangeRateSetting.DuplicateInput')
      }
    }
  }
  onRateMonthChange() {
    this.onDateChange(this.param, 'rate_Month')
    this.data.map(x => {
      x.rate_Month = this.param.rate_Month
      x.rate_Month_Str = this.param.rate_Month_Str
    })
    this.checkData()
  }
  onRateDateChange(item: MonthlyExchangeRateSetting_Main) {
    this.onDateChange(item, 'rate_Date')
    this.onValueChange(item)
  }
  onDateChange(item: any, name: string) {
    item[`${name}_Str`] = this.functionUtility.isValidDate(new Date(item[name])) ? this.functionUtility.getDateFormat(new Date(item[name])) : '';
  }
}

