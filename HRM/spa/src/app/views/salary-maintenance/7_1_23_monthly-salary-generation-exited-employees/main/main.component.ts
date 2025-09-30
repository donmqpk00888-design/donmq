import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { MonthlySalaryGenerationExitedEmployees_Param, MonthlySalaryGenerationExitedEmployees_Memory } from '@models/salary-maintenance/7_1_23_monthly-salary-generation-exited-employees';
import { S_7_1_23_MonthlySalaryGenerationExitedEmployees } from '@services/salary-maintenance/s_7_1_23_monthly-salary-generation-exited-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('salaryGenerationTab', { static: true }) salaryGenerationTab: TemplateRef<any>;
  @ViewChild('dataLockTab', { static: true }) dataLockTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };

  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder

  selectedTab: string = ''

  salaryGeneration_Param: MonthlySalaryGenerationExitedEmployees_Param = <MonthlySalaryGenerationExitedEmployees_Param>{}
  dataLock_Param: MonthlySalaryGenerationExitedEmployees_Param = <MonthlySalaryGenerationExitedEmployees_Param>{}

  factoryList: KeyValuePair[] = [];
  permissionGroupList: KeyValuePair[] = [];
  salaryLockList: KeyValuePair[] = [
    { key: "Y", value: "Yes", optional: false },
    { key: "N", value: "No", optional: false }
  ];
  i18n: string = 'SalaryMaintenance.MonthlySalaryGenerationExitedEmployees.'

  constructor(private service: S_7_1_23_MonthlySalaryGenerationExitedEmployees) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab()
      this.getDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.salaryGeneration_Param = this.service.paramSearch().salaryGeneration_Param;
    this.dataLock_Param = this.service.paramSearch().dataLock_Param;
    this.selectedTab = this.service.paramSearch().selected_Tab
    this.initTab()
    this.getDropDownList()
  }
  initTab() {
    this.tabs = [
      {
        id: 'salaryGeneration',
        title: this.translateService.instant(this.i18n + 'MonthlySalaryGeneration'),
        isEnable: true,
        content: this.salaryGenerationTab
      },
      {
        id: 'dataLock',
        title: this.translateService.instant(this.i18n + 'MonthlyDataLock'),
        isEnable: true,
        content: this.dataLockTab
      },
    ]
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<MonthlySalaryGenerationExitedEmployees_Memory>{
      salaryGeneration_Param: this.salaryGeneration_Param,
      dataLock_Param: this.dataLock_Param,
      selected_Tab: this.selectedTab
    });
  }
  execute() {
    this.snotifyService.confirm(
      this.translateService.instant(this.i18n + 'ExecuteConfirm'),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show();
        this.selectedTab == 'salaryGeneration'
          ? this.service.checkCloseStatus(this[this.selectedTab + '_Param']).subscribe({
            next: (res) => {
              setTimeout(() => {
                this.spinnerService.hide();
                if (res.isSuccess) {
                  this.functionUtility.checkEmpty(res.error)
                    ? this.continueExecute()
                    : this.snotifyService.confirm(
                      this.translateService.instant(`${this.i18n}${res.error}`),
                      this.translateService.instant('System.Caption.Confirm'),
                      () => this.continueExecute()
                    );
                } else {
                  this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
                }
              }, 1000);
            }
          }) : this.continueExecute()
      }
    );
  }
  continueExecute() {
    this.spinnerService.show();
    this.service.execute(this[this.selectedTab + '_Param']).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          if (res.data.error)
            this.snotifyService.warning(res.data.error, this.translateService.instant('System.Caption.Warning'));
          this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.CreateOKMsg');
          this[this.selectedTab + '_Param'].total_Rows = this.selectedTab === 'salaryGeneration' ? res.data.count : res.data;
        } else {
          this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
        }
        this.spinnerService.hide();
      },
    });
  }
  clearParam() {
    const initParam = structuredClone(this.service.initData[this.selectedTab + '_Param'])
    this[this.selectedTab + '_Param'] = initParam
    if (this.selectedTab == 'dataLock')
      this.salaryLockList.map(x => x.optional = false)
  }
  getDropDownList() {
    this.service.getDropDownList(this[this.selectedTab + '_Param'])
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.permissionGroupList = structuredClone(keys.filter((x: { key: string; }) => x.key == "PE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.functionUtility.getNgSelectAllCheckbox(this.permissionGroupList)
  }

  changeTab() {
    this.getDropDownList()
  }

  onFactoryChange() {
    this.getDropDownList()
    this.deleteProperty('permission_Group')
  }
  onRadioChange(event: any) {
    this.salaryLockList.forEach(x => x.optional = x.key === event.srcElement.value && event.srcElement.checked);
    this.dataLock_Param.salary_Lock = this.salaryLockList.find((x) => x.optional)?.key ?? '';
  }
  onDateChange() {
    this[this.selectedTab + '_Param'].year_Month_Str = this.functionUtility.isValidDate(new Date(this[this.selectedTab + '_Param'].year_Month))
      ? this.functionUtility.getDateFormat(new Date(this[this.selectedTab + '_Param'].year_Month))
      : '';
  }
  deleteProperty(name: string) {
    delete this[this.selectedTab + '_Param'][name]
  }
  validateDecimal(event: any): boolean {
    const inputChar = event.key;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.indexOf(inputChar) !== -1)
      return true;

    const currentValue = event.target.value;
    if (currentValue === '' && inputChar === '.') {
      event.preventDefault();
      return false;
    }
    const newValue = currentValue.substring(0, event.target.selectionStart) + inputChar + currentValue.substring(event.target.selectionEnd);
    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (integerPartLength > 5 || decimalPartLength > 5) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^[0-9]*(\.[0-9]{0,5})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    return true;
  }

  onDecimalPaste(event: ClipboardEvent): boolean {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';

    const decimalRegex = /^[0-9]{1,5}(\.[0-9]{1,5})?$/;
    if (decimalRegex.test(pastedText)) {
      const input = event.target as HTMLInputElement;
      input.value = pastedText;
      this.salaryGeneration_Param.salary_Days = parseFloat(pastedText);
    }
    return false;
  }
}
