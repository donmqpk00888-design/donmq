import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ValidateResult } from '@models/base-source';
import { S_5_2_14_MonthlyEmployeeStatusChangesSheetByReasonForResignationService } from '@services/attendance-maintenance/s_5_2_14_monthly-employee-status-changes-sheet-by-reason-for-resignation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import {
  MonthlyEmployeeStatusChangesSheet_ByReasonForResignationParam,
  MonthlyEmployeeStatusChangesSheet_ByReasonForResignationValue
} from '@models/attendance-maintenance/5_2_14_monthly-employee-status-changes-sheet-by-reason-for-resignation';
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
  yearMonthDate: Date | null = null;
  //#endregion

  //#region Objects
  iconButton = IconButton;
  classButton = ClassButton;

  param: MonthlyEmployeeStatusChangesSheet_ByReasonForResignationParam = <MonthlyEmployeeStatusChangesSheet_ByReasonForResignationParam>{
    permisionGroups: []
  };
  //#endregion

  //#region Arrays
  factories: KeyValuePair[] = [];
  levels: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  workTypeJobs: KeyValuePair[] = [];

  table: MonthlyEmployeeStatusChangesSheet_ByReasonForResignationValue[] = []
  //#endregion

  constructor(private _services: S_5_2_14_MonthlyEmployeeStatusChangesSheetByReasonForResignationService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this._services.baseInitSource();

      if (!this.functionUtility.checkEmpty(this.param.yearMonth)) {
        let datetime = this.param.yearMonth.split('/');
        this.yearMonthDate = new Date(+datetime[0], +datetime[1] - 1)
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
      this.yearMonthDate == null ||
      this.yearMonthDate == undefined ||
      this.param.permisionGroups.length == 0
    )
  }

  loadDropDownList() {
    this.getFactories();
  }

  joinArray = (arr: string[]) => arr.join(',')
  stringToArray = (value: string) => value.split(',')

  validate(): ValidateResult {
    if (this.yearMonthDate != null && (this.yearMonthDate == undefined || this.yearMonthDate.toString() == "Invalid Date"))
      return new ValidateResult(`Year Month invalid`);

    return { isSuccess: true };
  }


  convertData() {
    this.param.yearMonth = this.yearMonthDate.toDate().toStringYearMonth();
  }

  getTotalRecords(isQuery: boolean = true) {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;

      // Convert Data
      this.convertData();
      this.spinnerService.show();
      this._services.getTotalRecords(this.param).subscribe({
        next: (result) => {
          this.spinnerService.hide();
          this.totalRows = result.totalRecords;
          this.table = result.data;
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

  checkRequiredParams(): boolean {
    var result = (!this.functionUtility.checkEmpty(this.param.factory))
    return result;
  }

  clear() {
    this.yearMonthDate = null;

    this.param = <MonthlyEmployeeStatusChangesSheet_ByReasonForResignationParam>{
      permisionGroups: []
    };
    this.totalRows = 0;
    this.table = [];
  }

  deleteProperty = (name: string) => delete this.param[name]

  //#endregion

  //#region Events
  onFactoryChange() {
    this.permissionGroups = [];
    this.getPermistionGroups();
  }
  onClearFactory() {
    this.param.permisionGroups = [];
    this.deleteProperty('factory')
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
          if (result.isSuccess){
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
            this.functionUtility.exportExcel(result.data, this.functionUtility.getFileName(fileName));
          }
          else this.functionUtility.snotifySuccessError(result.isSuccess, result.error, false)
        }
      })
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }
  //#endregion

}
