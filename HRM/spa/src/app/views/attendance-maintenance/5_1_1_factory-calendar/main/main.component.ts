import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Day, FactoryCalendar_MainData, FactoryCalendar_MainMemory, FactoryCalendar_MainParam, FactoryCalendar_Table } from '@models/attendance-maintenance/5_1_1_factory-calendar';
import { S_5_1_1_FactoryCalendar } from '@services/attendance-maintenance/s_5_1_1_factory-calendar.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};
  minMode: BsDatepickerViewMode = 'month';
  acceptFormat: string = '.xls, .xlsx, .xlsm';

  iconButton = IconButton;
  classButton = ClassButton;

  param: FactoryCalendar_MainParam = <FactoryCalendar_MainParam>{};
  data: FactoryCalendar_MainData = <FactoryCalendar_MainData>{
    table: {
      result: [],
      pagination: <Pagination>{
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0
      }
    },
    calendar: {
      weeks: []
    }
  };

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  categoryList: KeyValuePair[] = [];

  title: string
  programCode: string = '';

  constructor(
    private service: S_5_1_1_FactoryCalendar
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
      if (this.data.table.result.length > 0)
        this.getData(false)
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.data = this.service.paramSearch().data;
      if (!this.functionUtility.checkEmpty(this.param.division))
        this.retryGetDropDownList()
      this.data = this.service.paramSearch().data
      if (this.data.table.result.length > 0) {
        if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
          this.getData(false)
        else
          this.clear()
      }
    });
  }
  ngOnInit(): void {
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM',
        minMode: this.minMode
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.filterList(role.dataResolved)
      });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<FactoryCalendar_MainMemory>{
      param: this.param,
      data: this.data
    });
  }

  retryGetDropDownList() {
    this.service.getDropDownList(this.param.division)
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.divisionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getData = (isSearch: boolean = false) => {
    this.spinnerService.show();
    this.param.lang = localStorage.getItem(LocalStorageConstants.LANG)
    this.service
      .getSearchDetail(this.data.table.pagination, this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.data.table = res.data;
          this.calculateCalendar();
          if (isSearch)
            this.snotifyService.success(
              this.translateService.instant('System.Message.SearchOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
        }
      });
  };
  search = () => {
    this.data.table.pagination.pageNumber == 1
      ? this.getData(true)
      : (this.data.table.pagination.pageNumber = 1);
  };
  clear() {
    this.param = <FactoryCalendar_MainParam>{};
    this.data = <FactoryCalendar_MainData>{
      table: {
        result: [],
        pagination: <Pagination>{
          pageNumber: 1,
          pageSize: 10,
          totalCount: 0
        }
      },
      calendar: {
        weeks: []
      }
    }
    this.data.table.pagination.pageNumber = 1
    this.data.table.pagination.totalCount = 0
  }
  checkRequiredParams(): boolean {
    let result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.param.month)
    return result
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
    this.deleteProperty('month')
  }
  changePage = (e: PageChangedEvent) => {
    this.data.table.pagination.pageNumber = e.page;
    this.getData(false);
  };
  add() {
    this.service.setParamForm(<FactoryCalendar_Table>{});
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(e: FactoryCalendar_Table) {
    this.spinnerService.show();
    this.service.checkExistedData(e.division, e.factory, e.att_Date_Str)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.service.setParamForm(e);
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
          }
          else {
            this.getData(false)
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.FactoryCalendar.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  delete(e: FactoryCalendar_Table) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show()
      this.service.deleteData(e).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.DeleteOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
            this.getData(false);
          }
          else {
            this.snotifyService.success(
              this.translateService.instant('System.Message.DeleteErrorMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          }
        }
      })
    });
  }
  onDateChange() {
    this.param.month_Str = this.param.month ? this.functionUtility.getDateFormat(new Date(this.param.month)) : '';
  }
  calculateCalendar() {
    const selectedDate = new Date(this.param.month)
    if (selectedDate instanceof Date && !isNaN(selectedDate.getTime())) {
      this.data.calendar.weeks = this.service.getCalendarTemplate(this.param.division, this.param.factory, selectedDate.getMonth(), selectedDate.getFullYear())
      if (this.data.table.result.length > 0)
        this.data.calendar.weeks.map(week => {
          week.days.map(day => {
            if (this.data.table.result.some(x => x.att_Date_Str == day.date_String))
              day.style = day.style.includes('today') ? 'red-date today' : 'red-date'
          })
        })
    }
    else this.data.calendar.weeks = []
  }
  downloadTemplate() {
    this.spinnerService.show();
    this.service
      .downloadExcelTemplate()
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            var link = document.createElement('a');
            document.body.appendChild(link);
            link.setAttribute("href", res.data);
            link.setAttribute("download", `${this.functionUtility.getFileNameExport(this.programCode, 'Template')}.xlsx`);
            link.click();
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.FactoryCalendar.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  download() {
    this.spinnerService.show();
    this.service
      .downloadExcel(this.param)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }
  upload(event: FileResultModel) {
    this.spinnerService.show();
    this.service.uploadExcel(event.formData).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
            this.getData();
          this.functionUtility.snotifySuccessError(true, 'System.Message.UploadOKMsg')
        } else {
          if (!this.functionUtility.checkEmpty(res.data)) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Report')
            this.functionUtility.exportExcel(res.data, fileName);
          }
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
        }
      }
    });
  }
  onDateClick(day: Day) {
    if (day.style != 'disabled-date') {
      this.spinnerService.show();
      this.service.checkExistedData(day.division, day.factory, day.date_String)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              this.service.setParamForm(<FactoryCalendar_Table>{
                division: res.data.division,
                factory: res.data.factory,
                type_Code: res.data.type_Code,
                describe: res.data.describe,
                att_Date: res.data.att_Date,
                att_Date_Str: this.functionUtility.getDateFormat(new Date(res.data.att_Date)),
                update_By: res.data.update_By,
                update_Time: res.data.update_Time,
                update_Time_Str: this.functionUtility.getDateTimeFormat(new Date(res.data.update_Time)),
              });
              this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
            }
            else {
              this.service.setParamForm(<FactoryCalendar_Table>{
                division: day.division,
                factory: day.factory,
                att_Date: new Date(day.date_String),
                att_Date_Str: day.date_String
              });
              this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
            }
          }
        });
    }
  }
}
