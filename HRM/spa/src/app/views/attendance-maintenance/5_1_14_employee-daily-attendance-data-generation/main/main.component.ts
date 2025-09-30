import { Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { EnabledDateConfig, HRMS_Att_Swipe_Card_Excute_Param } from '@models/attendance-maintenance/5_1_14_employee-daily-attendance-data-generation';
import { ValidateResult } from '@models/base-source';
import { S_5_1_14_EmployeeDailyAttendanceDataGenerationService } from '@services/attendance-maintenance/s_5_1_14_employee-daily-attendance-data-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, DatepickerDateCustomClasses } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  //#region Configs
  enabledDatesHolidays = []; // Danh sách ngày nghỉ được phép hiển thị
  enabledDatesNationalHolidays = [];// Danh sách ngày nghỉ lề được phép hiển thị

  holidayCustomClass: DatepickerDateCustomClasses[] = []; // Cấu hình danh sách ngày nghỉ
  nationalHolidayCustomClass: DatepickerDateCustomClasses[] = []; // Cấu hình danh sách ngày nghỉ lễ

  //#endregion

  //#region Data
  factories: KeyValuePair[] = [];
  bsDatepickerConfig: BsDatepickerConfig = <BsDatepickerConfig>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  //#endregion

  //#region object
  param: HRMS_Att_Swipe_Card_Excute_Param = <HRMS_Att_Swipe_Card_Excute_Param>{
    factory: '',
    clockOffDay: '',
    workOnDay: '',
    holiday: '',
    nationalHoliday: '',
  };
  //#endregion

  //#region Vaiables
  title: string = '';
  isFirstTime: boolean = true;
  workOnDayDate: Date = new Date();
  offWorkDate: Date = null;
  holidayDate: Date = null;
  nationalHolidayDate: Date = null;
  processedRecords: number = null;
  iconButton = IconButton;
  //#endregion

  constructor(
    private _services: S_5_1_14_EmployeeDailyAttendanceDataGenerationService
  ) {
    super();
    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        // this.getFactories();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    // load dữ liệu [Factories]
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (res) => (this.factories = res.resolverFactories)
    );
    this.workOnDayDate = new Date();
  }

  //#region Methods
  getFactories() {
    this._services.getFactories().subscribe({
      next: (result) => {
        this.factories = result;
      }
    });
  }


  /**
   * Trả về danh sách ngày nghỉ | ngày nghỉ lễ hợp lệ
   * @param {string[]} dates
   * @memberof MainComponent
   */
  generateEnableDate(dates: string[], isHolidate: boolean = true): EnabledDateConfig {
    let result = <EnabledDateConfig>{
      dates: [],
      configs: [],
      nearestDate: null,
    }
    if (dates.length == 0) return result;
    dates.forEach(date => {
      const newDate = new Date(date);
      result.dates.push(newDate);
      result.configs.push({
        date: newDate,
        classes: [isHolidate ? 'bg-danger' : 'bg-warning', 'text-white']
      },
      );
    });

    result.dates.sort((a, b) => {
      if (a.getFullYear() !== b.getFullYear()) return a.getFullYear() - b.getFullYear();
      if (a.getMonth() !== b.getMonth()) return a.getMonth() - b.getMonth();
      return a.getDay() - b.getDay();
    });
    result.nearestDate = result.dates[0]
    return result;
  }

  convertToMonthDay(value: Date) {
    let month = (value.getMonth() + 1).toStringLeadingZeros(2);
    let day = value.getDate().toStringLeadingZeros(2);
    return `${month}${day}`;
  }

  resetParam() {
    // Reset toàn bộ dữ liệu
    this.processedRecords = null;
    // Reset param
    this.offWorkDate = null;
    this.param.clockOffDay = '';
    this.param.holiday = '';
    this.param.nationalHoliday = '';
  }

  getHolidays() {
    this.spinnerService.show();
    let validate = this.validateParam();
    if (!validate.isSuccess)
      return this.snotifyService.error(validate.message, this.translateService.instant('System.Caption.Error'))
    let workDay = this.workOnDayDate.toDateString();
    let offWork = this.offWorkDate.toDateString();
    this._services.getHolidays(this.param.factory, offWork, workDay).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        let model = this.generateEnableDate(result.data);
        this.enabledDatesHolidays = model.dates;
        this.holidayCustomClass = model.configs;
        this.holidayDate = model.nearestDate;
      },
    });
  }

  getNationalHolidays() {
    this.spinnerService.show();
    let workDay = this.workOnDayDate.toDateString();
    let offWork = this.offWorkDate.toDateString();

    this._services.getNationalHolidays(this.param.factory, offWork, workDay).subscribe({
      next: (result) => {
        this.spinnerService.hide();

        let model = this.generateEnableDate(result.data, false);
        this.enabledDatesNationalHolidays = model.dates;
        this.nationalHolidayCustomClass = model.configs;
        this.nationalHolidayDate = model.nearestDate;
      },
    });
  }

  checkHolidayAndNationalHolidays() {

    if (
      !this.functionUtility.checkEmpty(this.workOnDayDate) &&
      !this.functionUtility.checkEmpty(this.offWorkDate) &&
      this.workOnDayDate != null &&
      this.offWorkDate != null
    ) {
      if (this.workOnDayDate.toString() == "Invalid Date" || this.offWorkDate.toString() == "Invalid Date") {
        this.nationalHolidayDate = null;
        this.holidayDate = null;
        return this.snotifyService.error("Invalid Date", this.translateService.instant('System.Caption.Error'));
      } else {
        // Check workOnDay & WorkOff has value
        this.getHolidays();
        // Get Holiday & National Holiday
        this.getNationalHolidays();
      }

    }
  }

  validateParam(): ValidateResult {
    if (this.functionUtility.checkEmpty(this.param.factory))
      return new ValidateResult('Please choose Factory');
    if (this.functionUtility.checkEmpty(this.workOnDayDate))
      return new ValidateResult('Please choose workOnDay');
    if (this.functionUtility.checkEmpty(this.offWorkDate))
      return new ValidateResult('Please choose offWorkDate');
    return { isSuccess: true };
  }
  //#endregion

  //#region SAVECHANGE
  validateExcute() {
    return (
      this.functionUtility.checkEmpty(this.param.factory) ||
      this.functionUtility.checkEmpty(this.workOnDayDate) ||
      this.functionUtility.checkEmpty(this.offWorkDate) ||
      this.workOnDayDate.toString() == "Invalid Date" ||
      this.offWorkDate.toString() == "Invalid Date");
  }

  excuteConfirm() {
    let validate = this.validateParam();
    if (validate.isSuccess) {
      this.functionUtility.snotifyConfirm(
        'AttendanceMaintenance.SwipeCardDataUpload.ConfirmExecution',
        'System.Action.Confirm',
        true,
        () => {
          // Check
          // ConvertData
          let card_Date = this.convertToMonthDay(this.workOnDayDate); //MMDD
          this._services
            .checkClockInDateInCurrentDate(this.param.factory, card_Date)
            .subscribe({
              next: (result) => {
                if (!result.isSuccess) {
                  // Thông báo lỗi
                  this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
                } else {
                  this.param.clockOffDay = this.offWorkDate.toDate().toStringDateTime();
                  this.param.workOnDay = !this.functionUtility.checkEmpty(this.workOnDayDate) ? this.workOnDayDate.toDate().toStringDateTime() : '';
                  this.param.clockOffDay = !this.functionUtility.checkEmpty(this.offWorkDate) ? this.offWorkDate.toDate().toStringDateTime() : '';
                  this.param.holiday = !this.functionUtility.checkEmpty(this.holidayDate) ? this.holidayDate.toDate().toStringDateTime() : '';
                  this.param.nationalHoliday = !this.functionUtility.checkEmpty(this.nationalHolidayDate) ? this.nationalHolidayDate.toDate().toStringDateTime() : '';
                  this.spinnerService.show();
                  this._services.excute(this.param).subscribe({
                    next: (result) => {
                      this.spinnerService.hide();
                      this.processedRecords = result.data ?? null;
                      this.functionUtility.snotifySuccessError(
                        result.isSuccess,
                        result.isSuccess
                          ? this.translateService.instant('System.Message.ExecuteOKMsg')
                          : this.functionUtility.checkEmpty(result.error)
                            ? this.translateService.instant('System.Message.UnknowError')
                            : result.error
                      )
                    }
                  });
                }
              }
            });
        }
      );
    } else
      this.snotifyService.warning(
        validate.message,
        this.translateService.instant('System.Caption.Warning')
      );
  }
  //#endregion

  //#region Events
  onFactoryChange() {
    this.checkHolidayAndNationalHolidays();
  }

  onGetHolidateAndNationalHolidays() {
    this.param.holiday = '';
    this.param.nationalHoliday = ''
    this.checkHolidayAndNationalHolidays();
  }

  //#endregion
}
