import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { Pagination } from '@utilities/pagination-utility';
import {
  SystemLanguageSetting_Data,
  SystemLanguageSetting_Memory
} from '@models/system-maintenance/1_1_3-system-language-setting';
import { InjectBase } from '@utilities/inject-base-app';
import { IconButton } from '@constants/common.constants';
import { S_1_1_3_SystemLanguageSettingService } from '@services/system-maintenance/s_1_1_3_system-language-setting.service';
import { LangChangeEvent } from '@ngx-translate/core';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  data: SystemLanguageSetting_Data[] = [];
  selectedData: SystemLanguageSetting_Data = <SystemLanguageSetting_Data>{};
  constructor(
    private service: S_1_1_3_SystemLanguageSettingService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource()
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
   }

  ngOnDestroy(): void {
    let source: SystemLanguageSetting_Memory = <SystemLanguageSetting_Memory>{
      selectedData: this.selectedData,
      pagination: this.pagination
    };
    this.service.setSource(source);
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  getDataFromSource() {
    effect(() => {
      this.pagination = this.service.basicCodeSource().pagination;
      if (this.functionUtility.checkFunction('Search')) {
        this.getData();
      } else
        this.clear()
    });
  }

  getData() {
    this.spinnerService.show();
    this.service.getAll(this.pagination).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
      },
    });
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: SystemLanguageSetting_Data) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  clear() {
    this.pagination.pageNumber = 1;
    this.data = [];
  }
  changeState(item: SystemLanguageSetting_Data) {
    if (item.isActive == true) {
      item.isActive = false;
      this.updateState(item);
    } else {
      item.isActive = true;
      this.updateState(item);
    }
  }

  updateState(item: SystemLanguageSetting_Data) {
    this.spinnerService.show();
    this.service.update(item).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.UpdateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.getData();
        } else this.snotifyService.error(res.error, this.translateService.instant('System.Caption.Error'));
      }
    });
  }
}
