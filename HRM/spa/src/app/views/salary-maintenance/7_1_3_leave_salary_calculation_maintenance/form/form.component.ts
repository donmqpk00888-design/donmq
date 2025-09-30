import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LeaveSalaryCalculationMaintenanceDTO } from '@models/salary-maintenance/7_1_3_leave_salary_calculation_maintenance';
import { S_7_1_3_Leave_Salary_Calculation_MaintenanceService } from '@services/salary-maintenance/s_7_1_3_leave_salary_calculation_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase implements OnInit {
  listLeaveCode: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  data: LeaveSalaryCalculationMaintenanceDTO = <LeaveSalaryCalculationMaintenanceDTO>{};
  isEdit: boolean = false;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  classButton = ClassButton;
  isDuplicate: boolean = true;
  isAllow: boolean = false;
  constructor(
    private service: S_7_1_3_Leave_Salary_Calculation_MaintenanceService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res.title
      this.getSource()
    });
  }
  getSource() {
    this.isEdit = this.formType == 'Edit';
    if (this.isEdit) {
      let source = this.service.paramSource();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      }
      else this.back();
    }
    this.getDropDownList()
  }
  getDropDownList() {
    this.getListFactory()
    this.getListLeaveCode()
  }
  getListLeaveCode() {
    this.service.getListLeaveCode().subscribe({
      next: (res) => {
        this.listLeaveCode = res
      },
    });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  back = () => this.router.navigate([this.tempUrl]);

  deleteProperty = (name: string) => delete this.data[name]

  save(isNext: boolean) {
    const observable = this.isEdit ? this.service.update(this.data) : this.service.create(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          isNext ? this.data = <LeaveSalaryCalculationMaintenanceDTO>{ factory: this.data.factory } : this.back();
        } else {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        }
      },

    });
  }

  onValueChange() {
    this.data.update_By = this.updateBy
    this.data.update_Time = new Date().toStringDateTime()
  }
  convertNumber(value: number) {
    this.data.salary_Rate = Math.max(0, Math.min(value, 100));
    this.onValueChange()
  }
}
