import { Component, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_1_5_PregnancyAndMaternityDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_5_pregnancy-and-maternity-data-maintenance.service';
import { Pagination } from '@utilities/pagination-utility';
import { ClassButton, IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import {
  PregnancyMaternityDetail,
  PregnancyMaternityMemory,
  PregnancyMaternityParam
} from '@models/attendance-maintenance/5_1_5_pregnancy_and_maternity_data';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  params: PregnancyMaternityParam = <PregnancyMaternityParam>{};
  pagination: Pagination = <Pagination>{};
  datas: PregnancyMaternityDetail[] = [];
  selectedData: PregnancyMaternityDetail = <PregnancyMaternityDetail>{}

  iconButton = IconButton;
  classButton = ClassButton;

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
    adaptivePosition: true
  };

  constructor(
    private _service: S_5_1_5_PregnancyAndMaternityDataMaintenanceService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.processData()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }
  getDataFromSource() {
    this.params = this._service.paramSearch().params;
    this.pagination = this._service.paramSearch().pagination;
    this.datas = this._service.paramSearch().datas;
    this.processData()
  }
  processData() {
    if (this.datas.length > 0) {
      if (this.functionUtility.checkFunction('Search') && !this.functionUtility.checkEmpty(this.params.factory))
        this.getData(false)
      else
        this.clear()
    }
    this.loadDropdownList();
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
  }
  ngOnDestroy(): void {
    this._service.setParamSearch(<PregnancyMaternityMemory>{
      params: this.params,
      pagination: this.pagination,
      datas: this.datas,
      selectedData: this.selectedData
    });
  }

  getListFactory() {
    this._service.getListFactory()
      .subscribe({
        next: (res) => {
          this.factories = res
        }
      });
  }

  changeFactory() {
    this.deleteProperty('department_Code')
    this.getListDepartment();
  }
  onDateChange(name: string) {
    this.params[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.params[name]))
      ? this.functionUtility.getDateFormat(new Date(this.params[name]))
      : '';
  }
  getListDepartment() {
    if (this.params.factory) {
      this._service.getListDepartment(this.params.factory)
        .subscribe({
          next: (res) => {
            this.departments = res
          }
        });
    }
  }

  getData(isSearch: boolean = true) {
    this.spinnerService.show();
    this._service.query(this.pagination, this.params)
      .subscribe({
        next: (res) => {
          this.datas = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.snotifyService.success(
              this.translateService.instant('System.Message.SearchOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );

          this.spinnerService.hide();
        }
      });
  }

  search = () => {
    this.pagination.pageNumber == 1 ? this.getData() : this.pagination.pageNumber = 1;
  };

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData(false);
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: PregnancyMaternityDetail) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  detail(item: PregnancyMaternityDetail) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/detail`]);
  }

  delete(item: PregnancyMaternityDetail) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show();
        this._service.delete(item).subscribe({
          next: res => {
            if (res.isSuccess) {
              this.snotifyService.success(
                this.translateService.instant('System.Message.DeleteOKMsg'),
                this.translateService.instant('System.Caption.Success')
              );
              this.getData(false);
            } else {
              this.snotifyService.error(
                this.translateService.instant('System.Message.DeleteErrorMsg'),
                this.translateService.instant('System.Caption.Error')
              );
            }
            this.spinnerService.hide();
          }
        });
      });
  }

  download() {
    this.spinnerService.show();
    this._service.exportExcel(this.params).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
      },
    });
  }

  clear() {
    this.params = <PregnancyMaternityParam>{}
    this.departments = [];
    this.datas = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0
    };
  }
  deleteProperty(name: string) {
    delete this.params[name]
  }
}
