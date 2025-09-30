import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { BasicCode, HRMS_Basic_Code_TypeDto, HRMS_Basic_Code_TypeParam, HRMS_Type_Code_Source, ResultMain } from '@models/basic-maintenance/2_1_3_type-code-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_3_CodeTypeMaintenanceService } from '@services/basic-maintenance/s_2_1_3_code-type-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  title: string = '';
  iconButton = IconButton;
  param: HRMS_Basic_Code_TypeParam = <HRMS_Basic_Code_TypeParam>{};
  data: ResultMain[] = [];
  constructor(private service: S_2_1_3_CodeTypeMaintenanceService,
  ) {
    super();
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear()
        else
          this.getData()
      }
    });
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => this.title = res['title']);
    this.service.currentParamSearch.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(param => {
      if (param) this.param = param;
    })
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<BasicCode>{ param: this.param, pagination: this.pagination, data: this.data });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData()
  }
  clear() {
    this.param.type_Seq = "";
    this.param.type_Name = "";
    this.pagination.totalCount = 0;
    this.data = []
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getAll(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.pagination = res.pagination;
        this.data = res.result;

        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    })
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  onAdd() {
    let source: HRMS_Type_Code_Source = <HRMS_Type_Code_Source>{
      isEdit: false,
      currentPage: this.pagination.pageNumber,
    };
    this.service.setSource(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  onEdit(item: HRMS_Basic_Code_TypeDto) {
    let source: HRMS_Type_Code_Source = <HRMS_Type_Code_Source>{
      isEdit: true,
      currentPage: this.pagination.pageNumber,
      source: { ...item }
    };
    this.service.typeCodeSource.set(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  onDelete(type_Seq: string) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(type_Seq).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.getData()
        }
      })
    });
  }
}
