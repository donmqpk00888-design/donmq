import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  DirectoryProgramLanguageSetting_Param,
  DirectoryProgramLanguageSetting_Data,
  DirectoryProgramLanguageSetting_Memory
} from '@models/system-maintenance/1_1_4_directory-program-language';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_1_1_4_DirectoryProgramLanguageSettingService } from '@services/system-maintenance/s_1_1_4_directory-program-language-setting.service';
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
  iconButton = IconButton;

  key: KeyValuePair[] = [
    { key: '', value: 'SystemMaintenance.DirectoryProgramLanguageSetting.All' },
    { key: 'P', value: 'SystemMaintenance.DirectoryProgramLanguageSetting.Program' },
    { key: 'D', value: 'SystemMaintenance.DirectoryProgramLanguageSetting.Directory' }
  ];
  data: DirectoryProgramLanguageSetting_Data[] = []
  param: DirectoryProgramLanguageSetting_Param = <DirectoryProgramLanguageSetting_Param>{}
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  isEdit: boolean = false;
  selectedData: DirectoryProgramLanguageSetting_Data
  constructor(
    private service: S_1_1_4_DirectoryProgramLanguageSettingService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource()
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    this.service.SetSource(<DirectoryProgramLanguageSetting_Memory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
      isEdit: this.isEdit,
      selectedData: this.selectedData
    });
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.pagination = this.service.programSource().pagination;
      this.data = this.service.programSource().data;
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search'))
          this.getDataPagination()
        else
          this.clear()
      }
    });
  }

  getDataPagination(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        this.pagination = result.pagination;
        this.data = result.result;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg');
      }
    });
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getDataPagination();
  }
  clear() {
    this.data = []
    this.param = <DirectoryProgramLanguageSetting_Param>{}
    this.pagination.totalCount = 0
  }

  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: DirectoryProgramLanguageSetting_Data) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(kind: string, code: string) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service.delete(kind, code).subscribe({
          next: result => {
            this.spinnerService.hide();
            this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? this.translateService.instant('System.Message.DeleteOKMsg') : result.error, false);
            if (result.isSuccess) this.getDataPagination()
          }
        })
      });
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getDataPagination(isSearch) : this.pagination.pageNumber = 1;
  }
}
