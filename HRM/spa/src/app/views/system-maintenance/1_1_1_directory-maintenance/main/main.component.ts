import { InjectBase } from '@utilities/inject-base-app';
import { IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import {
  DirectoryMaintenance_Param,
  DirectoryMaintenance_Data,
  DirectoryMaintenance_Memory
} from '@models/system-maintenance/1_1_1_directory-maintenance';
import { S_1_1_1_DirectoryMaintenanceService } from '@services/system-maintenance/s_1_1_1_directory-maintenance.service';
import { Pagination } from '@utilities/pagination-utility';
import { LangChangeEvent } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  listData: DirectoryMaintenance_Data[] = [];
  selectedData: DirectoryMaintenance_Data = <DirectoryMaintenance_Data>{};
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  param: DirectoryMaintenance_Param = <DirectoryMaintenance_Param>{}
  listParentDirectoryCode: KeyValuePair[] = [];
  title: string = '';
  iconButton = IconButton;

  constructor(
    private service: S_1_1_1_DirectoryMaintenanceService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    effect(() => {
      this.param = this.service.directorySource().param;
      this.pagination = this.service.directorySource().pagination;
      this.listData = this.service.directorySource().data;
      if (this.listData.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clearSearch()
        else
          this.getData()
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    let source: DirectoryMaintenance_Memory = <DirectoryMaintenance_Memory>{
      selectedData: this.selectedData,
      pagination: this.pagination,
      param: this.param,
      data: this.listData
    };
    this.service.setSource(source);
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.listData = res.result;
        this.pagination = res.pagination;
        this.spinnerService.hide();
        if (isSearch) {
          this.snotifyService.success(this.translateService.instant('System.Message.QuerySuccess'), this.translateService.instant('System.Caption.Success'));
        }
      }
    });
  }

  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clearSearch() {
    this.param = <DirectoryMaintenance_Param>{}
    this.pagination.totalCount = 0
    this.listData = []
  }

  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: DirectoryMaintenance_Data) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  deleteData(directory_Code: string) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show();
      this.service.deleteData(directory_Code).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.DeleteOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.getData();
          }
          else {
            this.snotifyService.error(res.error, this.translateService.instant('System.Caption.Error'));
          }
          this.spinnerService.hide();
        }
      });
    });
  }
}
