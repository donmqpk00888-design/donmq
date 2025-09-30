import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { HRMS_Emp_ResignationDto, HRMS_Emp_ResignationFormDto } from '@models/employee-maintenance/4_1_12_resignation-management';
import { S_4_1_12_ResignationManagementService } from '@services/employee-maintenance/s_4_1_12_resignation-management.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  formType: string = '';
  iconButton = IconButton;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listResignationType: KeyValuePair[] = [];
  listResignReason: KeyValuePair[] = [];
  employee_ID: string[] = []
  data: HRMS_Emp_ResignationDto = <HRMS_Emp_ResignationDto>{
    update_By: this.getCurrentUser(),
    update_Time: new Date().toStringDate()
  };
  dataForm: HRMS_Emp_ResignationFormDto = <HRMS_Emp_ResignationFormDto>{};
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };
  warningDisplayed: boolean = false;
  public inputChangeSubject: Subject<any> = new Subject<any>();

  constructor(
    private service: S_4_1_12_ResignationManagementService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListResignationType();
      this.getListResignReason();
      this.getVerifierTitle();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res =>
      this.formType = res.title
    );
  }

  getCurrentUser() {
    const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
    return user ? user.account : '';
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSearch();
      if (source && source != null) {
        if (this.formType == 'Add') {
          this.data = <HRMS_Emp_ResignationDto>{
            update_By: this.getCurrentUser(),
            update_Time: new Date().toStringDate()
          }
        } else {
          this.data = { ...source.source };
          if (this.functionUtility.checkEmpty(this.data.division))
            this.back()
          if (this.formType == 'Edit') {
            this.data.update_By = this.getCurrentUser();
            this.data.update_Time = new Date().toStringDate();
          }
        }
        this.getListDivision();
        this.getListFactory();
        this.getListResignationType();
        this.getListResignReason();
        this.getEmployeeID();
      } else
        this.back();
    })
  }

  getListDivision() {
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
  }

  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getListResignationType() {
    this.getListData('listResignationType', this.service.getListResignationType.bind(this.service));
  }

  getListResignReason() {
    this.service.getListResignReason(this.data.resignation_Type).subscribe({
      next: (res) => {
        this.listResignReason = res;
      }
    });
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    this.spinnerService.show();
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
        this.spinnerService.hide();
      }
    });
  }

  onChange() {
    this.resetData();
    if (this.data.factory && this.data.employee_ID) {
      this.service.getEmployeeData(this.data.factory, this.data.employee_ID).subscribe({
        next: (res) => {
          if (res && res.length > 0) {
            this.data.nationality = res[0].nationality;
            this.data.local_Full_Name = res[0].local_Full_Name;
            this.data.identification_Number = res[0].identification_Number;
            this.data.onboard_Date = res[0].onboard_Date;
          }
        }
      });
    }
  }

  getEmployeeID() {
    this.service.getEmployeeID().subscribe({
      next: (res) => {
        this.employee_ID = res
      }
    })
  }

  getVerifierName() {
    this.getDataVirifier('verifier_Name', this.service.getVerifierName.bind(this.service));
  }

  getVerifierTitle() {
    this.getDataVirifier('verifier_Title', this.service.getVerifierTitle.bind(this.service));
  }

  getDataVirifier(property: string, serviceMethod: (factory: string, verifier: string) => Observable<OperationResult>): void {
    serviceMethod(this.data.factory, this.data.verifier).subscribe({
      next: (res) => {
        if (res && res.isSuccess && res.data) {
          this.data[property] = res.data;
          this.warningDisplayed = false;
        } else {
          this.deleteProperty(property)
        }
      },
    });
  }

  onTypehead(e: TypeaheadMatch) {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, `System.Message.InvalidEmployeeIDLength`)
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    this.listFactory = [];
    this.resetData();
    if (!this.functionUtility.checkEmpty(this.data.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  onResignationTypeChange() {
    this.deleteProperty('resign_Reason')
    this.listResignReason = [];
    if (!this.functionUtility.checkEmpty(this.data.resignation_Type))
      this.getListResignReason();
    else
      this.listResignReason = [];
  }

  onVerifierChange() {
    if (this.data.factory && this.data.verifier) {
      this.getVerifierName();
      this.getVerifierTitle();
    }
  }

  onBlacklistChange() {
    if (this.data.blacklist) {
      this.snotifyService.warning(
        this.translateService.instant("EmployeeInformationModule.ResignationManagement.VerifierRequired"),
        this.translateService.instant('System.Caption.Warning')
      )
    } else
      this.resetVerifier();
  }

  onDateInputKeyDown(event: KeyboardEvent, name: string) {
    if (event.key === 'Backspace' || event.key === 'Delete') {
      this.data[name] = null
      event.preventDefault();
    }
  }

  resetData() {
    this.deleteProperty('nationality')
    this.deleteProperty('identification_Number')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('onboard_Date')
    this.data.blacklist = false;
    this.resetVerifier();
  }

  resetVerifier() {
    this.deleteProperty('verifier')
    this.deleteProperty('verifier_Name')
    this.deleteProperty('verifier_Title')
  }

  save() {
    const observable = this.formType == 'Edit' ? this.service.edit(this.data) : this.service.addNew(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        const message = this.formType == 'Edit' ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? message : result.error)
        if (result.isSuccess) this.back();
      },

    });
  }

  back = () => this.router.navigate([this.url]);

  checkEmpty() {
    const { data, functionUtility } = this;
    const commonChecks = [
      functionUtility.checkEmpty(data.division),
      functionUtility.checkEmpty(data.factory),
      functionUtility.checkEmpty(data.employee_ID),
      functionUtility.checkEmpty(data.nationality),
      functionUtility.checkEmpty(data.identification_Number),
      functionUtility.checkEmpty(data.local_Full_Name),
      functionUtility.checkEmpty(data.onboard_Date),
      functionUtility.checkEmpty(data.resign_Date),
      functionUtility.checkEmpty(data.resignation_Type),
      functionUtility.checkEmpty(data.resign_Reason)
    ];

    if (data.blacklist == true) {
      return commonChecks.some(check => check) ||
        functionUtility.checkEmpty(data.verifier) ||
        functionUtility.checkEmpty(data.verifier_Name);
    }

    return commonChecks.some(check => check);
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
