import { InjectBase } from '@utilities/inject-base-app';
import { BsDatepickerViewMode, BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { NewResignedEmployeeDataPrintingParam, NewResignedEmployeeDataPrintingSource } from '@models/attendance-maintenance/5_2_1_new-resigned-employee-data-printing';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { S_5_2_1_NewResignedEmployeeDataPrintingService } from '@services/attendance-maintenance/s_5_2_1_new-resigned-employee-data-printing.service';
import { Observable } from 'rxjs';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  param: NewResignedEmployeeDataPrintingParam = <NewResignedEmployeeDataPrintingParam>{}
  total: number = 0;
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };
  dateFrom: Date;
  dateTo: Date;
  selectedKey: string = 'NewHired';
  key: KeyValuePair[] = [
    {
      'key': 'NewHired',
      'value': 'AttendanceMaintenance.NewResignedEmployeeDataPrinting.NewHired'
    },
    {
      'key': 'Resigned',
      'value': 'AttendanceMaintenance.NewResignedEmployeeDataPrinting.Resigned'
    },
  ];

  constructor(private service: S_5_2_1_NewResignedEmployeeDataPrintingService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListDepartment();
    });
    effect(() => {
      const { param, dateFrom, dateTo, selectedKey, total } = this.service.paramSearch();
      this.param = param;
      this.dateFrom = dateFrom;
      this.dateTo = dateTo;
      this.selectedKey = selectedKey;
      this.total = total;
      if (!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getListFactory();
        this.getListDepartment();
      }
    });
  }


  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<NewResignedEmployeeDataPrintingSource>{
      param: this.param,
      dateFrom: this.dateFrom,
      dateTo: this.dateTo,
      selectedKey: this.selectedKey,
      total: this.total
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.selectedKey) &&
      !this.functionUtility.checkEmpty(this.dateFrom) &&
      !this.functionUtility.checkEmpty(this.dateTo);
    return result;
  }

  //#region getList
  getListFactory() {
    this.getList(() => this.service.getListFactory(), this.listFactory);
  }

  getListDepartment() {
    this.getList(() => this.service.getListDepartment(this.param.factory), this.listDepartment);
  }

  onFactoryChange() {
    this.deleteProperty('department');
    this.getListDepartment();
  }

  getList(serviceMethod: () => Observable<KeyValuePair[]>, resultList: KeyValuePair[]) {
    serviceMethod().subscribe({
      next: (res) => {
        resultList.length = 0;
        resultList.push(...res);
      },
    });
  }
  //#endregion

  //#region search
  getTotal(isSearch?: boolean): Promise<void> {
    this.spinnerService.show();
    Object.assign(this.param, {
      date_From: this.formatDate(this.dateFrom),
      date_To: this.formatDate(this.dateTo),
      kind: this.selectedKey
    });

    return this.service.getTotal(this.param).toPromise().then(res => {
      this.total = res;
      if (isSearch) {
        this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');
        this.spinnerService.hide()
      }
    })
  }

  search(isSearch: boolean) {
    this.getTotal(isSearch);
  }
  //#endregion

  //#region clear
  clear() {
    this.param = <NewResignedEmployeeDataPrintingParam>{};
    this.listDepartment = [];
    this.dateFrom = null;
    this.dateTo = null;
    this.selectedKey = 'NewHired';
    this.total = 0;
  }
  //#endregion

  //#region download
  download() {
    this.spinnerService.show();
    Object.assign(this.param, {
      date_From: this.formatDate(this.dateFrom),
      date_To: this.formatDate(this.dateTo),
      kind: this.selectedKey
    });

    this.service.downloadExcel(this.param).subscribe({
      next: async (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
          await this.getTotal();
        } else
          this.functionUtility.snotifySuccessError(false, 'System.Message.NoData');
        this.spinnerService.hide()
      }
    });
  }
  //#endregion

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
