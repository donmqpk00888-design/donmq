import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { IncomeTaxBracketSetting_SubData, IncomeTaxBracketSettingDto } from '@models/salary-maintenance/7_1_13_income-tax-bracket-setting';
import { S_7_1_13_IncomeTaxBracketSettingService } from '@services/salary-maintenance/s_7_1_13_income-tax-bracket-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  data: IncomeTaxBracketSettingDto = <IncomeTaxBracketSettingDto>{ subData: [] };

  listNationality: KeyValuePair[] = [];
  listTaxCode: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  isEdit: boolean = false;
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;
  constructor(private service: S_7_1_13_IncomeTaxBracketSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
      this.getSource()
    });
  }
  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      } else this.back()
    }
    this.loadDropdownList()
  }

  back = () => this.router.navigate([this.tempUrl]);

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
      .create(this.data)
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
    this.service.update(this.data).subscribe({
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
  remove(index: number) {
    this.data.subData.splice(index, 1)
    this.isDuplicate()
  }
  addItem() {
    this.data.subData.push(<IncomeTaxBracketSetting_SubData>{
      income_Start: 0,
      income_End: 0,
      rate: 0,
      deduction: 0,
      update_By: this.user.id,
      update_Time_Str: this.functionUtility.getDateTimeFormat(new Date()),
      is_Duplicate: false,
    });
  }
  //#endregion

  //#region On Change
  onChangeEffectiveMonth() {
    this.onDataChange();
    this.data.effective_Month_Str = this.functionUtility.isValidDate(new Date(this.data.effective_Month)) ? new Date(this.data.effective_Month).toStringYearMonth() : ''
  }

  onChangeTaxCode() {
    this.onDataChange();
    this.data.type = this.data.tax_Code
      ? this.listTaxCode.find(x => x.key == this.data.tax_Code).value?.type
      : ''
  }

  onDataChange() {
    this.data.subData = []
    this.checkDuplicate = false
  }
  //#endregion
  onValueChange(item: IncomeTaxBracketSetting_SubData) {
    item.update_By = this.user.id
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date())
  }
  //#region Load & get list
  loadDropdownList() {
    this.getListNationality();
    this.getListTaxCode();
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: res => {
        this.listNationality = res;
      }
    });
  }

  getListTaxCode() {
    this.service.getListTaxCode().subscribe({
      next: res => {
        this.listTaxCode = res;
      }
    });
  }
  checkDuplicate: boolean
  onLevelChange(item: IncomeTaxBracketSetting_SubData) {
    this.isDuplicate()
    this.onValueChange(item)
  }
  async isDuplicate() {
    this.checkDuplicate = false
    if (this.data.subData.length > 0) {
      this.data.subData.map(x => x.is_Duplicate = false)
      //Count number of occurrences of each value in data list
      const lookup = this.data.subData.reduce((a, e) => {
        a[e.tax_Level] = ++a[e.tax_Level] || 0;
        return a;
      }, {});
      //Get all occurrences of each value that has more than 2 occurrences in the data list
      let deplicateValues = this.data.subData.filter(e => lookup[e.tax_Level])
      if (deplicateValues.length > 1) {
        this.checkDuplicate = true
        deplicateValues.map(x => x.is_Duplicate = true)
      }
      //Check for duplicate items against previously imported data in database
      await Promise.all(this.data.subData.map(async (item) => {
        if (item.tax_Level && await this.isDuplicatedData(item)) this.checkDuplicate = item.is_Duplicate = true;
      }))
    }
  }
  isDuplicatedData(item: IncomeTaxBracketSetting_SubData): Promise<boolean> {
    return new Promise((resolve) => {
      this.service.isDuplicatedData(this.data.nation, this.data.tax_Code, item.tax_Level, this.data.effective_Month_Str)
        .subscribe({
          next: (res) => resolve(res.isSuccess)
        });
    })
  }
  //#endregion
}
