import { Component, OnInit } from '@angular/core';
import {
  ClassButton,
  EmployeeMode,
  IconButton,
} from '@constants/common.constants';
import {
  EmployeeInformationParam,
  EmployeeInformationResult,
  EmployeeTransferOperationOutboundDto,
} from '@models/employee-maintenance/4_1_20_employee-transfer-operation-outbound';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_20_EmployeeTransferOperationOutboundService } from '@services/employee-maintenance/s_4_1_20_employee-transfer-operation-outbound.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss'],
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  mode = EmployeeMode;
  data: EmployeeTransferOperationOutboundDto = <EmployeeTransferOperationOutboundDto>{
    effectiveStatusBefore: false,
    effectiveStatusAfter: false,
  };
  tmp: EmployeeTransferOperationOutboundDto = <EmployeeTransferOperationOutboundDto>{};
  url: string = '';
  history_GUID: string = '';
  title: string;
  action: string = '';
  maxEffectiveDate: Date = new Date(9999, 12, 31);
  isEmployeeExist: boolean = false;
  employeeID: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listFactoryAfter: KeyValuePair[] = [];
  listAssignedFactoryAfter: KeyValuePair[] = [];
  listDepartmentBefore: KeyValuePair[] = [];
  listDepartmentAfter: KeyValuePair[] = [];
  listAssignedDepartmentBefore: KeyValuePair[] = [];
  listAssignedDepartmentAfter: KeyValuePair[] = [];
  listPositionGrade: KeyValuePair[] = [];
  listPositionTitleBefore: KeyValuePair[] = [];
  listPositionTitleAfter: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listReasonChangeBefore: KeyValuePair[] = [];
  listReasonChangeAfter: KeyValuePair[] = [];
  effectiveStatus: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' },
  ];
  constructor(
    private _service: S_4_1_20_EmployeeTransferOperationOutboundService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropDownList();
        this.getListPositionTitleAfter();
        this.getListPositionTitleBefore();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((role) => {
      this.action = role.title;
    });

    this._service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.action.toLowerCase() == this.mode.edit.toLowerCase()) {
        res != null ? (this.history_GUID = res) : this.back();
        this.getDetail();
      } else if (this.action == this.mode.add) {
        this.getEmployeeID();
      }
    });

    this._service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (res != null) this.history_GUID = res;
    });
    this.loadDropDownList();
  }

  ngOnDestroy(): void {
    this._service.setParamForm(null);
  }

  getDetail() {
    this.spinnerService.show();
    this._service.getDetail(this.history_GUID).subscribe({
      next: (res) => {
        if (this.functionUtility.isEmptyObject(res))
          this.back();
        this.isEmployeeExist = true;
        this.data = res;
        if (res.effectiveDateAfter != null)
          this.maxEffectiveDate = new Date(new Date(res.effectiveDateAfter).getTime() - 86400000)
        this.tmp = { ...res } as EmployeeTransferOperationOutboundDto;
        this.getListFactoryAfter();
        this.getListAssignedFactoryAfter();
        this.getListDepartmentAfter();
        this.getListDepartmentBefore();
        this.getListAssignedDepartmentAfter();
        this.getListAssignedDepartmentBefore();
        this.getListPositionTitleAfter();
        this.getListPositionTitleBefore();
        this.spinnerService.hide();
      },
      error: () => { this.back(); },
    });
  }

  loadDropDownList() {
    this.getListDivision();
    this.getListFactory();
    this.getListNationality();
    this.getListWorkType();
    this.getListReasonChangeOut();
    this.getListReasonChangeIn();
    this.getListPositionGrade();
    this.getListDepartmentAfter();
    this.getListDepartmentBefore();
    this.getListAssignedDepartmentAfter();
    this.getListAssignedDepartmentBefore();
    this.getListPositionTitleAfter();
    this.getListPositionTitleBefore();
    this.getListFactoryAfter();
    this.getListAssignedFactoryAfter();
  }

  disableModeEdit = () => this.action == this.mode.edit;
  onDateChange(name: string) {
    this.data[`${name}Str`] = this.data[name] ? this.functionUtility.getDateFormat(new Date(this.data[name])) : '';
  }
  save() {
    this.spinnerService.show();
    this._service[this.mode.add ? 'add' : 'edit'](this.data).subscribe({
      next: (res: any) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess,
          res.isSuccess ? this.action == this.mode.add ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg' : res.error)
        if (res.isSuccess) this.back();
      }
    })
  }

  back = () => this.router.navigate([this.url]);

  paramBeforeEmpty() {
    return (
      this.functionUtility.checkEmpty(this.data.divisionBefore) ||
      this.functionUtility.checkEmpty(this.data.factoryBefore)
    );
  }

  getEmployeeInformationBefore() {
    const param = <EmployeeInformationParam>{
      division: this.data.divisionBefore,
      factory: this.data.factoryBefore,
      employeeID: this.data.employeeIDBefore,
    };
    this._service.getEmployeeInformation(param).subscribe({
      next: (res) => {
        this.isEmployeeExist = (res != null)
        this.setValueEmployee(res ?? <EmployeeInformationResult>{});
      },
      error: () => {
        this.isEmployeeExist = false;
        this.setValueEmployee(<EmployeeInformationResult>{});
      }
    });
  }

  getEmployeeID() {
    this._service.getEmployeeID().subscribe({
      next: (res) => {
        this.employeeID = res;
      }
    });
  }

  setValueEmployee(res: EmployeeInformationResult) {
    this.data.useR_GUID = res.useR_GUID;
    this.data.nationalityBefore = res.nationality;
    this.data.identificationNumberBefore = res.identificationNumber;
    this.data.localFullNameBefore = res.localFullName;
    this.data.departmentBefore = res.department;
    this.data.assignedDivisionBefore = res.assignedDivision;
    this.data.assignedFactoryBefore = res.assignedFactory;
    this.data.assignedEmployeeIDBefore = res.assignedEmployeeID;
    this.data.assignedDepartmentBefore = res.assignedDepartment;
    this.data.positionGradeBefore = res.positionGrade;
    this.data.positionTitleBefore = res.positionTitle;
    this.data.workTypeBefore = res.workType;
    this.data.nationalityAfter = res.nationality;
    this.data.identificationNumberAfter = res.identificationNumber;
    this.data.localFullNameAfter = res.localFullName;

    if (this.data.assignedFactoryBefore) this.getListDepartmentBefore();
    if (this.data.assignedFactoryBefore) this.getListAssignedDepartmentBefore();
    if (this.data.positionGradeBefore) this.getListPositionTitleBefore();
  }

  resetValueEmployee() {
    if (
      this.data.divisionAfter == this.tmp.divisionAfter &&
      this.data.factoryAfter == this.tmp.factoryAfter
    ) {
      this.data.employeeIDAfter = this.tmp.employeeIDAfter;
      this.data.departmentAfter = this.tmp.departmentAfter;
      this.data.assignedDivisionAfter = this.tmp.assignedDivisionAfter;
      this.data.assignedFactoryAfter = this.tmp.assignedFactoryAfter;
      this.data.assignedEmployeeIDAfter = this.tmp.employeeIDAfter;
      this.data.assignedDepartmentAfter = this.tmp.employeeIDAfter;
      this.data.positionGradeAfter = this.tmp.positionGradeAfter;
      this.data.positionTitleAfter = this.tmp.positionTitleAfter;
      this.data.workTypeAfter = this.tmp.workTypeAfter;
      this.data.reasonForChangeAfter = this.tmp.reasonForChangeAfter;
      this.data.effectiveDateAfter = this.tmp.effectiveDateAfter;
      this.data.effectiveDateAfterStr = this.tmp.effectiveDateAfterStr;
    } else {
      this.deleteProperty('employeeIDAfter')
      this.deleteProperty('departmentAfter')
      this.deleteProperty('assignedDivisionAfter')
      this.deleteProperty('assignedFactoryAfter')
      this.deleteProperty('assignedEmployeeIDAfter')
      this.deleteProperty('assignedDepartmentAfter')
      this.deleteProperty('positionGradeAfter')
      this.deleteProperty('positionTitleAfter')
      this.deleteProperty('workTypeAfter')
      this.deleteProperty('reasonForChangeAfter')
      this.deleteProperty('effectiveDateAfter')
      this.deleteProperty('effectiveDateAfterStr')
    }
  }
  //#region OnChange
  onChangeDivisionBefore() {
    if (!this.paramBeforeEmpty()) {
      this.getListDepartmentBefore()
      if (!this.functionUtility.checkEmpty(this.data.employeeIDBefore))
        this.getEmployeeInformationBefore();
    } else {
      const res = <EmployeeInformationResult>{};
      this.setValueEmployee(res);
    }
  }

  onChangeFactoryBefore() {
    if (!this.paramBeforeEmpty()) {
      this.getListDepartmentBefore()
      if (!this.functionUtility.checkEmpty(this.data.employeeIDBefore))
        this.getEmployeeInformationBefore();
    } else {
      const res = <EmployeeInformationResult>{};
      this.setValueEmployee(res);
    }
  }

  onChangeEmployeeIDBefore() {
    if (!this.paramBeforeEmpty()) {
      this.getListDepartmentBefore()
      if (!this.functionUtility.checkEmpty(this.data.employeeIDBefore))
        this.getEmployeeInformationBefore();
    } else {
      const res = <EmployeeInformationResult>{};
      this.setValueEmployee(res);
    }
  }

  onChangeDivisionAfter() {
    this.data.factoryAfter = null;
    this.getListFactoryAfter();
    this.resetValueEmployee();
  }

  onChangeFactoryAfter() {
    this.getListDepartmentAfter();
    this.resetValueEmployee();
  }
  //#region

  //#region Get List
  getListDivision() {
    this._service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }

  getListFactory() {
    this._service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListFactoryAfter() {
    this._service
      .getListFactoryByDivision(this.data.divisionAfter)
      .subscribe({
        next: (res) => {
          this.listFactoryAfter = res;
        },
      });
  }

  getListAssignedFactoryAfter() {
    this._service
      .getListFactoryByDivision(this.data.assignedDivisionAfter)
      .subscribe({
        next: (res) => {
          this.listAssignedFactoryAfter = res;
        },
      });
  }
  getListNationality() {
    this._service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }

  getListWorkType() {
    this._service.getListWorkType().subscribe({
      next: (res) => {
        this.listWorkType = res;
      },
    });
  }

  getListReasonChangeOut() {
    this._service.getListReasonChangeOut().subscribe({
      next: (res) => {
        this.listReasonChangeBefore = res;
      },
    });
  }

  getListReasonChangeIn() {
    this._service.getListReasonChangeIn().subscribe({
      next: (res) => {
        this.listReasonChangeAfter = res;
      },
    });
  }

  getListDepartmentBefore() {
    this._service
      .getListDepartment(this.data.divisionBefore, this.data.factoryBefore)
      .subscribe({
        next: (res) => {
          this.listDepartmentBefore = res;
        },
      });
  }

  getListDepartmentAfter() {
    this._service
      .getListDepartment(this.data.divisionAfter, this.data.factoryAfter)
      .subscribe({
        next: (res) => {
          this.listDepartmentAfter = res;
        },
      });
  }

  getListAssignedDepartmentBefore() {
    this._service
      .getListDepartment(this.data.assignedDivisionBefore, this.data.assignedFactoryBefore)
      .subscribe({
        next: (res) => {
          this.listAssignedDepartmentBefore = res;
        },
      });
  }

  getListAssignedDepartmentAfter() {
    this._service
      .getListDepartment(this.data.assignedDivisionAfter, this.data.assignedFactoryAfter)
      .subscribe({
        next: (res) => {
          this.listAssignedDepartmentAfter = res;
        },
      });
  }

  getListPositionGrade() {
    this._service.getListPositionGrade().subscribe({
      next: (res) => {
        this.listPositionGrade = res;
      },
    });
  }

  getListPositionTitleBefore() {
    this._service
      .getListPositionTitle(this.data.positionGradeBefore ?? -1)
      .subscribe({
        next: (res) => {
          this.listPositionTitleBefore = res;
        },
      });
  }

  getListPositionTitleAfter() {
    this._service
      .getListPositionTitle(this.data.positionGradeAfter ?? -1)
      .subscribe({
        next: (res) => {
          this.listPositionTitleAfter = res;
        },
      });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
