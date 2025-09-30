import { Component, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { ValidateResult } from '@models/base-source';
import { HRMS_Basic_Code } from '@models/basic-maintenance/2_1_4_code-maintenance';
import { S_2_1_4_CodeMaintenanceService } from '@services/basic-maintenance/s_2_1_4-code-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase {

  //#region Data
  typeSeqs: KeyValuePair[] = [];
  codeMaintenance: HRMS_Basic_Code = <HRMS_Basic_Code>{
    isActive: true
  };

  date1: Date = null;
  date2: Date = null;
  date3: Date = null;

  //#endregion

  //#region Vaiables
  title: string = '';
  url: string = '';
  formType: string = '';
  iconButton = IconButton;
  //#endregion

  constructor(
    private codeMaintenanceServices: S_2_1_4_CodeMaintenanceService,
  ) {
    super()
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res['title']
      this.typeSeqs = res.resolverTypeSeqs
    });

    if (this.typeSeqs.length == 0)
      this.getTypeSeqs();
  }

  //#region Methods
  getDataFromSource() {
    effect(() => {
      if (this.formType != 'Add') {
        let source = this.codeMaintenanceServices.basicCodeSource()?.model;
        if (!source || source == null || this.functionUtility.checkEmpty(source.type_Seq))
          this.back();
        this.codeMaintenance = structuredClone(source);
        if (!this.functionUtility.checkEmpty(this.codeMaintenance.date1))
          this.date1 = new Date(this.codeMaintenance.date1);
        if (!this.functionUtility.checkEmpty(this.codeMaintenance.date2))
          this.date2 = new Date(this.codeMaintenance.date2);
        if (!this.functionUtility.checkEmpty(this.codeMaintenance.date3))
          this.date3 = new Date(this.codeMaintenance.date3);
      }
    })
  }

  cancel = () => this.back();
  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.codeMaintenance[name]
  getTypeSeqs() {
    this.codeMaintenanceServices.getTypeSeqs().subscribe({
      next: result => this.typeSeqs = result
    })
  }
  //#endregion

  //#region SAVECHANGE
  validate(): ValidateResult {
    if (this.functionUtility.checkEmpty(this.codeMaintenance.type_Seq))
      return new ValidateResult(`Please input ${this.translateService.instant('BasicMaintenance.CodeMaintain.TypeSeq')}`);
    if (this.functionUtility.checkEmpty(this.codeMaintenance.code))
      return new ValidateResult(`Please input ${this.translateService.instant('BasicMaintenance.CodeMaintain.Code')}`);
    if (this.date1 != null && this.date1 == undefined)
      return new ValidateResult(`${this.translateService.instant('BasicMaintenance.CodeMaintain.Date1')} invalid`);
    if (this.date2 != null && this.date2 == undefined)
      return new ValidateResult(`${this.translateService.instant('BasicMaintenance.CodeMaintain.Date2')} invalid`);
    if (this.date3 != null && this.date3 == undefined)
      return new ValidateResult(`${this.translateService.instant('BasicMaintenance.CodeMaintain.Date3')} invalid`);
    return { isSuccess: true };
  }

  save() {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      // Gán lại data
      this.codeMaintenance.date1 = this.date1?.isValidDate() ? this.date1.toDate().toBeginDate().toUTCDate().toJSON() : null;
      this.codeMaintenance.date2 = this.date2?.isValidDate() ? this.date2.toDate().toBeginDate().toUTCDate().toJSON() : null;
      this.codeMaintenance.date3 = this.date3?.isValidDate() ? this.date3.toDate().toBeginDate().toUTCDate().toJSON() : null;

      if (this.functionUtility.checkEmpty(this.codeMaintenance.int1)) this.codeMaintenance.int1 = null;
      if (this.functionUtility.checkEmpty(this.codeMaintenance.int2)) this.codeMaintenance.int2 = null;
      if (this.functionUtility.checkEmpty(this.codeMaintenance.int3)) this.codeMaintenance.int3 = null;

      if (this.functionUtility.checkEmpty(this.codeMaintenance.decimal1)) this.codeMaintenance.decimal1 = null;
      if (this.functionUtility.checkEmpty(this.codeMaintenance.decimal2)) this.codeMaintenance.decimal2 = null;
      if (this.functionUtility.checkEmpty(this.codeMaintenance.decimal3)) this.codeMaintenance.decimal3 = null;

      this.spinnerService.show();
      this.codeMaintenanceServices[this.formType == 'Add' ? 'create' : 'update'](this.codeMaintenance).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess,
            result.isSuccess ? `System.Message.${this.formType == 'Add' ? 'CreateOKMsg' : 'UpdateOKMsg'}` : result.error,
            result.isSuccess)

          if (result.isSuccess) this.back()
        }
      })
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }
  //#endregion

  //#region Events

  /**
   * Kiểm tra giá trị max của int
   * Max INT : 2,147,483,647
   * @param {*} value : Giá trị khi bấm
   * @param {number} int : đối tượng được bấm Int1, Int2, Int3
   * @memberof FormComponent
   */
  onNumberKeyup(value: any, int: number) {
    let number = value.target.value;
    if (number.toString().split('').length > 10) {
      let nums = number.toString().split('').slice(0, 9);
      if (int == 1) this.codeMaintenance.int1 = nums;
      if (int == 2) this.codeMaintenance.int2 = nums;
      if (int == 3) this.codeMaintenance.int3 = nums;
    }

    if (number > 2147483647) {
      if (int == 1) this.codeMaintenance.int1 = 2147483647;
      if (int == 2) this.codeMaintenance.int2 = 2147483647;
      if (int == 3) this.codeMaintenance.int3 = 2147483647;
    }
  }
  //#endregion

}
