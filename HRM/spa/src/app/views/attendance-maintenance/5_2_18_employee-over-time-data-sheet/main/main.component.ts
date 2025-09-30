import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { EmployeeOvertimeDataSheetParam } from '@models/attendance-maintenance/5_2_18_Employee-overtime-data-sheet';
import { S_5_2_18_EmployeeOvertimeDataSheetService } from '@services/attendance-maintenance/s_5_2_18_employee-overtime-data-sheet.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ValidateResult } from '@models/base-source';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  totalRows: number = 0;
  // on_Date:  Date | null = null;
  dateStart: Date = null;
  dateEnd: Date = null;
  //#region Objects
  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  param: EmployeeOvertimeDataSheetParam = <EmployeeOvertimeDataSheetParam>{};
  //#endregion

  constructor(private _reportService: S_5_2_18_EmployeeOvertimeDataSheetService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this._reportService.baseInitSource();

      // Nếu có param load lại dropdown
      if (!this.functionUtility.checkEmpty(this.param.factory))
        this.getListDepartment();

      // Load lại dữ liệu ngày
      if (!this.functionUtility.checkEmpty(this.param.overtime_Date_Start)) this.dateStart = new Date(this.param.overtime_Date_Start);
      if (!this.functionUtility.checkEmpty(this.param.overtime_Date_End)) this.dateEnd = new Date(this.param.overtime_Date_End);

      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getPagination(false);
      }
      else this.clear()
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();

      if (!this.functionUtility.checkEmpty(this.param.factory))
        this.getListDepartment();
    });
  }

  checkRequiredParams(): boolean {
    var result = (!this.functionUtility.checkEmpty(this.param.factory))
    return result;
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.loadDropDownList();
  }


  ngOnDestroy(): void {
    this._reportService.setSource(this.param);
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
  }

  onChangeFactory() {
    this.deleteProperty('department')
    this.getListDepartment();
  }

  getListFactory() {
    this._reportService.getFactories().subscribe({
      next: (res) => this.listFactory = res
    });
  }

  disableSearch() {
    return (this.functionUtility.checkEmpty(this.param.factory) || this.dateStart == null ||
      this.dateStart == undefined || this.dateEnd == null ||
      this.dateEnd == undefined)
  }

  getListDepartment() {
    this._reportService.getListDepartment(this.param.factory).subscribe({
      next: (res) => this.listDepartment = res
    })
  }

  getPagination(isQuery: boolean = true) {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;

      // Convert Data
      this.param.overtime_Date_Start = this.functionUtility.getDateFormat(new Date(this.dateStart));
      this.param.overtime_Date_End = this.functionUtility.getDateFormat(new Date(this.dateEnd));

      this.spinnerService.show();
      this._reportService.getPagination(this.param).subscribe({
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

  validate(): ValidateResult {
    if (this.dateStart != null && (this.dateStart == undefined || this.dateStart.toString() == "Invalid Date"))
      return new ValidateResult(`Date of Resignation invalid`);
    if (this.dateEnd != null && (this.dateEnd == undefined || this.dateEnd.toString() == "Invalid Date"))
      return new ValidateResult(`Date of Resignation invalid`);
    return { isSuccess: true };
  }

  clear() {
    this.param = <EmployeeOvertimeDataSheetParam>{};
    this.dateStart = null;
    this.dateEnd = null;
    this.deleteProperty('factory')
    this.deleteProperty('employeeID')
    this.deleteProperty('department')
    this.totalRows = 0;
  }

  deleteProperty = (name: string) => delete this.param[name]
  onExport() {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;
      this.param.overtime_Date_Start = this.functionUtility.getDateFormat(new Date(this.dateStart));
      this.param.overtime_Date_End = this.functionUtility.getDateFormat(new Date(this.dateEnd));
      this.spinnerService.show();
      this._reportService.export(this.param).subscribe({
        next: result => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
            this.functionUtility.exportExcel(result.data, fileName);
          }
          else this.functionUtility.snotifySuccessError(result.isSuccess, result.error, false)
        }
      })
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }
}



