import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  DirectWorkTypeAndSectionSetting,
  DirectWorkTypeAndSectionSettingParam,
  HRMS_Org_Direct_SectionDto
} from '@models/organization-management/3_1_5_organization-management';
import { S_3_1_5_DirectWorkTypeAndSectionSettingService } from '@services/organization-management/s_3_1_5_direct-work-type-and-section-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{};
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listSection: KeyValuePair[] = [];
  iconButton = IconButton;
  effective_Date_value: Date = null;
  data: HRMS_Org_Direct_SectionDto[] = [];
  selectedData: HRMS_Org_Direct_SectionDto = <HRMS_Org_Direct_SectionDto>{};
  param: DirectWorkTypeAndSectionSettingParam = <DirectWorkTypeAndSectionSettingParam>{};

  constructor(
    private service: S_3_1_5_DirectWorkTypeAndSectionSettingService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.param.effective_Date != undefined && this.param.effective_Date != "")
        this.effective_Date_value = this.param.effective_Date.toDate();
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear(false)
        else
          this.getData()
      }
      this.getListSection();
      this.getListDivision();
      this.getListSection();
      this.getListWorkType();
      this.changeGetFactory();
    });
    this.service.paramSearchSource$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(param => {
      if (param) this.param = param;
    })
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListSection();
      this.getListDivision();
      this.getListSection();
      this.getListWorkType();
      this.changeGetFactory();
      this.getData();
    });
  }
  ngOnDestroy(): void {
    this.checkDate()
    this.service.setParamSearch(<DirectWorkTypeAndSectionSetting>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
      selectedData: this.selectedData
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListSection();
    this.getListWorkType();
  }

  checkDate() {
    if (this.effective_Date_value != null)
      this.param.effective_Date = this.effective_Date_value.toDate().toStringYearMonth();
    else this.deleteProperty('effective_Date');
  }
  getData(isSearch?: boolean) {
    this.checkDate()
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      },
    });
  }

  download() {
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }
  onDivisionChange() {
    this.deleteProperty('factory');
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.changeGetFactory();
  }
  changeGetFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListWorkType() {
    this.service.getListWorkType().subscribe({
      next: (res) => {
        this.listWorkType = res;
      },
    });
  }

  getListSection() {
    this.service.getListSection().subscribe({
      next: (res) => {
        this.listSection = res;
      },
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  add() {
    this.selectedData = <HRMS_Org_Direct_SectionDto>{ direct_Section: 'Y' };
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(item: HRMS_Org_Direct_SectionDto) {
    this.selectedData = { ...item }
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  clear(isClear: boolean) {
    this.deleteProperty('division')
    this.deleteProperty('factory')
    this.deleteProperty('effective_Date')
    this.deleteProperty('work_Type_Code')
    this.deleteProperty('section_Code')
    this.deleteProperty('direct_Section')

    this.effective_Date_value = null;
    this.changeGetFactory()
    if (isClear) {
      this.pagination.pageNumber = 1;
      this.data = [];
      this.pagination.totalCount = 0;
    }
    else this.functionUtility.checkFunction('Search') ? this.getData() : this.data = [];
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  deleteProperty = (name: string) => delete this.param[name]
}
