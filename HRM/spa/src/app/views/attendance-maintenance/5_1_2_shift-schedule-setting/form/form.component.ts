import { Component, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Work_Shift } from '@models/attendance-maintenance/5_1_2_shift-schedule-setting';
import { S_5_1_2_ShiftScheduleSettingService } from '@services/attendance-maintenance/s_5_1_2_shift-schedule-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase {
  title: string = '';
  url: string = '';
  currentUser = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));

  //#region Data
  divisions: KeyValuePair[] = [];
  allFactories: KeyValuePair[] = [];
  factories: KeyValuePair[] = [];
  workShiftTypes: KeyValuePair[] = [];

  overnights: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];

  workShift: HRMS_Att_Work_Shift = <HRMS_Att_Work_Shift>{
    overnight: 'N',
    effective_State: true,
  }

  //#endregion

  //#region Vaiables
  formType: string = ''; // trạng thái [Thêm mới | Chỉnh sửa ]
  iconButton = IconButton;
  //#endregion

  constructor(private shiftScheduleSettingServices: S_5_1_2_ShiftScheduleSettingService,) {
    super();
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDivisions();
      if (!this.functionUtility.checkEmpty(this.workShift.division))
        this.getFactories();
      this.getWorkShiftTypes();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res['title']
      this.divisions = res.resolverDivisions
      this.workShiftTypes = res.resolverWorkShiftTypes
    });
  }

  //#region Methods

  getDataFromSource() {
    effect(() => {
      if (this.formType != 'Add') {
        let source = this.shiftScheduleSettingServices.workShiftSource()?.model;
        if (!source || source == null)
          this.back();
        this.workShift = structuredClone(source);
        this.getDivisions();
        if (!this.functionUtility.checkEmpty(this.workShift.division))
          this.getFactories();
        this.getWorkShiftTypes();
      }
    })
  }

  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();

  getDivisions() {
    this.shiftScheduleSettingServices.getDivisions().subscribe({
      next: result => this.divisions = result
    })
  }

  getWorkShiftTypes() {
    this.shiftScheduleSettingServices.getWorkShiftTypes().subscribe({
      next: result => this.workShiftTypes = result
    })
  }

  getFactories() {
    if (!this.functionUtility.checkEmpty(this.workShift.division)) {
      this.shiftScheduleSettingServices.getFactoriesByDivision(this.workShift.division).subscribe({
        next: result => {
          this.factories = result
        }
      })
    } else {
      this.factories = [];
    }
  }

  //#endregion

  //#region SAVECHANGE
  save(isSaveNext: boolean = false) {
    this.spinnerService.show();
    // Add
    if (this.workShift.overtime_ClockIn === '')
      this.workShift.overtime_ClockIn = null;
    if (this.workShift.overtime_ClockOut === '')
      this.workShift.overtime_ClockOut = null;
    if (this.formType == 'Add') {
      this.shiftScheduleSettingServices.create(this.workShift).subscribe({
        next: result => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            this.functionUtility.snotifySuccessError(result.isSuccess, 'System.Message.CreateOKMsg')
            if (!isSaveNext) this.back();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
    // Update
    else {
      this.shiftScheduleSettingServices.update(this.workShift).subscribe({
        next: result => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            this.functionUtility.snotifySuccessError(result.isSuccess, 'System.Message.UpdateOKMsg')
            if (!isSaveNext) this.back();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
  }
  //#endregion


  //#region Events
  editData() {
    this.workShift.update_By = this.currentUser.id
    this.workShift.update_Time = new Date
    this.workShift.update_Time_Str = this.functionUtility.getDateTimeFormat(this.workShift.update_Time)
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    this.getFactories();
    this.editData()
  }

  onCheckWeek(e: any) {
    const value = e.target.value.toString().split('.');
    if (+(value)[0] > 6)
      this.workShift.week = 6;
    else if (+(value)[0] < 0)
      this.workShift.week = 0;
    else if (value[0].split('').length > 0)
      this.workShift.week = value[0].split('')[0];
  }
  deleteProperty(name: string) {
    delete this.workShift[name]
  }
  //#endregion

  validateInput(event: KeyboardEvent) {
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight'];
    if (allowedKeys.indexOf(event.key) !== -1)
      return;
    if (event.code === 'Space' || event.key === ' ') {
      event.preventDefault();
      return;
    }
    if (!/^[0-9]$/.test(event.key))
      event.preventDefault();
  }
}
