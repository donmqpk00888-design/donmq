import { AfterViewChecked, Component, OnInit, ViewChild, effect } from '@angular/core';
import { FormGroup, NgForm } from '@angular/forms';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  ExitEmployeeMasterFileHistoricalDataMainMemory,
  ExitEmployeeMasterFileHistoricalDataParam,
  ExitEmployeeMasterFileHistoricalDataSource,
  ExitEmployeeMasterFileHistoricalDataView,
} from '@models/employee-maintenance/4_1_19_exit-employee-master-file-historical-data';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_19_ExitEmployeeMasterFileHistoricalDataService } from '@services/employee-maintenance/s_4_1_19_exit-employee-master-historical-data.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, AfterViewChecked {
  @ViewChild('mainForm') public mainForm: NgForm;
  param: ExitEmployeeMasterFileHistoricalDataParam = <ExitEmployeeMasterFileHistoricalDataParam>{};
  data: ExitEmployeeMasterFileHistoricalDataView[];
  pagination: Pagination = <Pagination>{};
  title: string;
  iconButton = IconButton;
  classButton = ClassButton;
  listNationality: KeyValuePair[] = [];
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  allowGetData: boolean = false
  constructor(
    private _service: S_4_1_19_ExitEmployeeMasterFileHistoricalDataService
  ) {
    super();
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadData()
      });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  //Retry to get data by validation required params
  ngAfterViewChecked() {
    if (this.allowGetData && this.mainForm) {
      const form : FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getPagination();
        this.allowGetData = false
      }
    }
  }

  ngOnDestroy(): void {
    this._service.setParamSearch(<ExitEmployeeMasterFileHistoricalDataMainMemory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
    });
  }

  getDataFromSource() {
    effect(() => {
      this.param = this._service.paramSearch().param;
      this.pagination = this._service.paramSearch().pagination;
      this.data = this._service.paramSearch().data;
      this.loadData()
    });
  }
  private loadData() {
    if (this.data?.length > 0) {
      if (!this.functionUtility.checkFunction('Search')) {
        this.clear();
        this.allowGetData = false
      }
      else
        this.allowGetData = true
    }
    this.loadDropDownList();
  }

  loadDropDownList() {
    this.getListDivision();
    this.getListNationality();
    this.getListFactory();
  }

  query(item: ExitEmployeeMasterFileHistoricalDataView) {
    const param = <ExitEmployeeMasterFileHistoricalDataSource>{
      useR_GUID: item.useR_GUID,
      resignDate: item.resignDate,
    };
    this._service.setParamForm(param);
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  //#region On Change
  onChangeDivision() {
    this.deleteProperty('factory')
    this.getListFactory();
  }

  //#endregion
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.param[name] ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }

  getPagination(isSearch: boolean = false) {
    this.spinnerService.show();
    this._service.getPagination(this.pagination, this.param).subscribe({
      next: (res) => {
        this.data = res.result;
        this.pagination = res.pagination;
        this.spinnerService.hide();
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  search() {
    this.pagination.pageNumber === 1 ? this.getPagination(true) : (this.pagination.pageNumber = 1);
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPagination();
  }

  clear() {
    this.param = <ExitEmployeeMasterFileHistoricalDataParam>{};
    this.data = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    };
  }

  //#region Get List
  getListNationality() {
    this._service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }

  getListDivision() {
    this._service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }

  getListFactory() {
    this._service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
