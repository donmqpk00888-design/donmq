import { Component, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { ContractTypeSetupDto, ContractTypeSetupParam, ContractTypeSetupSource, HRMSEmpContractTypeDetail } from '@models/employee-maintenance/4_1_14_contract-type-setup';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_14_ContractTypeSetupService } from '@services/employee-maintenance/s_4_1_14_contract-type-setup.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  source = <ContractTypeSetupSource><null>{};
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listScheduleFrequency: KeyValuePair[] = [];
  listAlertRule: KeyValuePair[] = [];
  dataDetail: HRMSEmpContractTypeDetail[] = [];

  formType: string = ''
  iconButton = IconButton;
  classButton = ClassButton;
  userInfo = JSON.parse(localStorage.getItem("HRM_User"));
  title: string = '';
  url: string = '';
  isSave: boolean = false; // cho phép hiển thị nút [SAVE]

  param: ContractTypeSetupParam = <ContractTypeSetupParam>{};
  data: ContractTypeSetupDto = <ContractTypeSetupDto>{
    probationary_Year: 0,
    probationary_Month: 0,
    probationary_Day: 0,
    alert: true,
    probationary_Period: true
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  alert: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];

  probationary_Period: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];
  constructor(
    private service: S_4_1_14_ContractTypeSetupService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListAlertRule();
      this.getListScheduleFrequency();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListDivision();
    this.getListFactory();
    this.getListAlertRule();
    this.getListScheduleFrequency();
    this.dataDetail.forEach(item => {
      item.update_By = this.getCurrentUser();
      item.update_Time = new Date();
    });
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => this.formType = res.title);
  }

  getCurrentUser() {
    const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
    return user ? user.account : '';
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.contractTypeSetupSource();
      if (source && source != null) {
        this.param = { ...source.source };
        if (this.formType == 'Edit') {
          if (this.functionUtility.checkEmpty(this.param.division))
            this.back()
          else {
            this.data = { ...source.source };
            this.data.contract_Type_After = this.data.contract_Type;
            this.service.getDataDetail(this.param).subscribe({
              next: (res) => {
                this.dataDetail = res
              }
            })
          }
        }
        else this.addItem()
      }
      else this.back();
    })
  }
  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      }
    });
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    if (!this.functionUtility.checkEmpty(this.data.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  onFieldValueChange(selectedValue: string, itemArray: any, field: string, i: number) {
    field === 'schedule_Frequency'
      ? selectedValue === 'D'
        ? itemArray.alert_Rules = '01'
        : itemArray.alert_Rules = '02'
      : selectedValue === '01'
        ? itemArray.schedule_Frequency = 'D'
        : itemArray.schedule_Frequency = 'M'

    //check Day or Month
    this.dataDetail[i].month_Range = 0
    this.dataDetail[i].contract_Start = 0
    this.dataDetail[i].contract_End = 0
    this.dataDetail[i].days_Before_Expiry_Date = 0
    this.setUpdateByAndTime(i);
  }

  getListAlertRule() {
    this.service.getListAlertRule().subscribe({
      next: (res) => {
        this.listAlertRule = res;
      },
    })
  }

  getListScheduleFrequency() {
    this.service.getListScheduleFrequency().subscribe({
      next: (res) => {
        this.listScheduleFrequency = res;
      },
    })
  }

  onChangeDetail() {
    if (this.data.alert && this.formType == 'Add') {
      this.dataDetail = [];
    }
  }

  setUpdateByAndTime(i: number) {
    if (this.formType == 'Edit') {
      this.dataDetail[i].update_Time = new Date();
      this.dataDetail[i].update_By = this.userInfo.id;
    }
  }

  addItem() {
    let isValid = this.onValidate();
    if (isValid) {
      this.dataDetail.push(<HRMSEmpContractTypeDetail>{
        seq: 0,
        month_Range: 0,
        contract_Start: 0,
        contract_End: 0,
        day_Of_Month: 0,
        days_Before_Expiry_Date: 0,
        update_By: this.getCurrentUser(),
        update_Time: new Date()
      });
    }
    for (let i = 0; i < this.dataDetail.length; i++) {
      this.dataDetail[i].seq = i + 1;
    }
  }

  deleteItem(index: number) {
    this.dataDetail = this.dataDetail.filter(item => item.seq !== this.dataDetail[index].seq);
    for (let i = 0; i < this.dataDetail.length; i++) {
      this.dataDetail[i].seq = i + 1;
    }
  }

  validateInput(event: any) {
    let keyCode = event.keyCode || event.which;
    let currentValue = event.target.value + String.fromCharCode(keyCode);
    if (parseInt(currentValue, 10) > 31) {
      event.preventDefault();
    }
  }

  checkValidate() {
    if ((this.functionUtility.checkEmpty(this.data.division) ||
      this.functionUtility.checkEmpty(this.data.factory) ||
      this.functionUtility.checkEmpty(this.data.contract_Type) ||
      this.functionUtility.checkEmpty(this.data.contract_Title) ||
      this.data.probationary_Year < 0 ||
      this.data.probationary_Year == null ||
      this.data.probationary_Month < 0 ||
      this.data.probationary_Month == null ||
      this.data.probationary_Day < 0 ||
      this.data.probationary_Day == null ||
      this.data.probationary_Period == undefined ||
      this.data.alert == undefined)) {
      return true;
    }
    else return false;
  }

  onValidate() {
    let isValid = true;
    this.dataDetail.forEach(x => {
      if (this.functionUtility.checkEmpty(x.schedule_Frequency)) {
        this.snotifyService.warning("Please choose Schedule Frequency", this.translateService.instant('System.Caption.Warning'))
        isValid = false;
      }
      else if (this.functionUtility.checkEmpty(x.alert_Rules)) {
        this.snotifyService.warning("Please choose Alert Rules", this.translateService.instant('System.Caption.Warning'))
        isValid = false;
      } else if (x.month_Range == null) {
        this.snotifyService.warning("Please input Month Range", this.translateService.instant('System.Caption.Warning'))
        isValid = false;
      }
    })
    return isValid;
  }

  save() {
    let isValid = this.onValidate();
    if (isValid) {
      this.dataDetail.forEach(item => {
        item.division = this.data.division;
        item.factory = this.data.factory;
        item.contract_Type = this.data.contract_Type;
      });
      if (this.formType == 'Add') {
        this.spinnerService.show();
        let dataAll: ContractTypeSetupDto = {
          ...this.data,
          dataDetail: this.dataDetail
        };
        this.service.add(dataAll).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.CreateOKMsg' : res.error)
            if (res.isSuccess) this.back();
          }
        });
      } else {
        let dataAll: ContractTypeSetupDto = {
          ...this.data,
          dataDetail: this.dataDetail
        };
        this.spinnerService.show();
        this.service.edit(dataAll).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : 'System.Message.UpdateErrorMsg')
            if (res.isSuccess) this.back();
          }
        });
      }
    }
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
  deleteSubProperty(name: string, index: number) {
    delete this.dataDetail[index][name]
  }
}
