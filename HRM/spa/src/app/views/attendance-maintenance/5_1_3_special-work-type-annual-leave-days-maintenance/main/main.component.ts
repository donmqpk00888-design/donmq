import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Work_Type_DaysDto, SpecialWorkTypeAnnualLeaveDaysMaintenanceParam, SpecialWorkTypeAnnualLeaveDaysMaintenanceSource } from '@models/attendance-maintenance/5_1_3_special-work-type-annual-leave-days-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService } from '@services/attendance-maintenance/s_5_1_3_special-work-type-annual-leave-days-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  userName: string = '';
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalPage: 0,
    totalCount: 0
  };
  iconButton = IconButton;
  classButton = ClassButton;
  param: SpecialWorkTypeAnnualLeaveDaysMaintenanceParam = {
    division: '',
    factory: '',
    lang: '',
  };
  data: HRMS_Att_Work_Type_DaysDto[] = [];
  source: SpecialWorkTypeAnnualLeaveDaysMaintenanceSource
  constructor(
    private service: S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getListDivision();
        this.getListFactory();
        if (
          this.functionUtility.checkFunction('Search') &&
          this.data?.length > 0
        )
          this.getData(false);
      });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    const userInfo = JSON.parse(localStorage.getItem(LocalStorageConstants.USER))
    this.userName = userInfo?.name;
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  search() {
    this.pagination.pageNumber === 1
      ? this.getData(true)
      : (this.pagination.pageNumber = 1);
  };

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    })
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res
      }
    });
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res
      }
    });
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  ngOnDestroy(): void {
    if (!this.source)
      this.source = <SpecialWorkTypeAnnualLeaveDaysMaintenanceSource>{
        pagination: this.pagination,
        param: { ...this.param }
      };
    this.service.setSource(this.source);
  }

  clear(isClear?: boolean) {
    if (isClear)
      this.functionUtility.snotifySuccessError(true, 'System.Message.ClearSuccess')

    this.param = {
      division: '',
      factory: '',
      lang: '',
    }
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
    this.data = [];
    this.service.paramSource().param = this.param;
  }

  edit(item: HRMS_Att_Work_Type_DaysDto) {
    item.work_Type = item.work_Type.split('-')[0]?.trim() ?? '';
    item.update_Time = this.functionUtility.getDateTimeFormat(new Date());
    this.source = <SpecialWorkTypeAnnualLeaveDaysMaintenanceSource>{
      pagination: this.pagination,
      param: { ...this.param },
      data: item
    };
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  getDataFromSource() {
    effect(() => {
      if (this.functionUtility.isEmptyObject(this.service.paramSource().param)) {
        this.clear();
      }
      else {
        this.param = this.service.paramSource().param;
        this.pagination = this.service.paramSource().pagination;
        this.getListFactory();
        this.getListDivision();
        if (this.functionUtility.checkFunction('Search'))
          this.getData(false);
      };
    });
  }
  download() {
    this.spinnerService.show();
    this.service
      .downloadExcel(this.param)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }

  onChangeDivision() {
    this.param.factory = '';
    this.getListFactory();
  }
}
