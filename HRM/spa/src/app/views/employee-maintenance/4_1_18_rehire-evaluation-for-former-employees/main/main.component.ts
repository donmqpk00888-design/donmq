import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_18_RehireEvaluationForFormerEmployeesService } from '@services/employee-maintenance/s_4_1_18_rehire-evaluation-for-former-employees.service';
import { S_4_1_6_IdentificationCardHistoryService } from '@services/employee-maintenance/s_4_1_6_identification-card-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { RehireEvaluationForFormerEmployees, RehireEvaluationForFormerEmployeesDto, RehireEvaluationForFormerEmployeesParam, RehireEvaluationForFormerEmployeesSource } from '@models/employee-maintenance/4_1_18_rehire-evaluation-for-former-employees';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  data: RehireEvaluationForFormerEmployeesDto[] = []
  param: RehireEvaluationForFormerEmployeesParam = <RehireEvaluationForFormerEmployeesParam>{}
  pagination: Pagination = <Pagination>{};
  dataTypeahead: string[] = [];
  iconButton = IconButton;
  classButton = ClassButton;
  listNationality: KeyValuePair[] = [];
  constructor(
    private serviceIdentificationCardHistory: S_4_1_6_IdentificationCardHistoryService,
    private service: S_4_1_18_RehireEvaluationForFormerEmployeesService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      this.loadData()
    });
  }

  private loadData() {
    this.getListNationality()
    this.getListTypeHeadIdentificationNumber();
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getData()
      }
      else
        this.clear(false)
    }
  }
  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.nationality) &&
      !this.functionUtility.checkEmpty(this.param.identification_Number)
    return result;
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  validateSearch() {
    return this.functionUtility.checkEmpty(this.param.identification_Number) || this.functionUtility.checkEmpty(this.param.nationality)
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true,'System.Message.QuerySuccess')
      },
    });
  }
  getListTypeHeadIdentificationNumber() {
    this.service.getListTypeHeadIdentificationNumber(this.param.nationality).subscribe({
      next: (res) => {
        this.dataTypeahead = res;
      },
    });
  }

  getListNationality() {
    this.serviceIdentificationCardHistory.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }


  clear(isClear: boolean) {
    this.deleteProperty('nationality')
    this.deleteProperty('identification_Number')
    if (isClear) {
      this.pagination.pageNumber = 1;
      this.data = []
      this.pagination.totalCount = 0
    }
    else this.functionUtility.checkFunction('Search') ? this.getData() : this.data = [];
  }
  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;

  ngOnDestroy() {
    this.service.setParamSearch(<RehireEvaluationForFormerEmployees>{ param: this.param, pagination: this.pagination, data: this.data });
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: RehireEvaluationForFormerEmployeesDto) {
    let source = <RehireEvaluationForFormerEmployeesSource>{
      formType: 'Edit',
      data: { ...item }
    }
    this.service.setSource(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  detail(item: RehireEvaluationForFormerEmployeesDto) {
    let source = <RehireEvaluationForFormerEmployeesSource>{
      formType: 'Detail',
      data: { ...item },
    }
    this.service.setSource(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
