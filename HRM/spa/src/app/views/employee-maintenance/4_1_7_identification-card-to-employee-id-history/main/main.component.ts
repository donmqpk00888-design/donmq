import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Emp_IDcard_EmpID_HistoryDto, IdentificationCardToEmployeeIDHistoryParam, IdentificationCardToEmployeeIDHistorySource } from '@models/employee-maintenance/4_1_7_identification-card-to-employee-id-history';
import { S_4_1_7_IdentificationCardToEmployeeIdHistoryService } from '@services/employee-maintenance/s_4_1_7_identification-card-to-employee-id-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  iconButton = IconButton;
  data: HRMS_Emp_IDcard_EmpID_HistoryDto[] = [];
  param: IdentificationCardToEmployeeIDHistoryParam = <IdentificationCardToEmployeeIDHistoryParam>{}

  constructor(private service: S_4_1_7_IdentificationCardToEmployeeIdHistoryService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListNationality();
      if (this.data.length > 0)
        this.getData()
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data
      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getListDivision();
        this.getListFactory();
      }
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear()
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListNationality();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<IdentificationCardToEmployeeIDHistorySource>{ param: this.param, pagination: this.pagination, data: this.data });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }


  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <IdentificationCardToEmployeeIDHistoryParam>{};
    this.listFactory = [];
    this.data = [];
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      }
    });
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      }
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty = (name: string) => delete this.param[name];
}
