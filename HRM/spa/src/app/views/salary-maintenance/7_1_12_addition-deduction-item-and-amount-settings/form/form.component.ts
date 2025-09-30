import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { AdditionDeductionItemAndAmountSettings_SubData, AdditionDeductionItemAndAmountSettings_SubParam } from '@models/salary-maintenance/7_1_12_addition-deduction-item-and-amount-settings';
import { S_7_1_12_AdditionDeductionItemAndAmountSettingsService } from '@services/salary-maintenance/s_7_1_12_addition-deduction-item-and-amount-settings.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss',
})
export class FormComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  param: AdditionDeductionItemAndAmountSettings_SubParam = <AdditionDeductionItemAndAmountSettings_SubParam>{};
  data: AdditionDeductionItemAndAmountSettings_SubData[] = []

  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listAdditionsAndDeductionsType: KeyValuePair[] = [];
  listAdditionsAndDeductionsItem: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  title: string = '';
  url: string = '';
  formType: string = '';
  isEdit: boolean = false;
  isDuplicated: boolean = false;
  effective_Month: Date;
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  onjob_Print_List: KeyValuePair[] = [
    { key: "Y", value: "Yo" },
    { key: "N", value: "No" }
  ];
  resigned_Print_List: KeyValuePair[] = [
    { key: "Y", value: "Yr" },
    { key: "N", value: "Nr" }
  ];
  constructor(
    private service: S_7_1_12_AdditionDeductionItemAndAmountSettingsService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
      this.getSource()
    });
  }
  async getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        const dto = structuredClone(source.selectedData) as AdditionDeductionItemAndAmountSettings_SubParam;
        await this.getDetail(dto)
      } else this.back()
    }
    this.loadDropdownList()
  }
  getDetail(dto: AdditionDeductionItemAndAmountSettings_SubParam): Promise<boolean> {
    return new Promise((resolve) => {
      this.spinnerService.show();
      this.service.getDetail(dto).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (!this.functionUtility.isEmptyObject(res)) {
            this.data = res.data;
            this.param = res.param;
            this.param.effective_Month = new Date(this.param.effective_Month);
            this.param.effective_Month_Str = this.functionUtility.getDateFormat(this.param.effective_Month)
          } else {
            this.back();
          }
          resolve(true)
        }
      })
    })
  }

  checkData() {
    if (this.param.factory && this.param.permission_Group && this.param.salary_Type && this.param.effective_Month) {
      this.param.effective_Month_Str = this.functionUtility.getDateFormat(new Date(this.param.effective_Month))
      this.service.checkData(this.param).subscribe(res => {
        if (res.isSuccess) {
          this.data = res.data
          this.isDuplicated = false;
        }
        else {
          this.data = [];
          this.isDuplicated = true;
          this.snotifyService.error(
            this.translateService.instant(res.error),
            this.translateService.instant('System.Caption.Error')
          );
        }
      });
    }
    else {
      this.data = [];
    }
  }

  back = () => this.router.navigate([this.url]);

  clear() {
    this.param = <AdditionDeductionItemAndAmountSettings_SubParam>{}
  }

  onKeyChange(item: AdditionDeductionItemAndAmountSettings_SubData) {
    this.onDataChange(item);
    this.checkDuplicate()
  }

  checkDuplicate() {
    this.isDuplicated = false;
    if (this.data.length > 0) {
      this.data.map(x => x.is_Duplicate = false);

      const lookup = this.data.reduce((a, e) => {
        const key = `${e.addDed_Item}-${e.addDed_Type}`;
        a[key] = ++a[key] || 0;
        return a;
      }, {});

      const duplicateValues = this.data.filter(e => {
        const key = `${e.addDed_Item}-${e.addDed_Type}`;
        return lookup[key];
      });

      if (duplicateValues.length > 1) {
        this.isDuplicated = true;

        duplicateValues.map(x => x.is_Duplicate = true);

        this.snotifyService.clear();
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput');
      }
    }
  }

  onDataChange(item: AdditionDeductionItemAndAmountSettings_SubData) {
    item.update_By = this.user.id;
    item.update_Time = new Date();
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(
      item.update_Time
    );
  }

  onChangeFactory() {
    this.deleteProperty(this.param, 'permission_Group')
    this.getListPermissionGroup();
    this.checkData()
  }

  onChangeEffectiveMonth() {
    this.param.effective_Month_Str = this.functionUtility.isValidDate(this.effective_Month) ? this.effective_Month.toStringYearMonth() : ''
    this.checkData()
  }

  onChangeAmount(item: AdditionDeductionItemAndAmountSettings_SubData) {
    this.onDataChange(item);
    if (item.amount > 2147483647)
      item.amount = 2147483647;
  }

  addNew() {
    const current = new Date();
    this.data.push(<AdditionDeductionItemAndAmountSettings_SubData>{
      amount: 0,
      update_By: this.user.id,
      update_Time: current,
      update_Time_Str: this.functionUtility.getDateTimeFormat(current),
      isNew: true,
      onjob_Print: 'N',
      resigned_Print: 'N'
    })
  }

  remove(index: number) {
    this.data.splice(index, 1)
    this.checkDuplicate()
  }

  //#region Add & Edit
  save() {
    if (!this.isEdit) {
      this.add();
    } else {
      this.edit();
    }
  }

  add() {
    this.spinnerService.show();
    this.service
      .create(this.param, this.data)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.back();
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(
                res.error ?? 'System.Message.CreateErrorMsg'
              ),
              this.translateService.instant('System.Caption.Error')
            );
          }
        }
      })
  }

  edit() {
    this.spinnerService.show();
    this.service.update(this.param, this.data).subscribe({
      next: async (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.back();
          this.snotifyService.success(
            this.translateService.instant('System.Message.UpdateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(
            this.translateService.instant(
              res.error ?? 'System.Message.UpdateErrorMsg'
            ),
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
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
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListPermissionGroup() {
    this.service
      .getListPermissionGroupByFactory(this.param.factory)
      .subscribe({
        next: (res) => {
          this.listPermissionGroup = res;
        }
      });
  }

  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: (res) => {
        this.listSalaryType = res;
      },
    });
  }

  getListAdditionsAndDeductionsType() {
    this.service.getListAdditionsAndDeductionsType().subscribe({
      next: (res) => {
        this.listAdditionsAndDeductionsType = res;
      },
    });
  }

  getListAdditionsAndDeductionsItem() {
    this.service.getListAdditionsAndDeductionsItem().subscribe({
      next: (res) => {
        this.listAdditionsAndDeductionsItem = res;
      },
    });
  }
  //#endregion
  deleteProperty(item: any, name: string) {
    delete item[name]
  }
}
