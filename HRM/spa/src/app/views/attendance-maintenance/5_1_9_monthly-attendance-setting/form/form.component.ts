import { formatDate } from '@angular/common';
import { MonthlyAttendanceSettingParam_Form, MonthlyAttendanceSettingParam_SubData } from '@models/attendance-maintenance/5_1_9_monthly-attendance-setting';
import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Use_Monthly_LeaveDto } from '@models/attendance-maintenance/5_1_9_monthly-attendance-setting';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_5_1_9_MonthlyAttendanceSettingService } from '@services/attendance-maintenance/s_5_1_9_monthly-attendance-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  @ViewChild('leaveTab', { static: true }) leaveTab: TemplateRef<any>;
  @ViewChild('allowanceTab', { static: true }) allowanceTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];

  title: string = '';
  url: string = '';
  selectedTab: string = ''

  tab = <const>{
    leave: 'leave',
    allowance: 'allowance'
  }

  iconButton = IconButton;
  factorys: KeyValuePair[] = [];
  leaveTypes: KeyValuePair[] = [];
  allowances: KeyValuePair[] = [];
  numberTab: string = '1';
  isDupplicateData: boolean;
  isEdit: boolean = false;

  param: MonthlyAttendanceSettingParam_Form = <MonthlyAttendanceSettingParam_Form>{};
  data: MonthlyAttendanceSettingParam_SubData = <MonthlyAttendanceSettingParam_SubData>{ leaveData: [], allowanceData: [] };

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };
  isShow: boolean = false;

  method: string = "";

  message: string = "";
  isValidate: boolean = false;

  effectiveMonthSelected: string | Date = null;
  constructor(
    private _service: S_5_1_9_MonthlyAttendanceSettingService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab();
      this.getFactorys();
      this.getLeaveTypes();
      this.getAllowances();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.method = role.title;
      })
    this._service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.method == 'Edit' || this.method === "Query") {
        if (res == null)
          this.back()
        else {
          this.param = res.param
          this.getRecentData(this.tab.leave);
          this.getRecentData(this.tab.allowance);
        }
      }
    })
    this.selectedTab = this.tab.leave;
    this.initTab();
    this.getFactorys();
    this.getLeaveTypes();
    this.getAllowances();
  }

  initTab() {
    this.tabs = [
      {
        id: 'leave',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceSetting.Leave'),
        isEnable: true,
        content: this.leaveTab
      },
      {
        id: 'allowance',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceSetting.Allowance'),
        isEnable: true,
        content: this.allowanceTab
      },
    ]
  }

  isValidParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory) && !this.functionUtility.checkEmpty(this.param.effective_Month) &&
      this.param.effective_Month.toString() != 'Invalid Date' &&
      this.param.effective_Month.toString() != 'NaN/NaN'
  }
  getCloneData = (tabID?: string) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      const selectedTab = tabID ? tabID : this.selectedTab
      this._service.getCloneData(this.param.factory, selectedTab == this.tab.leave ? '1' : '2', this.param.effective_Month_Str)
        .subscribe({
          next: res => {
            if (selectedTab == this.tab.leave) {
              this.data.leaveData = res
              this.data.leaveData.map(x => {
                x.effective_Month = this.param.effective_Month;
                x.effective_Month_Str = this.param.effective_Month_Str
              })
            }
            else {
              this.data.allowanceData = res;
              this.data.allowanceData.map(x => {
                x.effective_Month = this.param.effective_Month;
                x.effective_Month_Str = this.param.effective_Month_Str
              })
            }
            this.spinnerService.hide();
            resolve()
          },
          error: () => { reject() }
        })
    })
  };

  getRecentData(tabID?: string) {
    this.spinnerService.show();
    const selectedTab = tabID ? tabID : this.selectedTab
    this._service.getRecentData(this.param.factory, this.param.effective_Month_Str, selectedTab == this.tab.leave ? '1' : '2', this.method)
      .subscribe({
        next: res => {
          if (selectedTab == this.tab.leave) {
            this.data.leaveData = res
            this.data.leaveData.map(x => {
              x.effective_Month = this.param.effective_Month;
              x.effective_Month_Str = this.param.effective_Month_Str
            })
          }
          else {
            this.data.allowanceData = res;
            this.data.allowanceData.map(x => {
              x.effective_Month = this.param.effective_Month;
              x.effective_Month_Str = this.param.effective_Month_Str
            })
          }
          this.spinnerService.hide();
        }
      })
  }

  getAllowances() {
    this._service.getAllowance().subscribe({
      next: res => {
        this.allowances = res;
      }
    });
  }

  getLeaveTypes() {
    this._service.getLeaveTypes().subscribe({
      next: res => {
        this.leaveTypes = res;
      }
    });
  }

  getFactorys() {
    this._service.getFactorys().subscribe({
      next: res => {
        this.factorys = res
      }
    });
  }

  back = () => this.router.navigate([this.url]);

  saveChange() {
    this.snotifyService.confirm(
      `Leave : ${this.data.leaveData.length} ${this.data.leaveData.length > 1 ? 'items' : 'item'}, Alowance : ${this.data.allowanceData.length} ${this.data.allowanceData.length > 1 ? 'items' : 'item'}
      Total : ${this.data.leaveData.length + this.data.allowanceData.length} ${this.data.leaveData.length + this.data.allowanceData.length > 1 ? 'items' : 'item'}`,
      this.translateService.instant('System.Caption.Confirm'), () => {
        const dataAlls = this.data.allowanceData.concat(this.data.leaveData);
        if (!this.onValidationData(dataAlls)) {
          this.spinnerService.show()
          if (this.method === 'Add') {
            this._service.create(dataAlls).subscribe({
              next: res => {
                this.spinnerService.hide();
                if (res.isSuccess) {
                  this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.CreateOKMsg');
                  this.back();
                } else
                  this.functionUtility.snotifySuccessError(false, !res.isSuccess && res.error === null ? 'System.Message.CreateErrorMsg' : res.error);
              }
            })
          } else {
            this._service.edit(dataAlls).subscribe({
              next: res => {
                this.spinnerService.hide();
                if (res.isSuccess) {
                  this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.UpdateOKMsg');
                  this.back();
                } else
                  this.functionUtility.snotifySuccessError(false, !res.isSuccess && res.error === null ? 'System.Message.UpdateErrorMsg' : res.error);
              }
            })
          }
        }
      })
  }

  onDelete(item: HRMS_Att_Use_Monthly_LeaveDto, index: number) {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    currentDataList.splice(index, 1);
    this.isCheckDupplicate(item);
  }

  onAdd() {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    if (!this.onValidationData(currentDataList)) {
      currentDataList.push(<HRMS_Att_Use_Monthly_LeaveDto>{
        factory: this.param.factory,
        effective_Month: this.param.effective_Month,
        effective_Month_Str: this.functionUtility.getDateFormat(new Date(this.param.effective_Month)),
        leave_Type: this.selectedTab,
        month_Total: true,
        year_Total: true,
      });
    }
  }
  onValidationData(datas: HRMS_Att_Use_Monthly_LeaveDto[]): boolean {
    let result: boolean = false;
    if (this.functionUtility.checkEmpty(this.param.factory) || this.functionUtility.checkEmpty(this.param.effective_Month)) {
      this.snotifyService.warning("Factory/Effective Month is required!", this.translateService.instant('System.Caption.Warning'));
      return !result;
    }
    if (this.isValidate) {
      this.snotifyService.warning("Effective Month format error.", this.translateService.instant('System.Caption.Warning'));
      return !result;
    }
    if (this.isDupplicateData) {
      this.snotifyService.warning(this.message, this.translateService.instant('System.Caption.Warning'));
      return !result;
    }
    if (datas?.length > 0 && datas.some(x => this.functionUtility.checkEmpty(x.seq))) {
      let item = datas.find(x => this.functionUtility.checkEmpty(x.seq));
      this.snotifyService.warning(
        `${item.leave_Type === "leave" ? 'Tab Leave' : 'Tab Allowance'} - Seq is not null !`,
        this.translateService.instant('System.Caption.Warning'));
      return !result;
    }
    if (datas?.length > 0 && datas.some(x => this.functionUtility.checkEmpty(x.code))) {
      let item = datas.find(x => this.functionUtility.checkEmpty(x.code));
      this.snotifyService.warning(
        `${item.leave_Type === "leave" ? 'Tab Leave - Leave is not null !' : 'Tab Allowance - Allowance is not null !'}`,
        this.translateService.instant('System.Caption.Warning'));
      return !result;
    }
    return result;
  }

  async onChangeTab(event: string) {
    console.log(event)
    let currentDataList = event == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    if (this.method == 'Add' && currentDataList.length == 0 && this.isValidParams()) {
      await this.getCloneData(event)
      currentDataList.map(x => {
        x.effective_Month = this.param.effective_Month;
        x.effective_Month_Str = this.param.effective_Month_Str
      })
    }
  }

  isDisableSave(): boolean {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    let isDisableSave = this.functionUtility.checkEmpty(this.param.factory)
      || this.functionUtility.checkEmpty(this.param.effective_Month)
      || (currentDataList?.length > 0 && currentDataList.some(x => this.functionUtility.checkEmpty(x.code)))
      || (currentDataList?.length > 0 && currentDataList.some(x => this.functionUtility.checkEmpty(x.seq)))
      || currentDataList?.length === 0;
    return isDisableSave;
  }
  isCheckDupplicate(item: HRMS_Att_Use_Monthly_LeaveDto): boolean {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    let codes: string[] = Array.from(new Set(currentDataList?.filter(x => x.effective_Month_Str == item.effective_Month_Str && !this.functionUtility.checkEmpty(x.code)).map(x => x.code)));
    const result = codes?.length < currentDataList?.filter(x => x.effective_Month == item.effective_Month && !this.functionUtility.checkEmpty(x.code))?.length;
    return this.isDupplicateData = result;
  }

  onChangeCode(item: HRMS_Att_Use_Monthly_LeaveDto) {
    if (!this.functionUtility.checkEmpty(this.param.factory) && !this.functionUtility.checkEmpty(this.param.effective_Month)) {
      if (this.isCheckDupplicate(item)) {
        this.message = `Factory: ${item.factory}\r\nEffective Month: ${formatDate(item.effective_Month, 'yyyy/MM', 'en_US')}\r\nType: ${this.selectedTab == this.tab.leave ? "Leave" : "Allowance"}\r\nCode: ${item.code} can't input dupplicate`;
        return this.snotifyService.warning(this.message, this.translateService.instant('System.Caption.Warning'));
      } else {
        this.message = "";
      }
    }
  }

  async onChangeFactory() {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData = []
    if (this.method == 'Add') {
      if (this.isValidParams()) {
        this._service.checkDuplicateEffectiveMonth(this.param.factory, this.param.effective_Month_Str).subscribe({
          next: async res => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              this.deleteProperty('effective_Month')
              this.deleteProperty('effective_Month_Str')
              this.data = <MonthlyAttendanceSettingParam_SubData>{ leaveData: [], allowanceData: [] }
              this.functionUtility.snotifySuccessError(false, 'Selected Factory + Effective Month already exists. Please use the edit function instead');
            } else {
              await this.getCloneData()
              currentDataList.map(x => {
                x.effective_Month = this.param.effective_Month;
                x.effective_Month_Str = this.param.effective_Month_Str
              })
            }
          }
        })
      }
    }
  }

  async onChangeEffectiveMonth() {
    this.spinnerService.show();
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData = []
    this.param.effective_Month_Str = this.param.effective_Month ? this.functionUtility.getDateFormat(new Date(this.param.effective_Month)) : '';
    this._service.checkDuplicateEffectiveMonth(this.param.factory, this.param.effective_Month_Str).subscribe({
      next: async res => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.deleteProperty('effective_Month')
          this.deleteProperty('effective_Month_Str')
          this.data = <MonthlyAttendanceSettingParam_SubData>{ leaveData: [], allowanceData: [] }
          this.functionUtility.snotifySuccessError(false, 'Selected Factory + Effective Month already exists. Please use the edit function instead');
        } else {
          if (this.isValidParams()) {
            await this.getCloneData()
            currentDataList.map(x => {
              x.effective_Month = this.param.effective_Month;
              x.effective_Month_Str = this.param.effective_Month_Str
            })
          }
        }
      }
    })

  }

  isDisableAdd() {
    let currentDataList = this.selectedTab == this.tab.leave ? this.data.leaveData : this.data.allowanceData
    let isDisableAdd = this.functionUtility.checkEmpty(this.param.factory)
      || this.functionUtility.checkEmpty(this.param.effective_Month)
      || (currentDataList?.length > 0 && currentDataList.some(x => this.functionUtility.checkEmpty(x.code)))
      || (currentDataList?.length > 0 && currentDataList.some(x => this.functionUtility.checkEmpty(x.seq)));
    return isDisableAdd;
  }

  onChangeSeq(item: HRMS_Att_Use_Monthly_LeaveDto) {
    if (item.seq > 2147483647) {
      item.seq = null;
      this.functionUtility.snotifySuccessError(false, "Seq error!");
    }
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  clearFactory() {
    this.param = <MonthlyAttendanceSettingParam_Form>{}
    this.data = <MonthlyAttendanceSettingParam_SubData>{ leaveData: [], allowanceData: [] }
  }
}
