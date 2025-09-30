import { S_7_1_27_FinSalaryAttributionCategoryMaintenance } from '@services/salary-maintenance/s_7_1_27_fin-salary-attribution-category-maintenance.service';
import {
  FinSalaryAttributionCategoryMaintenance_Data,
  FinSalaryAttributionCategoryMaintenance_Param
} from '@models/salary-maintenance/7_1_27_fin-salary-attribution-category-maintenance';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
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
  formType: string = '';

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  iconButton = IconButton;
  classButton = ClassButton;

  param: FinSalaryAttributionCategoryMaintenance_Param = <FinSalaryAttributionCategoryMaintenance_Param>{}
  data: FinSalaryAttributionCategoryMaintenance_Data[] = []

  factoryList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  kindList: KeyValuePair[] = [];
  salaryCategoryList: KeyValuePair[] = [];
  kindCodeList: KeyValuePair[] = [];

  isDuplicated: boolean = false

  constructor(private service: S_7_1_27_FinSalaryAttributionCategoryMaintenance) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
      this.getDepartmentList()
      this.getKindCodeList()
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.formType = role.title
        this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
        this.filterList(role.dataResolved)
      })
  }
  private retryGetDropDownList() {
    this.service.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  private getDepartmentList() {
    if (this.param.factory) {
      this.service
        .getDepartmentList(this.param)
        .subscribe({
          next: (res) => {
            this.departmentList = res;
          }
        });
    }
  }
  private getKindCodeList() {
    if (this.param.kind) {
      this.service
        .getKindCodeList(this.param)
        .subscribe({
          next: (res) => {
            this.kindCodeList = res;
          }
        });
    }
  }
  private filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.kindList = structuredClone(keys.filter((x: { key: string; }) => x.key == "ME")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryCategoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "SA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  add() {
    this.data.push(<FinSalaryAttributionCategoryMaintenance_Data>{
      update_By: this.user.id,
      update_Time: this.functionUtility.getDateTimeFormat(new Date())
    })
  }
  async remove(index: number) {
    this.data.splice(index, 1)
    await this.checkDuplicate()
  }
  save() {
    this.spinnerService.show();
    this.service.postData(this.param, this.data)
      .subscribe({
        next: async (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.back()
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(`System.Message.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      })
  }
  back = () => this.router.navigate([this.tempUrl]);
  private resetDataRow(isFactory: boolean = false) {
    const now = new Date()
    this.data.forEach(x => {
      x.update_Time = this.functionUtility.getDateTimeFormat(now)
      x.is_Duplicate = false
      isFactory ? delete x.department : delete x.kind_Code
    })
    const temp = this.data.filter(x => !x.department && !x.kind_Code)
    if (temp.length == this.data.length && this.data.length > 0)
      this.data = [<FinSalaryAttributionCategoryMaintenance_Data>{
        update_By: this.user.id,
        update_Time: this.functionUtility.getDateTimeFormat(now)
      }]
  }
  onValueChange(item: FinSalaryAttributionCategoryMaintenance_Data) {
    item.update_By = this.user.id
    item.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
  onFactoryChange() {
    this.getDepartmentList()
    this.resetDataRow(true)
  }
  onKindChange() {
    this.getKindCodeList()
    this.resetDataRow()
  }
  async onKeyChange(item: FinSalaryAttributionCategoryMaintenance_Data) {
    this.onValueChange(item)
    await this.checkDuplicate()
  }
  private async checkDuplicate() {
    if (this.data.length == 0)
      this.isDuplicated = false
    else {
      this.data.map(x => x.is_Duplicate = false)
      const lookup = this.data.reduce((a, e) => {
        if (e.department && e.kind_Code)
          a[e.department + e.kind_Code] = ++a[e.department + e.kind_Code] || 0;
        return a;
      }, {});
      const deplicateValues = this.data.filter(e => lookup[e.department + e.kind_Code])
      if (deplicateValues.length > 1)
        deplicateValues.map(x => x.is_Duplicate = true)
      await Promise.all(this.data.filter(x => !x.is_Duplicate).map(async (x): Promise<void> => {
        await this.checkDatabase(x)
      }));
      if (this.isDuplicated = this.data.some(x => x.is_Duplicate)) {
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'System.Message.DuplicateInput')
      }
    }
  }
  private checkDatabase(item: FinSalaryAttributionCategoryMaintenance_Data): Promise<void> {
    return new Promise((resolve, reject) => {
      if (this.paramForm.form.valid && item.department && item.kind_Code) {
        this.spinnerService.show();
        this.service.isExistedData(this.param.factory, this.param.kind, item.department, item.kind_Code)
          .subscribe({
            next: (res) => {
              this.spinnerService.hide();
              item.is_Duplicate = res
              resolve()
            },
            error: () => { reject() }
          });
      }
    })
  }
  deleteItemProperty(item: FinSalaryAttributionCategoryMaintenance_Data, name: string) {
    delete item[name]
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}

