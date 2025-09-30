import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeTransferHistory, EmployeeTransferHistoryDetail, EmployeeTransferHistoryDetele, EmployeeTransferHistoryEffectiveConfirm, EmployeeTransferHistoryParam, EmployeeTransferHistorySource } from '@models/employee-maintenance/4_1_17_employee-transfer-history';
import { S_4_1_17_EmployeeTransferHistoryService } from '@services/employee-maintenance/s_4_1_17_employee-transfer-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {


  //#region Variables
  iconButton = IconButton;
  classButton = ClassButton;

  title: string = '';
  programCode: string = '';
  isDisableBtnEffectiveConfirm: boolean = false;

  effective_Date_End_value: Date;
  effective_Date_Start_value: Date;
  selectAll: boolean = false;
  dataToday: Date = new Date();

  //#endregion

  //#region Objects
  param: EmployeeTransferHistoryParam = <EmployeeTransferHistoryParam>{}

  //#endregion

  //#region Arrays
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedFactoryAfter: KeyValuePair[] = [];
  listAssignedDepartmentAfter: KeyValuePair[] = [];
  listAssignedDivisionAfter: KeyValuePair[] = [];
  dataSelect: EmployeeTransferHistoryDetail[] = [];

  data: EmployeeTransferHistoryDetail[] = [];
  //#endregion

  //#region Pagination
  pagination: Pagination = <Pagination>{};

  //#endregion


  constructor(
    private service: S_4_1_17_EmployeeTransferHistoryService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.param.effective_Date_Start != null && this.param.effective_Date_Start != undefined)
        this.effective_Date_Start_value = this.param.effective_Date_Start.toDate()
      if (this.param.effective_Date_End != null && this.param.effective_Date_Start != undefined)
        this.effective_Date_End_value = this.param.effective_Date_End.toDate()
      this.loadData()
    });

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
  }
  private loadData() {
    this.getListDepartment()
    this.getListFactory()
    this.getListAssignedFactoryAfter()
    this.getListAssignedDepartmentAfter()
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
    this.getListData('listAssignedDivisionAfter', this.service.getListAssignedDivisionAfter.bind(this.service));
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search')) {
        if (this.validateSearch())
          this.getData()
      }
      else
        this.clear(true)
    }
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  checkDate() {
    if (this.effective_Date_Start_value != null)
      this.param.effective_Date_Start = this.functionUtility.getDateFormat(this.effective_Date_Start_value);
    else this.deleteProperty('effective_Date_Start')

    if (this.effective_Date_End_value != null)
      this.param.effective_Date_End = this.functionUtility.getDateFormat(this.effective_Date_End_value);
    else this.deleteProperty('effective_Date_End')
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      },
    });
  }

  //#region  get value param
  getListFactory() {
    this.service.getListFactory(this.param.division_After).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  getListAssignedFactoryAfter() {
    this.service.getListAssignedFactoryAfter(this.param.assigned_Division_After).subscribe({
      next: (res) => {
        this.listAssignedFactoryAfter = res;
      },
    });
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.factory_After, this.param.division_After).subscribe({
      next: (res) => {
        this.listDepartment = res;
      },
    });
  }
  getListAssignedDepartmentAfter() {
    this.service.getListDepartmentAfter(this.param.assigned_Factory_After, this.param.assigned_Division_After).subscribe({
      next: (res) => {
        this.listAssignedDepartmentAfter = res;
      },
    });
  }
  //#endregion

  //#region on change value input
  onFactoryChange() {
    this.getListFactory()
    this.deleteProperty('department_After')
    this.deleteProperty('factory_After')
    this.listFactory = []
    this.listDepartment = []
  }

  onDepartmentChange() {
    this.deleteProperty('department_After')
    this.getListDepartment();
    this.listDepartment = []

  }

  onAssignedSupportedFactoryChange() {
    this.deleteProperty('assigned_Factory_After')
    this.deleteProperty('assigned_Department_After')
    this.getListAssignedFactoryAfter()
    this.listAssignedFactoryAfter = []
    this.listAssignedDepartmentAfter = []
  }

  onAssignedSupportedDepartmentChange() {
    this.deleteProperty('assigned_Department_After')
    this.getListAssignedDepartmentAfter()
    this.listAssignedDepartmentAfter = []

  }
  //#endregion

  getData(isSearch?: boolean) {
    this.checkDate()
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.data.map((val) => {
          val.actingPosition_Start_Before = val.actingPosition_Start_Before != null
            ? new Date(val.actingPosition_Start_Before) : null
          val.actingPosition_End_Before = val.actingPosition_End_Before != null
            ? new Date(val.actingPosition_End_Before) : null
          val.actingPosition_Start_After = val.actingPosition_Start_After != null
            ? new Date(val.actingPosition_Start_After) : null
          val.actingPosition_End_After = val.actingPosition_End_After != null
            ? new Date(val.actingPosition_End_After) : null
          val.effective_Date = new Date(val.effective_Date)
          val.update_Time = new Date(val.update_Time)
        })
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        this.selectAll = false
      }
    });
  }

  download() {
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }

  pageChanged(event: any) {
    if (this.pagination.pageNumber !== event.page) {
      this.pagination.pageNumber = event.page;
      this.getData();
    }
  }
  clear(isClear: boolean) {
    this.param = <EmployeeTransferHistoryParam>{ effective_Status: 0 }
    this.effective_Date_End_value = null;
    this.effective_Date_Start_value = null;
    this.dataSelect = []
    this.selectAll = false
    if (isClear) {
      this.listAssignedDepartmentAfter = [];
      this.listDepartment = [];
      this.listAssignedFactoryAfter = [];
      this.listFactory = [];
      this.pagination.pageNumber = 1;
      this.data = [];
      this.pagination.totalCount = 0
    }
    else this.functionUtility.checkFunction('Search') ? this.getData() : this.data = [];
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  ngOnDestroy(): void {
    this.checkDate()
    this.service.setParamSearch(<EmployeeTransferHistory>{ param: this.param, pagination: this.pagination, data: this.data });
  }

  add(method: string) {
    this.service.method.set(method);
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: EmployeeTransferHistoryDetail) {
    let source = <EmployeeTransferHistorySource>{
      currentPage: this.pagination.pageNumber,
      param: { ...this.param },
      basicCode: { ...item }
    }
    console.log("SOURCE", source);

    this.service.basicCodeSource.set(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  validateSearch() {
    return !this.functionUtility.checkEmpty(this.param.division_After) && !this.functionUtility.checkEmpty(this.param.factory_After)
  }
  effectiveConfirm() {
    let items: EmployeeTransferHistoryEffectiveConfirm[] = this.dataSelect.map(item => ({
      useR_GUID: item.useR_GUID,
      effective_Status: item.effective_Status,
      effective_Date: this.functionUtility.getDateFormat(item.effective_Date),
      history_GUID: item.history_GUID
    }));
    this.service.effectiveConfirm(items).subscribe({
      next: result => {
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess) {
          this.getData();
          this.dataSelect = []
        }
        this.spinnerService.hide();
      },
    });
  }

  batchDelete() {
    this.spinnerService.show();
    let items: EmployeeTransferHistoryDetele[] = this.dataSelect.map(item => ({
      history_GUID: item.history_GUID,
      effective_Status: item.effective_Status
    }));
    this.service.batchDelete(items).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess) {
          this.getData();
          this.dataSelect = []
        }
      },
    });
  }

  delete(itemDelete: EmployeeTransferHistoryDetail) {
    let item: EmployeeTransferHistoryDetele = {
      history_GUID: itemDelete.history_GUID,
      effective_Status: itemDelete.effective_Status
    };
    this.functionUtility.snotifyConfirmDefault(async () => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          if (result.isSuccess) {
            this.getData();
            this.dataSelect = []
          }
        },
      });
    });
  }

  async selectMutiple() {
    this.selectAll != this.selectAll
    this.data.filter(x => !x.effective_Status).map(x => { x.checked = this.selectAll })
    await this.filterData()
  }

  async selectItem() {
    this.selectAll = this.data.filter(x => !x.effective_Status).every(x => x.checked);
    await this.filterData()
  }

  async filterData() {
    this.dataSelect = this.data.filter(x => x.checked)
    await Promise.all(this.dataSelect.map(async (x) => x.isEffectiveConfirm = await this.checkEffectiveConfirm(x)))
    this.isDisableBtnEffectiveConfirm = !(this.dataSelect.length > 0 && this.dataSelect.every(x => x.isEffectiveConfirm == true));
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  changeEffective_Date_Start() {
    if (this.effective_Date_Start_value != null && isNaN(this.effective_Date_Start_value.getTime()))
      this.effective_Date_Start_value = null;
  }
  changeEffective_Date_End() {
    if (this.effective_Date_End_value != null && isNaN(this.effective_Date_End_value.getTime()))
      this.effective_Date_End_value = null;
  }
  checkEffectiveConfirm(data: EmployeeTransferHistoryDetail): Promise<boolean> {
    return new Promise((resolve) => {
      let item = <EmployeeTransferHistoryEffectiveConfirm>{
        useR_GUID: data.useR_GUID,
        effective_Status: data.effective_Status,
        effective_Date: this.functionUtility.getDateFormat(data.effective_Date),
        history_GUID: data.history_GUID
      };
      this.service.checkEffectiveConfirm(item).subscribe({
        next: (res) => resolve(res.isSuccess),
        error: () => resolve(false)
      });
    })
  }
}
