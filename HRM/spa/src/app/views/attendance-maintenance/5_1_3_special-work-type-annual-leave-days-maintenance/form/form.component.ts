
import { Component, Input, OnInit, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Work_Type_DaysDto } from '@models/attendance-maintenance/5_1_3_special-work-type-annual-leave-days-maintenance';
import { S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService } from '@services/attendance-maintenance/s_5_1_3_special-work-type-annual-leave-days-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  action: string = '';
  userName: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  data: HRMS_Att_Work_Type_DaysDto = <HRMS_Att_Work_Type_DaysDto>{
    update_By: this.userName,
    effective_State: true,
    update_Time: this.functionUtility.getDateTimeFormat(new Date())
  };
  isEdit: boolean = false;

  constructor(
    private service: S_5_1_3_SpecialWorkTypeAnnualLeaveDaysMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListWorkType();
      this.getListDivision();
      if (this.data.division != '')
        this.getListFactory();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    const userInfo = JSON.parse(localStorage.getItem(LocalStorageConstants.USER))
    this.data.update_By = this.userName = userInfo.id;
    this.getListDivision();
    this.getListWorkType();
    if (this.data.division != '')
      this.getListFactory();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(x => {
      this.action = x.title;
      this.isEdit = x.title == "Edit"
    })
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSource();
      if (source && source.data != null) {
        source.data.update_Time = source.data.update_Time != null ? this.functionUtility.getDateTimeFormat(new Date(source.data.update_Time)) : this.functionUtility.getDateTimeFormat(new Date());
        source.data.update_By = this.userName;
        this.data = { ...source.data };
        this.getListFactory();
      }
      if (this.isEdit && source.data == null) {
        this.back();
      }
    })
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res
      }
    });
  }

  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res
      }
    });
  }

  getListWorkType() {
    this.service.getListWorkType().subscribe({
      next: (res) => {
        this.listWorkType = res;
      }
    });
  }

  onChangeDivision() {
    this.deleteProperty('factory')
    this.getListFactory();
  }

  save(type?: string) {
    let action
    if (this.isEdit) {
      this.data.work_Type = this.data.work_Type.split('-')[0]?.trim() ?? '';
      action = this.service.edit(this.data)
    } else
      action = this.service.addNew(this.data)
    this.spinnerService.show();
    action.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
          this.functionUtility.snotifySuccessError(true, message)
          if (!this.isEdit && type === 'next') {
            this.resetParams();
          } else {
            this.back();
          }
        } else {
          this.resetDateStringDefault();
          this.functionUtility.snotifySuccessError(false, result.error)
        }
      },
      error: (e: any) => {
        const errLeaveDay = e.errors?.Annual_leave_days[0];
        if (errLeaveDay)
          this.functionUtility.snotifySuccessError(false, `AttendanceMaintenance.SpecialWorkTypeAnnualLeaveDaysMaintenance.${errLeaveDay}`)
        this.resetDateStringDefault();
      }
    })
  }

  resetDateStringDefault() {
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date());
  }

  back = () => this.router.navigate([this.url]);

  resetParams() {
    this.data = <HRMS_Att_Work_Type_DaysDto>{
      update_By: this.userName,
      effective_State: true,
      update_Time: this.functionUtility.getDateTimeFormat(new Date())
    };
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
