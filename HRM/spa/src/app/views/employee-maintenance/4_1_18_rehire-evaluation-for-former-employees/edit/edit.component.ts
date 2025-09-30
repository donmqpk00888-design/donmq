import { ChangeDetectorRef, Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  RehireEvaluationForFormerEmployeesDto,
  RehireEvaluationForFormerEmployeesEvaluation,
  RehireEvaluationForFormerEmployeesPersonal
} from '@models/employee-maintenance/4_1_18_rehire-evaluation-for-former-employees';
import { S_4_1_18_RehireEvaluationForFormerEmployeesService } from '@services/employee-maintenance/s_4_1_18_rehire-evaluation-for-former-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html',
  styleUrls: ['./edit.component.css'],
})
export class EditComponent extends InjectBase implements OnInit {
  param: RehireEvaluationForFormerEmployeesDto = <RehireEvaluationForFormerEmployeesDto>{
    personal: <RehireEvaluationForFormerEmployeesPersonal>{},
    evaluation: <RehireEvaluationForFormerEmployeesEvaluation>{}
  }
  tempEvaluation: RehireEvaluationForFormerEmployeesEvaluation = <RehireEvaluationForFormerEmployeesEvaluation>{}
  title: string = '';
  url: string = '';
  iconButton = IconButton;

  inputUpdateTime: string;
  listResignationType: KeyValuePair[] = [];
  listReasonforResignation: KeyValuePair[] = [];
  listFactoryPersonal: KeyValuePair[] = [];
  listFactoryEvaluation: KeyValuePair[] = [];
  listDepartmentPersonal: KeyValuePair[] = [];
  listDepartmentEvaluation: KeyValuePair[] = [];
  listDivision: KeyValuePair[] = [];
  formType: string = '';
  isResignationDate: boolean = false;
  action: string;

  constructor(
    private service: S_4_1_18_RehireEvaluationForFormerEmployeesService
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    this.getDataFromSource()
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      },
    });
  }
  getDataFromSource() {
    effect(() => {
      let source = this.service.basicCodeSource();
      this.formType = source.formType;
      this.action = this.formType == 'Detail' ? "System.Action.View" : "System.Action.Edit"
      this.param = { ...source.data };
      if (Object.keys(this.param.evaluation).length == 0 || Object.keys(this.param.personal).length == 0)
        this.back();
      else {
        this.inputUpdateTime = this.functionUtility.getDateTimeFormat(this.param.evaluation.update_Time.toDate());
        if (!this.functionUtility.checkEmpty(this.param.personal.onboard_Date))
          this.param.personal.onboard_Date_Date = this.functionUtility.getDateFormat(this.param.personal.onboard_Date.toDate());
        this.checkResignationDate()
        this.loadData()
      }
    })
  }
  private loadData() {
    this.getListData('listResignationType', this.service.getListResignationType.bind(this.service));
    this.getListData('listReasonforResignation', this.service.getListReasonforResignation.bind(this.service));
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
    this.getListDepartmentPersonal()
    this.getListDepartmentEvaluation()
    this.getListFactoryPersonal()
    this.getListFactoryEvaluation()
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
  onResultChange() {
    if (!this.param.evaluation.results) {
      this.tempEvaluation = JSON.parse(JSON.stringify(this.param.evaluation))
      this.deletePropertyEvaluation('employeeID')
      this.deletePropertyEvaluation('factory')
      this.deletePropertyEvaluation('department')
      this.deletePropertyEvaluation('division')
    }
    else {
      this.param.evaluation.employeeID = this.tempEvaluation.employeeID
      this.param.evaluation.factory = this.tempEvaluation.factory
      this.param.evaluation.department = this.tempEvaluation.department
      this.param.evaluation.division = this.tempEvaluation.division
    }
  }

  save() {
    this.spinnerService.show();
    this.service.update(this.param.evaluation).subscribe({
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
