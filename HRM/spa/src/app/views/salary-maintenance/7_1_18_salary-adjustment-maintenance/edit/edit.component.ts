import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { SalaryAdjustmentMaintenanceMain } from '@models/salary-maintenance/7_1_18_salary-adjustment-maintenance';
import { S_7_1_18_salaryAdjustmentMaintenanceService } from '@services/salary-maintenance/s_7_1_18_salary-adjustment-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { concat, Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html',
  styleUrls: ['./edit.component.scss']
})
export class EditComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;
  title: string = '';
  url: string = '';
  action: string = '';
  formType: string
  effectiveDate: Date = new Date();
  acting_Position_Start: Date = null;
  acting_Position_End: Date = null;
  dataTypeaHead: string[];
  listFactory: KeyValuePair[] = [];
  listReasonForChange: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listCurrency: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = []
  listPositionTitle: KeyValuePair[] = []
  listPermissionGroup: KeyValuePair[] = []
  listTechnicalType: KeyValuePair[] = []
  listExpertiseCategory: KeyValuePair[] = []
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Onjob' },
    { key: 'N', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Resigned' },
    { key: 'U', value: 'SalaryMaintenance.SalaryAdjustmentMaintenance.Unpaid' },
  ];
  data: SalaryAdjustmentMaintenanceMain = <SalaryAdjustmentMaintenanceMain>{};
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  isQuery: boolean = false

  constructor(private service: S_7_1_18_salaryAdjustmentMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListSalaryItem()
      this.getDropdownList();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
      this.getSource()
    });
  }
  getSource() {
    this.isQuery = this.action == 'Query'
    let source = this.service.programSource();
    if (source.selectedData && Object.keys(source.selectedData).length > 0) {
      this.data = structuredClone(source.selectedData)
      if (this.functionUtility.isValidDate(new Date(this.data.effective_Date_Str)))
        this.effectiveDate = new Date(this.data.effective_Date_Str);
      this.getListSalaryItem()
      this.getDropdownList()
    } else this.back()
  }
  getListSalaryItem() {
    this.service.getListSalaryItem(this.data.history_GUID).subscribe({
      next: (res) => {
        if (!this.functionUtility.isEmptyObject(res)) {
          this.data.salary_Item = res;
        } else {
          this.back();
        }
      }
    })
  }
  getDropdownList() {
    this.spinnerService.show()
    const observableList: Array<Observable<any>> = [
      this.callList('listFactory'),
      this.callList('listPositionTitle'),
      this.callList('listPermissionGroup'),
      this.callList('listTechnicalType'),
      this.callList('listExpertiseCategory'),
      this.callList('listReasonForChange'),
      this.callList('listSalaryType'),
      this.callList('listCurrency'),
      this.callList('listDepartment', this.data.factory)
    ]
    concat(...observableList).subscribe({
      error: () => { },
      complete: () => { this.spinnerService.hide() }
    })
  }
  callList(name: string, param: any = null): Observable<KeyValuePair[]> {
    return new Observable((observer: any) => {
      const _function: Observable<KeyValuePair[]> = param != null ? this.service['get' + name](param) : this.service['get' + name]()
      _function.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => observer.next(this[name] = res),
        error: () => observer.error(),
        complete: (() => observer.complete())
      });;
    })
  }

  save() {
    this.spinnerService.show()
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
  validateDecimal(event: KeyboardEvent, maxValue: number): boolean {
    const inputChar = event.key;
    const maxValueLength = maxValue.toString().length;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.includes(inputChar))
      return true;

    if (!/^\d$/.test(inputChar) && inputChar !== '.') {
      event.preventDefault();
      return false;
    }

    const input = event.target as HTMLInputElement;
    const currentValue = input.value;
    const newValue = currentValue.substring(0, input.selectionStart!) + inputChar + currentValue.substring(input.selectionEnd!);

    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (integerPartLength > maxValueLength ||
      (integerPartLength == maxValueLength && parts.length == 2 && parseInt(parts[0]) == maxValue) ||
      (integerPartLength === maxValueLength && parseInt(parts[0]) > maxValue) ||
      decimalPartLength > 1) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^(0|[1-9][0-9]?)(\.[0-9]{0,1})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    return true;
  }
  onDataChange() {
    this.data.update_By = this.user.id;
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date());
  }
  back = () => this.router.navigate([this.url]);
}
