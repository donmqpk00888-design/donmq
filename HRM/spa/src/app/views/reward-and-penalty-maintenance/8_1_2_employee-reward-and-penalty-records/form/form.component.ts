import { Component, OnInit } from '@angular/core';
import {
  ClassButton,
  IconButton,
  Placeholder,
} from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { EmployeeCommonInfo } from '@models/common';
import {
  D_8_1_2_EmployeeRewardPenaltyRecordsSubParam,
  EmployeeRewardAndPenaltyRecords_ModalInputModel,
} from '@models/reward-and-penalty-maintenance/8_1_2_employee-reward-and-penalty-records';
import { ModalService } from '@services/modal.service';
import { S_8_1_2_EmployeeRewardAndPenaltyRecordsService } from '@services/reward-and-penalty-maintenance/s_8_1_2_employee-reward-and-penalty-records.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { map, mergeMap, Observable, Observer, take, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss',
})
export class FormComponent extends InjectBase implements OnInit {
  employeeList$: Observable<EmployeeCommonInfo[]>;
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  action: any;

  listReward_Type: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listReason_Code: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];

  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  classButton = ClassButton;
  iconButton = IconButton;
  placeholder = Placeholder;
  inputError: boolean = false;
  data: D_8_1_2_EmployeeRewardPenaltyRecordsSubParam = <D_8_1_2_EmployeeRewardPenaltyRecordsSubParam>
  {
    counts_of: 1,
    file_List:[]
  };
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  pagination: Pagination = <Pagination>{};
  create_Date_Value: Date;
  constructor(
    private service: S_8_1_2_EmployeeRewardAndPenaltyRecordsService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
      this.getDropDownList()
      this.getListReasonCode();
      this.action === "Edit" || this.action === "Query"
        ? this.getSource()
        : this.employeeList$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((resp) => {
      if (resp.isSave) {
        this.data.file_List = resp.data.file_List
      }
    })
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res.title;
      this.action = res.title;
    });
    this.service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((history_GUID) => {
      if (this.action == 'Edit' || this.action == "Query") {
        if (!history_GUID)
          this.back()
        else {
          this.data.history_GUID = history_GUID
          this.getSource()
        }
      } else {
        this.data.update_By = this.user.id
        this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date)
      }
    })
    this.employeeList$ = new Observable((observer: Observer<any>) => {
      observer.next({
        factory: this.data.factory,
        employee_ID: this.data.employee_ID
      });
    }).pipe(
      mergeMap((_param: any) =>
        this.service.getEmployeeList(_param.factory, _param.employee_ID)
          .pipe(
            map((data: EmployeeCommonInfo[]) => data || []),
            tap(res => {
              if (res.length == 1 && _param.employee_ID == res[0].actual_Employee_ID) {
                this.setEmployeeInfo(res[0])
              } else {
                this.clearEmpInfo()
              }
            })
          )
      )
    );
    this.getDropDownList()
  }
  getDropDownList() {
    this.getListFactory();
    this.getListRewardType();
  }
  private setEmployeeInfo(emp: EmployeeCommonInfo) {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.division = emp.actual_Division;
      this.data.work_Type = emp.work_Type;
      this.data.work_Type_Name = emp.work_Type_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.department_Code_Name = emp.actual_Department_Code_Name;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }
  private clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('division')
    this.deleteProperty('work_Type')
    this.deleteProperty('work_Type_Name')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Code_Name')
  }
  save(isContinue: boolean) {
    this.spinnerService.show();
    const act = this.action == "Edit" ? this.service.putData(this.data) : this.service.postData(this.data);
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date)
    act.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: any) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          const message = this.action == 'Edit' ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
          this.functionUtility.snotifySuccessError(true, message)
          this.action == "Add" && isContinue
            ? this.resetParams()
            : this.back();
        } else {
          this.functionUtility.snotifySuccessError(false, result.error)
        }
      }
    })
  }

  getSource() {
    if (this.data.history_GUID) {
      this.spinnerService.show();
      this.service
        .getDetail(this.data.history_GUID, "language")
        .subscribe({
          next: (res) => {
            this.data = res;
            this.data.work_Type = this.data.work_Type_Name;
            this.getListReasonCode();
            this.spinnerService.hide();
          }
        });
    }
  }
  back = () => this.router.navigate([this.tempUrl]);

  onTypehead(e?: TypeaheadMatch): void {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, `System.Message.InvalidEmployeeIDLength`)
    this.data.useR_GUID = e.item.useR_GUID
    this.data.local_Full_Name = e.item.local_Full_Name
    this.data.division = e.item.actual_Division;
    this.data.work_Type = e.item.work_Type;
    this.data.work_Type_Name = e.item.work_Type_Name;
    this.data.department_Code = e.item.actual_Department_Code;
    this.data.department_Code_Name = e.item.actual_Department_Code_Name;
  }
  onEmployeeChange() {
    if (this.functionUtility.checkEmpty(this.data.employee_ID))
      this.clearEmpInfo()
  }

  attachment() {
    const data: EmployeeRewardAndPenaltyRecords_ModalInputModel = <EmployeeRewardAndPenaltyRecords_ModalInputModel>{
      division: this.data.division,
      factory: this.data.factory,
      serNum: this.data.serNum,
      employee_ID: this.data.employee_ID,
      file_List: this.data.file_List == undefined ? [] : this.data.file_List,
    }
    if (this.action == "Query")
      this.modalService.open(data, 'view-modal')
    else
      this.modalService.open(data, 'modify-modal')
  }
  deleteProperty = (name: string) => delete this.data[name]
  getListFactory() {
    this.service.GetListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }
  getListReasonCode() {
    if (this.data.factory)
      this.service.GetListReasonCode(this.data.factory).subscribe({
        next: res => {
          this.listReason_Code = res;
        }
      });
  }
  getListRewardType() {
    this.service.GetListRewardType().subscribe({
      next: (res) => {
        this.listReward_Type = res
      },
    });
  }
  onFactoryChange() {
    this.getListReasonCode()
    this.deleteProperty('reason_Code')
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }
  resetParams() {
    this.data = <D_8_1_2_EmployeeRewardPenaltyRecordsSubParam>{
      factory : this.data.factory,
      counts_of: 1,
      file_List:[],
      update_By: this.user.id,
      update_Time: this.functionUtility.getDateTimeFormat(new Date)
    };
  }
  onDateChange(name: string) {
    this.data[`${name}_Str`] = this.functionUtility.isValidDate(this.data[name]) ? this.functionUtility.getDateFormat(this.data[name]) : '';
  }
  validateInput(event: Event, name: string) {
    const intPattern = /^\d+$/; // chỉ cho phép số nguyên dương
    const inputElement = event.target as HTMLInputElement;

    if (!intPattern.test(inputElement.value)) {
      this.inputError = true;
      return this.snotifyService.error(
        name + " must be an integer",
        this.translateService.instant('System.Caption.Error')
      );
    }
    return this.inputError = false;
  }
}
