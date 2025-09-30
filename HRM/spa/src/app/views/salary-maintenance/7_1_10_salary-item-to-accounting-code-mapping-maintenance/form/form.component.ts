import { Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { SalaryItemToAccountingCodeMappingMaintenanceDto } from '@models/salary-maintenance/7_1_10_salary-item-to-accounting-code-mapping-maintenance';
import { S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService } from '@services/salary-maintenance/s_7_1_10_salary-item-to-accounting-code-mapping-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  tempUrl: string = '';
  formType: string = "";
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER))

  iconButton = IconButton;
  factorys: KeyValuePair[] = [];
  salaryItems: KeyValuePair[] = [];
  model: SalaryItemToAccountingCodeMappingMaintenanceDto = <SalaryItemToAccountingCodeMappingMaintenanceDto>{ dC_Code: "D" };
  isValidCode: boolean = true;

  constructor(private service: S_7_1_10_SalaryItemToAccountingCodeMappingMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.formType = role.title
        this.getSource()
      })
  }
  getSource() {
    if (this.formType == 'Edit') {
      let source = this.service.signalDataMain();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.model = structuredClone(source.selectedData);
      } else this.back()
    }
    this.getDropDownList()
  }
  getDropDownList() {
    this.getFactory();
    this.getSalaryItems();
  }
  getSalaryItems() {
    this.commonService.getListSalaryItems().subscribe({
      next: res => {
        this.salaryItems = res;
      }
    })
  }

  getFactory() {
    this.service.getFactory().subscribe({
      next: res => {
        this.factorys = res;
      }
    })
  }

  back = () => this.router.navigate([this.tempUrl]);

  save(isNext?: boolean) {
    this.spinnerService.show();
    const actionMethod = this.service[this.formType == "Add" ? "create" : "edit"](this.model);
    actionMethod.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.spinnerService.hide();
        if (this.formType === 'Add')
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.CreateOKMsg' : res.error ?? 'System.Message.CreateErrorMsg');
        else
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error ?? 'System.Message.UpdateErrorMsg');
        if (res.isSuccess)
          isNext ? this.model = <SalaryItemToAccountingCodeMappingMaintenanceDto>{ factory: this.model.factory, dC_Code: "D" } : this.back();
      }
    })
  }

  changeDC_Code() {
    this.model.dC_Code = this.model.dC_Code.toUpperCase();
    this.isValidCode = !(this.model.dC_Code != "D" && this.model.dC_Code != "C")
    this.snotifyService.clear()
    if (!this.isValidCode && !this.functionUtility.checkEmpty(this.model.dC_Code))
      this.functionUtility.snotifySuccessError(false, "Debit & Credit only D/C", false)
    this.checkDupplicate()
    this.onValueChange()
  }

  onFactoryChange() {
    this.checkDupplicate();
    this.onValueChange()
  }

  onSalaryItemChange() {
    this.checkDupplicate();
    this.onValueChange()
  }

  checkDupplicate() {
    if (this.model.factory && this.model.salary_Item && this.model.dC_Code && this.isValidCode) {
      this.service.checkDupplicate(this.model.factory, this.model.salary_Item, this.model.dC_Code).subscribe({
        next: res => {
          if (res.isSuccess)
            this.functionUtility.snotifySuccessError(false, `Factory: ${this.model.factory},\r\nSalary_Item: ${this.model.salary_Item}, \r\nDC_Code: ${this.model.dC_Code} already exists!`, false);
        }
      })
    }
  }

  patternCheck(event: any): boolean {
    let patt: RegExp = new RegExp(/[0-9a-zA-Z]$/g);
    return patt.test(event.key);
  }
  onValueChange() {
    this.model.update_By = this.user.id
    this.model.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date())
  }
}
