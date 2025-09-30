import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IconButton } from '@constants/common.constants';
import { RewardandPenaltyMaintenance, RewardandPenaltyMaintenance_Basic, RewardandPenaltyMaintenanceParam } from '@models/reward-and-penalty-maintenance/8_1_1_reward-and-penalty-reason-code-maintenance';
import { S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService } from '@services/reward-and-penalty-maintenance/s_8_1_1_reward-and-penalty-reason-code-maintenance.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  title: string = '';
  programCode: string = '';
  param: RewardandPenaltyMaintenanceParam = <RewardandPenaltyMaintenanceParam>{};
  data: RewardandPenaltyMaintenance[] = [];
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listReason: KeyValuePair[] = [];
  source: RewardandPenaltyMaintenance = <RewardandPenaltyMaintenance>{};

  constructor(
    private service: S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getDataFromSource();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }
  getDataFromSource() {
    this.service.source$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(source => {
      if (source && source != null) {
        this.pagination = source.pagination;
        this.param = source.param;
        this.getListFactory();
        if (source.data.length > 0) {
          if (this.functionUtility.checkFunction('Search')
            && !this.functionUtility.checkEmpty(this.param.factory)) {
            this.getData();
          }
        }
      }
    })
  }
  getSource() {
    this.param = this.service.paramSource().param;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
    this.getListFactory();
    this.getListReason();
    this.getDataFromSource();
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.param.factory) {
        this.getData(false)
      }
      else
        this.clear()
    }
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  getListReason() {
    this.service.getListReason(this.param.factory).subscribe({
      next: (res) => {
        this.listReason = res
      },
    });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    if (!this.param.reason_Code) this.deleteProperty('reason_Code');
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

  ngOnDestroy(): void {
    this.service.setSource(<RewardandPenaltyMaintenance_Basic>{
      param: this.param,
      pagination: this.pagination,
      source: this.source,
      data: this.data
    });
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clear() {
    this.param = <RewardandPenaltyMaintenanceParam>{}
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }
  deleteProperty = (name: string) => delete this.param[name]

  redirectToForm = (isEdit: boolean = false) => this.router.navigate([`${this.router.routerState.snapshot.url}/${isEdit ? 'edit' : 'add'}`]);
  onForm(item: RewardandPenaltyMaintenance = null) {
    this.source = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }
  delete(item: RewardandPenaltyMaintenance) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service.delete(item).subscribe({
          next: (result) => {
            this.spinnerService.hide();
            if (result.isSuccess) {
              this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
              this.getData(false);
            }
            else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          },

        });
      }
    );
  }

  download() {
    if (this.data.length == 0)
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
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
  onChangeFactory() {
    this.param.reason_Code = null;
    this.getListReason();
    this.data = [];
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
  }
}
