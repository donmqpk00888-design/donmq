import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  RehireEvaluationForFormerEmployeesDto,
  RehireEvaluationForFormerEmployeesEvaluation,
  RehireEvaluationForFormerEmployeesPersonal
} from '@models/employee-maintenance/4_1_18_rehire-evaluation-for-former-employees';
import { S_4_1_18_RehireEvaluationForFormerEmployeesService } from '@services/employee-maintenance/s_4_1_18_rehire-evaluation-for-former-employees.service';
import { S_4_1_6_IdentificationCardHistoryService } from '@services/employee-maintenance/s_4_1_6_identification-card-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-add',
  templateUrl: './add.component.html',
  styleUrls: ['./add.component.css'],
})
export class AddComponent extends InjectBase implements OnInit {
  param: RehireEvaluationForFormerEmployeesDto = <RehireEvaluationForFormerEmployeesDto>{
    personal: <RehireEvaluationForFormerEmployeesPersonal>{},
    evaluation: <RehireEvaluationForFormerEmployeesEvaluation>{ results: true }
  }
  listResignationType: KeyValuePair[] = [];
  listReasonforResignation: KeyValuePair[] = [];
  listDivision: KeyValuePair[] = [];
  listFactoryPersonal: KeyValuePair[] = [];
  listFactoryEvaluation: KeyValuePair[] = [];
  listDepartmentPersonal: KeyValuePair[] = [];
  listDepartmentEvaluation: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  iconButton = IconButton;
  isResignationDate: boolean = false;
  dataTypeahead: string[] = [];
  constructor(
    private service: S_4_1_18_RehireEvaluationForFormerEmployeesService,
    private serviceIdentificationCardHistory: S_4_1_6_IdentificationCardHistoryService,
    private changeRef: ChangeDetectorRef
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListData('listResignationType', this.service.getListResignationType.bind(this.service));
      this.getListData('listReasonforResignation', this.service.getListReasonforResignation.bind(this.service));
      this.getListData('listDivision', this.service.getListDivision.bind(this.service));
      this.getListDepartmentPersonal()
      this.getListDepartmentEvaluation()
      this.getListFactoryPersonal()
      this.getListFactoryEvaluation()
    });
  }
  ngAfterViewChecked(): void { this.changeRef.detectChanges(); }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListTypeHeadIdentificationNumber();
    this.getListData('listResignationType', this.service.getListResignationType.bind(this.service));
    this.getListData('listReasonforResignation', this.service.getListReasonforResignation.bind(this.service));
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
    this.getListData('listNationality', this.serviceIdentificationCardHistory.getListNationality.bind(this.serviceIdentificationCardHistory));
  }

  checkIdentificationNumber() {
    setTimeout(() => {
      this.getDetail()
    }, 1000);
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      },
    });
  }
  getListFactoryEvaluation() {
    this.service.getListFactory(this.param.evaluation.division).subscribe({
      next: (res) => {
        this.listFactoryEvaluation = res;
      },
    });
  }

  getListFactoryPersonal() {
    this.service.getListFactory(this.param.personal.division).subscribe({
      next: (res) => {
        this.listFactoryPersonal = res;
      },
    });
  }
  onNationalityChange() {
    this.deletePropertyEvaluation('identification_Number')
    this.deletePropertyEvaluation('seq')
    this.getListTypeHeadIdentificationNumber()
    this.param.personal = <RehireEvaluationForFormerEmployeesPersonal>{}
  }
  getListTypeHeadIdentificationNumber() {
    this.service.getListTypeHeadIdentificationNumber(this.param.evaluation.nationality).subscribe({
      next: (res) => {
        this.dataTypeahead = res;
      },
    });
  }

  getListDepartmentEvaluation() {
    this.service.getListDepartment(this.param.evaluation.factory, this.param.evaluation.division).subscribe({
      next: (res) => {
        this.listDepartmentEvaluation = res;
      },
    });
  }

  getListDepartmentPersonal() {
    this.service.getListDepartment(this.param.personal.factory, this.param.personal.division).subscribe({
      next: (res) => {
        this.listDepartmentPersonal = res;
      },
    });
  }
  onDepartmentChange() {
    this.getListDepartmentEvaluation()
    this.deletePropertyEvaluation('department')
    this.listDepartmentEvaluation = []
  }
  onFactoryChange() {
    this.getListFactoryEvaluation();
    this.deletePropertyEvaluation('factory')
    this.deletePropertyEvaluation('department')
    this.listDepartmentEvaluation = []
  }
  getDetail() {
    if ((this.functionUtility.checkEmpty(this.param.evaluation.nationality)
      || this.functionUtility.checkEmpty(this.param.evaluation.identification_Number)))
      return this.snotifyService.warning(
        this.translateService.instant('EmployeeInformationModule.RehireEvaluationforFormerEmployees.PleaseSearch'),
        this.translateService.instant('System.Caption.Warning')
      );
    this.spinnerService.show();
    this.service.getDetail(this.param.evaluation.nationality, this.param.evaluation.identification_Number)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (this.functionUtility.checkEmpty(res.useR_GUID)) {
            this.functionUtility.snotifySuccessError(false, 'System.Message.NoData')
            this.param.personal = <RehireEvaluationForFormerEmployeesPersonal>{}
            this.param.evaluation.seq = 0
          }
          else {
            this.param.personal = { ...res }
            if (!this.functionUtility.checkEmpty(this.param.personal.onboard_Date))
              this.param.personal.onboard_Date_Date = this.functionUtility.getDateFormat(this.param.personal.onboard_Date.toDate());
            this.checkResignationDate()
            this.param.evaluation.seq = this.param.personal.seq;
            this.param.evaluation.useR_GUID = this.param.personal.useR_GUID;
            this.getListFactoryPersonal()
            this.getListDepartmentPersonal()
          }
        },
      })
  }
  checkResignationDate() {
    if (this.functionUtility.checkEmpty(this.param.personal.date_of_Resignation))
      this.isResignationDate = false
    else {
      const resignationDate = this.param.personal.date_of_Resignation.toDate()
      const today = new Date();
      const diffInDays = Math.floor((today.getTime() - resignationDate.getTime()) / (1000 * 3600 * 24));
      this.isResignationDate = diffInDays + 1 < 365 ? true : false
      this.param.personal.date_of_Resignation_Date = this.functionUtility.getDateFormat(resignationDate);
    }
  }
  save() {
    this.spinnerService.show();
    this.service.create(this.param.evaluation).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess) this.back();
      },
    })
  }

  back = () => this.router.navigate([this.url]);

  deletePropertyEvaluation(name: string) {
    delete this.param.evaluation[name]
  }
}
