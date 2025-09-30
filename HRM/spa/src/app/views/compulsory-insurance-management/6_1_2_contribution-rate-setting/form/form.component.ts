import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { ContributionRateSettingCheckEffectiveMonth, ContributionRateSettingForm, ContributionRateSettingParam, ContributionRateSettingSource, ContributionRateSettingSubData, ContributionRateSettingSubParam } from '@models/compulsory-insurance-management/6_1_2_contribution-rate-setting';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_6_1_2_ContributionRateSettingService } from '@services/compulsory-insurance-management/s_6_1_2_contribution-rate-setting.service';
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
  param: ContributionRateSettingSubParam = <ContributionRateSettingSubParam>{};
  data: ContributionRateSettingSubData[] = []
  dataForm: ContributionRateSettingForm = <ContributionRateSettingForm>{};
  dataCheckEffectiveMonth: ContributionRateSettingCheckEffectiveMonth = <ContributionRateSettingCheckEffectiveMonth>{};
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  title: string = '';
  url: string = '';
  formType: string = '';
  isEdit: boolean = false;
  isDuplicated: boolean = false;
  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = []
  listPermissionGroup: KeyValuePair[] = []
  listInsuranceType: KeyValuePair[] = []

  constructor(
    private service: S_6_1_2_ContributionRateSettingService
  ) {
    super();
    this.getDataFromSource()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList();
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
    });
    this.loadDropdownList();
  }
  getDataFromSource() {
    effect(() => {
      let source = this.service.programSource();
      if (this.formType == 'Edit') {
        if (Object.keys(source.source).length == 0)
          this.back()
        else {
          this.isEdit = true
          this.param.factory = source.paramQuery.factory
          this.param.effective_Month = new Date(source.paramQuery.effective_Month)
          this.param.effective_Month_Str = this.functionUtility.getDateFormat(this.param.effective_Month)
          this.param.permission_Group = source.source.permission_Group
          this.getDetail(this.param)
        }
      }
      else {
        this.getListFactory();
        this.getListInsuranceType();
      }

    })
  }

  loadDropdownList() {
    this.getListFactory();
    this.getListInsuranceType();
    this.getListPermissionGroup();
  }

  getDetail(param: ContributionRateSettingSubParam) {
    this.spinnerService.show();
    this.service.getDetail(param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (!this.functionUtility.isEmptyObject(res)) {
          this.data = res;
          this.param = param;
          this.param.effective_Month = new Date(this.param.effective_Month);
          this.param.effective_Month_Str = this.functionUtility.getDateFormat(this.param.effective_Month)
          this.loadDropdownList()
        } else {
          this.back();
        }
      }
    })
  }
  add() {
    const current = new Date();
    this.data.push(<ContributionRateSettingSubData>{
      insurance_Type: null,
      employer_Rate: 0,
      employee_Rate: 0,
      update_By: this.user.id,
      update_Time: current,
      update_Time_Str: this.functionUtility.getDateTimeFormat(current),
      isNew: true
    })
  }
  remove(index: number) {
    this.data.splice(index, 1)
    this.checkDuplicate()
  }
  checkEmpty() {
    return (this.functionUtility.checkEmpty(this.param.factory)
      || this.functionUtility.checkEmpty(this.param.effective_Month)
      || this.param.effective_Month.toString() == "Invalid Date"
      || this.functionUtility.checkEmpty(this.param.permission_Group))
  }
  checkDataEmpty() {
    return this.data.every(item =>
      item.insurance_Type && item.employer_Rate !== undefined && item.employer_Rate !== null && item.employer_Rate.toString() !== ''
      && item.employee_Rate !== undefined && item.employee_Rate !== null && item.employee_Rate.toString() !== ''
    );
  }
  save() {
    this.dataForm.param = this.param
    this.dataForm.subData = this.data
    this.spinnerService.show();
    if (!this.isEdit)
      this.service.create(this.dataForm).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          if (res.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else
            this.snotifyService.error(res.error ?? 'System.Message.CreateErrorMsg', this.translateService.instant('System.Caption.Error'));
        }
      });
    else
      this.service.update(this.dataForm).subscribe({
        next: res => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.back();
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success')
            );
          } else
            this.snotifyService.error(this.translateService.instant(res.error ?? 'System.Message.UpdateErrorMsg'), this.translateService.instant('System.Caption.Error'));
        }
      });
  }

  onChange(index: number) {
    this.checkDuplicate()
    this.checkEffectiveMonth(index)
    this.onDataChange(this.data[index])
  }

  checkEffectiveMonth(index: number) {
    this.param.insurance_Type = this.data[index].insurance_Type
    this.param.effective_Month = this.param.effective_Month.toString().toDate()
    if (this.param.effective_Month != null)
      this.service.checkEffectiveMonth(this.param).subscribe({
        next: (res) => {
          this.dataCheckEffectiveMonth = res;
          if (this.dataCheckEffectiveMonth.checkEffective_Month) {
            if (this.formType != "Edit") {
              this.isDuplicated = this.data[index].isDuplicate = true
              this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput');
            }
          }
          else {
            this.data[index].employer_Rate = this.dataCheckEffectiveMonth.dataDefault.employer_Rate
            this.data[index].employee_Rate = this.dataCheckEffectiveMonth.dataDefault.employee_Rate
          }
        }
      });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListInsuranceType() {
    this.service.getListInsuranceType().subscribe({
      next: (res) => {
        this.listInsuranceType = res;
      },
    });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: (res) => {
        this.listPermissionGroup = res;
      },
    });
  }
  onChangeFactory() {
    this.param.permission_Group = null
    this.getListPermissionGroup();
    this.data = []
    this.isDuplicated = false
  }

  onChangeEffectiveMonth() {
    this.param.effective_Month_Str = this.functionUtility.getDateFormat(new Date(this.param.effective_Month))
    this.data = []
    this.isDuplicated = false
  }

  onChangePermissionGroup() {
    this.data = []
    this.isDuplicated = false
    console.log(this.checkEmpty())
  }

  onDataChange(item: ContributionRateSettingSubData) {
    item.update_By = this.user.id;
    item.update_Time = new Date();
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(item.update_Time);
  }

  checkDuplicate() {
    this.isDuplicated = false;
    if (this.data.length > 0) {
      this.data.map(x => x.isDuplicate = false);

      const lookup = this.data.reduce((a, e) => {
        const key = `${e.insurance_Type}`;
        a[key] = ++a[key] || 0;
        return a;
      }, {});

      const duplicateValues = this.data.filter(e => {
        const key = `${e.insurance_Type}`;
        return lookup[key];
      });

      if (duplicateValues.length > 1) {
        this.isDuplicated = true;

        duplicateValues.map(x => x.isDuplicate = true);

        this.snotifyService.clear();
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput');
      }
    }
  }


  validateYearMonth(event: KeyboardEvent): void {
    const inputField = event.target as HTMLInputElement;
    let input = inputField.value;
    const key = event.key;

    const allowedKeys = ['Backspace', 'Tab', 'ArrowLeft', 'ArrowRight'];
    if (allowedKeys.includes(key))
      return;

    if (!/^\d$/.test(key) && key !== '/') {
      event.preventDefault();
      return;
    }

    if (input.length >= 7)
      event.preventDefault();

    if (key === '/')
      if (input.length !== 4 || input.includes('/'))
        event.preventDefault();

    if (input.includes('/') && input.length > 4 && input.split('/')[1].length >= 2)
      event.preventDefault();

    if (input === '000' && key === '0') {
      this.resetToCurrentDate(inputField);
      event.preventDefault();
    }

    if (input.includes('/') && input.length === 6) {
      const monthPart = input.split('/')[1] + key;
      const month = parseInt(monthPart, 10);

      if (month < 1 || month > 12 || monthPart === '00') {
        this.resetToCurrentDate(inputField);
        event.preventDefault();
      }
    }
  }

  resetToCurrentDate(inputField: HTMLInputElement): void {
    const currentDate = new Date();
    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    inputField.value = `${year}/${month}`;
  }

  validateDecimal(event: KeyboardEvent): boolean {
    const inputChar = event.key;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];

    if (allowedKeys.includes(inputChar))
      return true;

    const input = event.target as HTMLInputElement;
    const currentValue = input.value;
    const selectionStart = input.selectionStart!;
    const selectionEnd = input.selectionEnd!;

    if (!/^\d$/.test(inputChar) && inputChar !== '.') {
      event.preventDefault();
      return false;
    }

    const newValue = currentValue.substring(0, selectionStart) + inputChar + currentValue.substring(selectionEnd);

    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    // Điều kiện xử lý xóa số 0 khi trỏ vào
    if (integerPartLength > 1 && parts[0][0] === '0' && selectionStart === 1) {
      const newValueWithoutLeadingZero = parts[0].substring(1) + (parts.length > 1 ? '.' + parts[1] : '');
      input.value = newValueWithoutLeadingZero;
      return false;
    }

    // Kiểm tra độ dài cho phần nguyên và phần thập phân
    if (integerPartLength > 3 ||
      (integerPartLength === 3 && parseInt(parts[0]) > 999) ||
      decimalPartLength > 3) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^(0|[1-9][0-9]{0,2})(\.[0-9]{0,3})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }
    return true;
  }

  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.data[name]
}
