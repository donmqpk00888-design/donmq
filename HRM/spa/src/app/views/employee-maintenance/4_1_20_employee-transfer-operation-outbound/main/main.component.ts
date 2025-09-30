import { Component, OnInit, ViewChild, effect } from '@angular/core';
import { FormGroup, NgForm } from '@angular/forms';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  EmployeeTransferOperationOutboundMainMemory,
  EmployeeTransferOperationOutboundParam,
  EmployeeTransferOperationOutboundDto,
} from '@models/employee-maintenance/4_1_20_employee-transfer-operation-outbound';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_20_EmployeeTransferOperationOutboundService } from '@services/employee-maintenance/s_4_1_20_employee-transfer-operation-outbound.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('mainForm') public mainForm: NgForm;
  param: EmployeeTransferOperationOutboundParam = <EmployeeTransferOperationOutboundParam>{};
  data: EmployeeTransferOperationOutboundDto[] = [];
  pagination: Pagination = <Pagination>{};
  title: string;
  iconButton = IconButton;
  classButton = ClassButton;

  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listAssignedFactory: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  listReasonChange: KeyValuePair[] = [];
  allowGetData: boolean = false
  constructor(
    private _service: S_4_1_20_EmployeeTransferOperationOutboundService
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
      const form: FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getPagination();
        this.allowGetData = false
      }
    }
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
  ngOnDestroy(): void {
    this._service.setParamSearch(<EmployeeTransferOperationOutboundMainMemory>{
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

  loadDropDownList() {
    this.getListDivision();
    this.getListNationality();
    this.getListFactory();
    this.getListAssignedFactory();
    this.getListReasonChange();
  }
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
        if (isSearch) this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')

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
    this.param = <EmployeeTransferOperationOutboundParam>{};
    this.data = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    };
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: EmployeeTransferOperationOutboundDto) {
    this._service.setParamForm(item.history_GUID);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  //#region On Change
  onChangeDivision() {
    this.deleteProperty('factory')
    this.getListFactory();
  }

  onChangeAssignedDivision() {
    this.deleteProperty('assignedFactory')
    this.getListAssignedFactory();
  }

  //#endregion

  //#region Get List
  getListDivision() {
    this._service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }
  getListFactory() {
    this._service
      .getListFactoryByDivision(this.param.division)
      .subscribe({
        next: (res) => {
          this.listFactory = res;
        },
      });
  }

  getListAssignedFactory() {
    this._service
      .getListFactoryByDivision(this.param.assignedDivision)
      .subscribe({
        next: (res) => {
          this.listAssignedFactory = res;
        },
      });
  }
  getListNationality() {
    this._service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }

  getListReasonChange() {
    this._service.getListReasonChangeOut().subscribe({
      next: (res) => {
        this.listReasonChange = res;
      },
    });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
