import { Component, OnInit } from '@angular/core';
import { IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { Sal_Dept_SAPCostCenter_MappingDTO, Sal_Dept_SAPCostCenter_MappingParam } from '@models/salary-maintenance/7_1_9_sal_dept_sapcostcenter_mapping';
import { S_7_1_9_departmentToSapCostCenterMappingService } from '@services/salary-maintenance/s_7_1_9_department-to-sap-cost-center-mapping.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  placeholder = Placeholder;
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER))
  data: Sal_Dept_SAPCostCenter_MappingDTO = <Sal_Dept_SAPCostCenter_MappingDTO>{};
  listFactory: KeyValuePair[] = []
  listDepartment: KeyValuePair[] = []
  listCostCenter: KeyValuePair[] = []
  title: string = '';
  formType: string = '';
  tempUrl: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY',
    minMode: 'year',
  };
  isDuplicate: boolean
  constructor(private service: S_7_1_9_departmentToSapCostCenterMappingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res.title;
      this.getSource()
    });
  }
  getSource() {
    if (this.formType == 'Edit') {
      let source = this.service.programSource();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
        this.data.department_Old = source.selectedData.department;
      } else this.back()
    }
    this.getDropDownList()
  }
  getDropDownList() {
    this.getListFactory();
    this.getListDepartment()
    this.getListCostCenter()
  }

  saveAndContinue() {
    this.spinnerService.show();
    this.service.create(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
          this.data = <Sal_Dept_SAPCostCenter_MappingDTO>{ factory: this.data.factory }
        }
        else
          this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
      }
    })
  }
  save() {
    this.spinnerService.show();
    if (this.formType != 'Edit') {
      this.service.create(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
    else {
      this.data.department_New = this.data.department
      this.service.update(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
  }
  onSelectFactory() {
    this.deleteProperty('department')
    this.deleteProperty('cost_Code')
    this.getListDepartment()
    this.getListCostCenter()
    this.onValueChange()
  }

  onDateChange() {
    this.deleteProperty('cost_Code')
    this.data.cost_Year_Str = this.functionUtility.isValidDate(new Date(this.data.cost_Year)) ? new Date(this.data.cost_Year).getFullYear().toString() : '';
    this.getListCostCenter()
    this.onValueChange()
    this.checkDuplicate()
  }
  checkDuplicate() {
    if (this.data.factory && this.data.cost_Year_Str && this.data.department){
      this.service.checkDuplicate(this.data.factory, this.data.cost_Year_Str, this.data.department).subscribe({
        next: (res) => {
          this.isDuplicate = res
          this.snotifyService.clear()
          if (res)
            this.snotifyService.error(
              this.translateService.instant('SalaryMaintenance.DepartmentToSAPCostCenterMapping.Duplicate'),
              this.translateService.instant('System.Caption.Error')
            )
        }
      });
    }
    this.onValueChange()
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getListCostCenter() {
    if (!this.functionUtility.checkEmpty(this.data.factory) && !this.functionUtility.checkEmpty(this.data.cost_Year_Str)) {
      const param = <Sal_Dept_SAPCostCenter_MappingParam>{
        factory: this.data.factory,
        year_Str: this.data.cost_Year_Str,
      }
      this.service.getListCostCenter(param).subscribe({
        next: (res: KeyValuePair[]) => this.listCostCenter = res
      });
    }
  }
  getListDepartment() {
    if (!this.functionUtility.checkEmpty(this.data.factory)) {
      this.service.getListDepartment(this.data.factory).subscribe({
        next: (res: KeyValuePair[]) => this.listDepartment = res
      });
    }
  }
  back = () => this.router.navigate([this.tempUrl]);
  deleteProperty = (name: string) => delete this.data[name]
  onValueChange() {
    this.data.update_By = this.user.id
    this.data.update_Time = new Date().toStringDateTime()
  }
}
