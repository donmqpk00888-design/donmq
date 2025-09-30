import { Component, OnDestroy, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Org_Work_Type_Headcount, HRMS_Org_Work_Type_HeadcountParam, HRMS_Org_Work_Type_HeadcountSource } from '@models/organization-management/3_1_2_work-type-headcount-maintenance';
import { S_3_1_2_WorktypeHeadcountMaintenanceService } from '@services/organization-management/s_3_1_2_work-type-headcount-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnDestroy {

  //#region Data
  divisions: KeyValuePair[] = [];
  factories: KeyValuePair[] = [];
  allFactories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];

  workTypeHeads: HRMS_Org_Work_Type_Headcount[] = [];

  totalHeadCount: number = 0;
  totalActualNumber: number = 0;
  currentDate = new Date();
  selectedData: HRMS_Org_Work_Type_HeadcountSource = null
  //#endregion

  //#region Vaiables
  effective_Date_value: Date = null;
  param: HRMS_Org_Work_Type_HeadcountParam = <HRMS_Org_Work_Type_HeadcountParam>{}

  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  //#endregion

  //#region Pagination
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10
  }
  //#endregion

  constructor(
    private workTypeHeadcountMaintermanceServices: S_3_1_2_WorktypeHeadcountMaintenanceService,
  ) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    // Load danh sách Data trước đó
    effect(() => {
      this.param = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().param;
      this.workTypeHeads = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().data;
      this.effective_Date_value = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().effective_Date_value;
      this.pagination = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().pagination;
      this.totalActualNumber = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().totalActualNumber;
      this.totalHeadCount = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource().totalHeadCount;
      if (this.workTypeHeads.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear()
        else
          this.getPaginationData()
      }
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDivisions();
      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getFactoriesByDivision()
        if (!this.functionUtility.checkEmpty(this.param.factory))
          this.getDepartmentsByDivisionAndFactory()
      }
      else this.getAllFactories();
      this.getPaginationData();
    });

  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      // Load danh sách Divisions
      this.divisions = res.resolverDivisions
      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getFactoriesByDivision()
        if (!this.functionUtility.checkEmpty(this.param.factory))
          this.getDepartmentsByDivisionAndFactory()
      }
      else // Load danh sách All Factories
        this.factories = res.resolverFactories

    });
  }
  ngOnDestroy(): void {
    if (!this.selectedData)
      this.selectedData = this.setDataSource()
    this.workTypeHeadcountMaintermanceServices.setSource(this.selectedData);
  }

  //#region Methods
  getDivisions() {
    this.workTypeHeadcountMaintermanceServices.getDivisions().subscribe({
      next: result => {
        this.divisions = result;
      }
    })
  }

  getAllFactories() {
    this.workTypeHeadcountMaintermanceServices.getFactories().subscribe({
      next: result => {
        this.allFactories = result;
        this.factories = result;
      }
    })
  }

  getFactoriesByDivision() {
    this.workTypeHeadcountMaintermanceServices.getFactoriesByDivision(this.param.division, ).subscribe({
      next: result => {
        if (result.length > 0)
          this.factories = result;
        else this.factories = [...this.allFactories];
      }
    })
  }

  getDepartmentsByDivisionAndFactory() {
    this.workTypeHeadcountMaintermanceServices.getDepartmentsByDivisionFactory(this.param.division, this.param.factory, ).subscribe({
      next: result => {
        this.departments = result;
      }
    })
  }

  getPaginationData(isSearch?: boolean) {
    if (this.effective_Date_value != null)
      this.param.effective_Date = this.effective_Date_value.toDate().toStringYearMonth();
    this.spinnerService.show();
    this.workTypeHeadcountMaintermanceServices.getDataMainPagination(this.pagination, this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.totalHeadCount = result.totalApprovedHeadcount;
        this.totalActualNumber = result.totalActual;
        this.workTypeHeads = result.dataPagination.result.map(x => { return { ...x, isDelete: new Date(x.effective_Date) > this.currentDate } });
        this.pagination = result.dataPagination.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true,'System.Message.QuerySuccess')
      }
    })
  }
  excel() {
    this.spinnerService.show();
    if (this.effective_Date_value != null)
      this.param.effective_Date = this.effective_Date_value.toDate().toStringYearMonth();
    this.workTypeHeadcountMaintermanceServices
      .downloadExcel(this.param)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }

  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getPaginationData(isSearch) : this.pagination.pageNumber = 1;
  resetSearch() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.totalHeadCount = 0;
    this.totalActualNumber = 0;
    this.workTypeHeads = [];
    this.workTypeHeadcountMaintermanceServices.setSource(this.setDataSource(null));
  }

  clear() {
    this.deleteProperty('division')
    this.deleteProperty('factory')
    this.deleteProperty('department_Code')
    this.deleteProperty('effective_Date')
    this.factories = this.allFactories;
    this.departments = [];
    this.effective_Date_value = null;
    this.resetSearch();
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPaginationData();
  }

  setDataSource(item?: HRMS_Org_Work_Type_Headcount) {
    return <HRMS_Org_Work_Type_HeadcountSource>{
      pagination: this.pagination,
      param: { ...this.param },
      data: this.workTypeHeads,
      effective_Date_value: this.effective_Date_value,
      model: item,
      totalHeadCount: this.totalHeadCount,
      totalActualNumber: this.totalActualNumber
    }
  }

  deleteProperty = (name: string) => delete this.param[name]

  //#endregion

  //#region Events
  onDivisionChange() {
    this.factories = [...this.allFactories];
    this.deleteProperty('factory')
    this.deleteProperty('department_Code')
    this.departments = [];
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getFactoriesByDivision();
  }

  onFactoryChange() {
    this.deleteProperty('department_Code')
    this.departments = [];
    if (!this.functionUtility.checkEmpty(this.param.division) && !this.functionUtility.checkEmpty(this.param.factory))
      this.getDepartmentsByDivisionAndFactory();
  }

  onAdd() {
    // set param
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: HRMS_Org_Work_Type_Headcount) {
    // set param
    this.selectedData = this.setDataSource({ ...item })
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(item: HRMS_Org_Work_Type_Headcount) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.workTypeHeadcountMaintermanceServices.delete(item).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.search(false);
        }
      })
    });
  }
  //#endregion

}
