import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { S_1_1_2_ProgramMaintenanceService } from '@services/system-maintenance/s_1_1_2_program-maintenance.service';
import { Pagination } from '@utilities/pagination-utility';
import { InjectBase } from '@utilities/inject-base-app';
import {
  ProgramMaintenance_Param,
  ProgramMaintenance_Data,
  ProgramMaintenance_Memory,
} from '@models/system-maintenance/1_1_2-program-maintenance';
import { IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { LangChangeEvent } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  data: ProgramMaintenance_Data[] = [];
  pagination: Pagination = <Pagination>{};
  title: string = '';
  param: ProgramMaintenance_Param = <ProgramMaintenance_Param>{};
  ListDirectory: KeyValuePair[] = [];
  selectedData: ProgramMaintenance_Data = <ProgramMaintenance_Data>{}
  constructor(private service: S_1_1_2_ProgramMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource();
  }
  ngOnDestroy(): void {
    let source: ProgramMaintenance_Memory = <ProgramMaintenance_Memory>{
      selectedData: this.selectedData,
      pagination: this.pagination,
      param: this.param,
      data: this.data
    };
    this.service.setSource(source);
  }
  getDataFromSource() {
    effect(() => {
      this.param = this.service.param().param;
      this.pagination = this.service.param().pagination;
      this.data = this.service.param().data;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search')) {
          this.clear()
        }
        else {
          this.getData()
        }
      }
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDirectory();
  }
  search = (isSearch: boolean) =>
    this.pagination.pageNumber === 1
      ? this.getData(isSearch)
      : (this.pagination.pageNumber = 1);

  getDirectory = () => this.service.getDirectory().subscribe({ next: (res) => this.ListDirectory = res });

  clear() {
    this.param = <ProgramMaintenance_Param>{}
    this.pagination.pageNumber = 1;
    this.data = [];
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.data.map(x => x.functions_Str = x.functions.join('/'))
        this.pagination = res.pagination;
        if (isSearch)
          this.snotifyService.success(
            this.translateService.instant(
              'SystemMaintenance.ProgramMaintenance.QueryOKMsg'
            ),
            this.translateService.instant('System.Caption.Success')
          );
      },
    });
  }
  onAdd = () => this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);

  onEdit(item: ProgramMaintenance_Data) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  deleteItem(item: ProgramMaintenance_Data) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service
          .delete(item.program_Code)
          .subscribe({
            next: (res) => {
              this.spinnerService.hide();
              if (res.isSuccess) {
                this.getData();
                this.snotifyService.success(
                  this.translateService.instant('System.Message.DeleteOKMsg'),
                  this.translateService.instant('System.Caption.Success')
                );
              } else {
                this.snotifyService.error(
                  this.translateService.instant(res.error),
                  this.translateService.instant('System.Caption.Error')
                );
              }
            }
          })
      }
    );
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
