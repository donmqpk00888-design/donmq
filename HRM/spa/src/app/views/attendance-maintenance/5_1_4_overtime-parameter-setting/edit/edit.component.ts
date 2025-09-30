import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Overtime_ParameterDTO } from '@models/attendance-maintenance/5_1_4_overtime-parameter-setting';
import { S_5_1_4_OvertimeParameterSettingService } from '@services/attendance-maintenance/s_5_1_4_overtime-parameter-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html',
  styleUrls: ['./edit.component.css']
})
export class EditComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  data: HRMS_Att_Overtime_ParameterDTO = <HRMS_Att_Overtime_ParameterDTO>{};
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  iconButton = IconButton;
  constructor(
    private service: S_5_1_4_OvertimeParameterSettingService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListWorkShiftType();
    });
    this.getDataFromSource()
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSource();
      if (source.data != null) {
        this.data = structuredClone(source.data);
        this.data.overtime_Start_Old = this.data.overtime_Start
      }
      else this.back();
      this.getListDivision();
      this.getListFactory();
      this.getListWorkShiftType();
    })
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
  }

  //#region
  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: (res) => {
        this.listWorkShiftType = res;
      },
    });
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res
      },
    });
  }

  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  //#endregion

  onFactoryChange() {
    this.listFactory = []
    this.deleteProperty('factory')
    this.getListFactory()
  }

  deleteProperty(name: string) {
    delete this.data[name]
  }
  editData() {
    this.data.update_By = JSON.parse((localStorage.getItem(LocalStorageConstants.USER))).account
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
  save() {
    this.spinnerService.show();
    this.service.update(this.data).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          this.back();
        }
        else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      }
    })
  }
  back = () => this.router.navigate([this.url]);
}
