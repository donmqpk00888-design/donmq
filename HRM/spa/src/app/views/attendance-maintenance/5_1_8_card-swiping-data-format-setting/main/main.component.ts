import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { CardSwipingDataFormatSettingMain, CardSwipingDataFormatSettingSource } from '@models/attendance-maintenance/5_1_8_hrms_att_swipecard_set';
import { S_5_1_8_CardSwipingDataFormatSettingService } from '@services/attendance-maintenance/s_5_1_8_card-swiping-data-format-setting.service'
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.css'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  iconButton = IconButton;
  factorys: KeyValuePair[] = [];
  factory: string;
  pagination: Pagination = <Pagination>{};
  data: CardSwipingDataFormatSettingMain[] = [];

  constructor(
    private service: S_5_1_8_CardSwipingDataFormatSettingService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.processData()
      });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave && this.functionUtility.checkFunction('Search') && this.factory)
        this.getData();
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.factory = this.service.paramSource().factory;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.factory)
        this.getData(false)
      else
        this.clear()
    }
    this.getListFactory()
  }

  getListFactory() {
    this.service.getFactoryMain().subscribe({
      next: res => {
        this.factorys = res;
      }
    });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.factory).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    })
  }

  clear() {
    this.deleteProperty('factory')
    this.data = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
  }

  search() {
    this.pagination.pageNumber === 1
      ? this.getData(true)
      : (this.pagination.pageNumber = 1);
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  // area modal form
  onAdd = () => this.modalService.open({ factory: this.factory, action: 'Add' });
  onEdit = (factory: string) => this.modalService.open({ factory: factory, action: 'Edit' });



  ngOnDestroy(): void {
    this.service.setSource(<CardSwipingDataFormatSettingSource>{ factory: this.factory, pagination: this.pagination, data: this.data });
  }
  deleteProperty(name: string) {
    delete this[name]
  }
}
