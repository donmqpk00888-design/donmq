import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Basic_Level } from '@models/basic-maintenance/2_1_6_grade-maintenance';
import { S_2_1_6_GradeMaintenanceService } from '@services/basic-maintenance/s_2_1_6_grade-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss'],
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  model: HRMS_Basic_Level = <HRMS_Basic_Level>{};
  listLevelCode: KeyValuePair[] = [];
  isFlat: boolean = false;
  types: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  formType: string = ''

  constructor(private _service: S_2_1_6_GradeMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getLevelCodesForm();
      this.getTypes();
    });
    effect(() => {
      this.model = this._service.data();
      if (this.formType != 'Add' && Object.keys(this.model).length === 0) this.back();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res['title']
    })
    this.getLevelCodesForm();
    this.getTypes();
  }

  getTypes() {
    this._service.getTypes().subscribe({
      next: res => {
        this.types = res;
      }
    })
  }

  getLevelCodesForm() {
    this._service.getListLevelCode('add').subscribe({
      next: res => {
        this.listLevelCode = res;
      }
    })
  }

  returnMessage(isSuccess: boolean, type: string, errorMsg: string = null) {
    return isSuccess
      ? `System.Message.${type === 'Add' ? 'CreateOKMsg' : 'UpdateOKMsg'}`
      : (errorMsg === null ? `System.Message.${type === 'Add' ? 'CreateErrorMsg' : 'UpdateOKMsg'}` : errorMsg)
  }

  save() {
    if (this.formType === 'Add')
      this.model.isActive = true;

    this.spinnerService.show();
    this._service[this.formType.toLowerCase()](this.model).subscribe({
      next: (res: OperationResult) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess,
          this.returnMessage(res.isSuccess, this.formType, res.error),
          res.isSuccess || !res.isSuccess && res.error === null
        )
        if (res.isSuccess) this.back();
      }
    })
  }

  isDisableSave() {
    const result = !this.model?.type_Code || !this.model?.level_Code || this.model.level == null || this.model.level > 15
    return result;
  }

  changeLevel(e: number) {
    if (e < 0 || e > 15) this.isFlat = true;
    else this.isFlat = false;
  }

  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.model[name]

}
