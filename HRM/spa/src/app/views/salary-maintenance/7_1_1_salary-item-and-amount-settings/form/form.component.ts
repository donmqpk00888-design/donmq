import { S_7_1_1_SalaryItemAndAmountSettings } from '@services/salary-maintenance/s_7_1_1_salary-item-and-amount-settings.service';
import {
  SalaryItemAndAmountSettings_SubData,
  SalaryItemAndAmountSettings_SubParam
} from '@models/salary-maintenance/7_1_1_salary-item-and-amount-settings';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { NgForm } from '@angular/forms';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  @ViewChild('paramForm') public paramForm: NgForm;
  tempUrl: string = ''

  title: string = '';

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  iconButton = IconButton;
  classButton = ClassButton;

  param: SalaryItemAndAmountSettings_SubParam = <SalaryItemAndAmountSettings_SubParam>{}
  data: SalaryItemAndAmountSettings_SubData[] = []

  formType: string = '';

  factoryList: KeyValuePair[] = [];
  salaryTypeList: KeyValuePair[] = [];
  salaryItemList: KeyValuePair[] = [];
  permissionGroupList: KeyValuePair[] = [];
  insuranceList: KeyValuePair[] = [
    { key: "Y", value: "Yes" },
    { key: "N", value: "No" }
  ];

  kindList: KeyValuePair[] = [];

  isDuplicated: boolean = false

  constructor(private service: S_7_1_1_SalaryItemAndAmountSettings) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.formType = role.title
        this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
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
        this.param.effective_Month_Str = this.functionUtility.getDateFormat(new Date(this.param.effective_Month))
        this.data = selectedData.data
        this.data.map((val: SalaryItemAndAmountSettings_SubData) => {
          val.update_Time = new Date(val.update_Time)
          val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time))
        })
        this.checkData()
      }
      else
        this.back()
    }
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.permissionGroupList = structuredClone(keys.filter((x: { key: string; }) => x.key == "PE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryTypeList = structuredClone(keys.filter((x: { key: string; }) => x.key == "ST")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryItemList = structuredClone(keys.filter((x: { key: string; }) => x.key == "SI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.kindList = structuredClone(keys.filter((x: { key: string; }) => x.key == "KI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  add() {
    this.data.push(<SalaryItemAndAmountSettings_SubData>{ amount: '0', seq: this.data.length + '', insurance: 'Y' })
  }
  remove(index: number) {
    this.data.splice(index, 1)
    this.checkData()
  }
  onValueChange(item: SalaryItemAndAmountSettings_SubData) {
    if (typeof item.amount === 'number') {
      item.amount = item.amount.toString();
    }
    item.update_By = this.user.id
    item.update_Time = new Date();
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date())
  }

  checkData() {
    this.isDuplicated = false
    if (this.data.length > 0) {
      this.data.map(x => x.is_Duplicate = false)
      const lookup = this.data.reduce((a, e) => {
        a[e.salary_Item] = ++a[e.salary_Item] || 0;
        return a;
      }, {});
      const deplicateValues = this.data.filter(e => lookup[e.salary_Item])
      if (deplicateValues.length > 1) {
        this.isDuplicated = true
        deplicateValues.map(x => x.is_Duplicate = true)
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.SalaryItemAndAmountSettings.DuplicateInput')
      }
    }
  }

  deleteItemProperty(item: SalaryItemAndAmountSettings_SubData, name: string) {
    delete item[name]
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  retryGetDropDownList() {
    this.service.getDropDownList(this.formType)
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }

  validateSalaryDays(value: number) {
    if (value > 31)
      this.param.salary_Days = '31'
    const now = new Date()
    this.data.forEach(x => {
      x.update_By = this.user.id
      x.update_Time = now;
      x.update_Time_Str = this.functionUtility.getDateTimeFormat(now)
    })
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
              this.translateService.instant(`SalaryMaintenance.SalaryItemAndAmountSettings.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      })
  }
  back = () => this.router.navigate([this.tempUrl]);

  async onPropertyChange() {
    if (this.paramForm.form.valid) {
      this.isDuplicated = await this.isDuplicatedData()
      if (this.isDuplicated) {
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.SalaryItemAndAmountSettings.DuplicateInput')
      }
      else {
        this.checkData()
      }
    }
  }
  onKeyChange(item: SalaryItemAndAmountSettings_SubData) {
    this.onValueChange(item)
    this.checkData()
  }
  isDuplicatedData(): Promise<boolean> {
    return new Promise((resolve) => {
      this.spinnerService.show()
      this.service.isDuplicatedData(this.param)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.data = res.isSuccess ? [] : res.data
            this.data.map((val: SalaryItemAndAmountSettings_SubData) => val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time)))
            resolve(res.isSuccess)
          }
        });
    })
  }
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.param[name])) ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
    this.onPropertyChange()
  }
}

