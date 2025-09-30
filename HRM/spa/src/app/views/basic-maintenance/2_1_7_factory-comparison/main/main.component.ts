import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Basic_Factory_Comparison, HRMS_Basic_Factory_ComparisonSource } from '@models/basic-maintenance/2_1_7_factory-comparison';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_7_FactoryComparisonService } from '@services/basic-maintenance/s_2_1_7_factory-comparison.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  kind: string
  kinds: KeyValuePair[] = [
    { key: '1', value: 'BasicMaintenance.FactoryComparison.Kind1' },
    { key: 'X', value: 'BasicMaintenance.FactoryComparison.KindX' },
  ];
  data: HRMS_Basic_Factory_Comparison[] = []
  title: string = '';
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  constructor(
    private service: S_2_1_7_FactoryComparisonService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });

    effect(() => {
      this.kind = this.service.factoryComparisonSource().kind;
      this.pagination = this.service.factoryComparisonSource().pagination;
      this.data = this.service.factoryComparisonSource().data;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clearSearch()
        else {
          this.getPaginationData()
        }
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  ngOnDestroy(): void {
    this.service.setSource(this.setParamSource());
  }

  clearSearch() {
    this.resetKind()
    this.pagination.totalCount = 0
    this.data = []
  }

  getPaginationData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getDataMainPagination(this.pagination, this.kind ?? '').subscribe({
      next: result => {
        this.spinnerService.hide();
        this.data = result.result;
        this.pagination = result.pagination;
        if (isSearch) {
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');
        }
      }
    })
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getPaginationData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPaginationData();
  }
  setParamSource() {
    // set param
    return <HRMS_Basic_Factory_ComparisonSource>{
      pagination: this.pagination,
      isEdit: false,
      kind: this.kind,
      factoryComparison: <HRMS_Basic_Factory_Comparison>{ kind: '', factory: '', division: '' },
      data: this.data
    }
  }

  onAdd = () =>  this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);

  onDelete(item: HRMS_Basic_Factory_Comparison) {
    let dataDelete: HRMS_Basic_Factory_Comparison = <HRMS_Basic_Factory_Comparison>{
      kind: item.kind,
      factory: item.factory.split("-")[0],
      division: item.division.split("-")[0]
    }

    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(dataDelete).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.search(false);
        }
      })
    })
  }
  resetKind = () => delete this.kind

}
