import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import {
  ClassButton,
  IconButton
} from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ValidateResult } from '@models/base-source';

import { MonthlyEmployeeStatusChangesSheet_ByWorkTypeJobParam } from '@models/attendance-maintenance/5_2_13_monthly-employee-status-changes-sheet-by-work-type-job';
import { S_5_2_13_MonthlyEmployeeStatusChangesSheetByWorkTypeJobService } from '@services/attendance-maintenance/s_5_2_13_monthly-employee-status-changes-sheet-by-work-type-job.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  //#region Commons
  title: string = '';
  programCode: string = '';
  datePickerConfig: Partial<BsDatepickerConfig> = {
    minMode: 'month',
    dateInputFormat: 'YYYY/MM',
    isAnimated: true
  }
  //#endregion

  //#region Variables
  totalRows: number = 0;
  yearMontDate: Date = null;
  //#endregion

  //#region Objects
  iconButton = IconButton;
  classButton = ClassButton;

  param: MonthlyEmployeeStatusChangesSheet_ByWorkTypeJobParam = <MonthlyEmployeeStatusChangesSheet_ByWorkTypeJobParam>{
    permisionGroup: [],
    work_Type: []
  };
  //#endregion

  //#region Arrays
  factories: KeyValuePair[] = [];
  levels: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  workTypeJobs: KeyValuePair[] = [];
  //#endregion

  //#region Pagination
  //#endregion

  constructor(private _services: S_5_2_13_MonthlyEmployeeStatusChangesSheetByWorkTypeJobService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this._services.baseInitSource();

      if (!this.functionUtility.checkEmpty(this.param.yearMonth)) {
        let datetime = this.param.yearMonth.split('/');
        this.yearMontDate = new Date(+datetime[0], +datetime[1] - 1)
      }

      // Nếu có param load lại dropdown
      if (!this.functionUtility.checkEmpty(this.param.factory))
        this.getPermistionGroups();

      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getTotalRecords(false);
      }
      else this.clear()
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();

      // Nếu có param load lại dropdown
      if (!this.functionUtility.checkEmpty(this.param.factory))
        this.getPermistionGroups();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.loadDropDownList();
  }

  ngOnDestroy(): void {
    this._services.setSource(this.param);
  }

  //#region Methods

  disableSearch() {
    return (this.functionUtility.checkEmpty(this.param.factory) ||
      this.yearMontDate == null ||
      this.yearMontDate == undefined ||
      this.functionUtility.checkEmpty(this.param.level) ||
      this.param.permisionGroup.length == 0 ||
      this.param.work_Type.length == 0
    )
  }

  loadDropDownList() {
    this.getFactories();
    this.getLevels();
    this.getWorkTypeJobs();
  }

  joinArray = (arr: string[]) => arr.join(',')
  stringToArray = (value: string) => value.split(',')

  validate(): ValidateResult {
    if (this.yearMontDate != null && (this.yearMontDate == undefined || this.yearMontDate.toString() == "Invalid Date"))
      return new ValidateResult(`Year Month invalid`);

    return { isSuccess: true };
  }

  convertData() {
    // Convert Data
    this.param.yearMonth = this.yearMontDate.toDate().toStringYearMonth();
  }

  getTotalRecords(isQuery: boolean = true) {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;

      this.convertData();
      this.spinnerService.show();
      this._services.getTotalRecords(this.param).subscribe({
        next: (result) => {
          this.spinnerService.hide();
          this.totalRows = result.data;
          if (isQuery)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        }
      });
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }

  getFactories() {
    this._services.getFactories().subscribe({
      next: (result) => this.factories = result
    });
  }

  getLevels() {
    this._services.getLevels().subscribe({
      next: (result) => this.levels = result
    });
  }

  getPermistionGroups() {
    this.permissionGroups = [];
    this._services.getPermistionGroups(this.param.factory).subscribe({
      next: (result) => {
        this.permissionGroups = result
        this.selectAllForDropdownItems(this.permissionGroups)
      }
    });
  }


  /**
   * Chọn tất cả danh sách Items
   * @param {KeyValuePair[]} items : Danh sách Permission Groups
   */
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }

  getWorkTypeJobs() {
    this._services.getWorkTypeJobs().subscribe({
      next: (result) => {
        this.workTypeJobs = result
        this.selectAllForDropdownItems(this.workTypeJobs)
      }
    });
  }

  checkRequiredParams(): boolean {
    var result = (!this.functionUtility.checkEmpty(this.param.factory))
    return result;
  }

  clear() {
    this.yearMontDate = null;

    this.param = <MonthlyEmployeeStatusChangesSheet_ByWorkTypeJobParam>{
      permisionGroup: [],
      work_Type: []
    };
    this.totalRows = 0;
  }

  deleteProperty = (name: string) => delete this.param[name]

  //#endregion

  //#region Events
  onFactoryChange() {
    this.param.permisionGroup = [];
    this.getPermistionGroups();
  }

  onExport() {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;
      this.convertData();
      this.spinnerService.show();
      this._services.exportExcel(this.param).subscribe({
        next: result => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
            this.functionUtility.exportExcel(result.data.result, fileName);
            this.totalRows = result.data.count;
          }
          else {
            this.totalRows = 0;
            this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          }
        }
      })
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }
  //#endregion

}
