import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeTransferOperationInboundDto } from '@models/employee-maintenance/4_1_21_employee-transfer-operation-inbound';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_21_EmployeeTransferOperationInboundService } from '@services/employee-maintenance/s_4_1_21_employee-transfer-operation-inbound.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Observer } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss'],
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  data: EmployeeTransferOperationInboundDto = <EmployeeTransferOperationInboundDto>{
    effectiveStatusBefore: false,
    effectiveStatusAfter: false,
  };
  url: string = '';
  history_GUID: string = '';
  title: string;
  action: string = '';
  minEffectiveDate: Date = new Date(1, 12, 31);
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
  isCheckedEffectiveDate: boolean = false
  constructor(
    private _service: S_4_1_21_EmployeeTransferOperationInboundService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropDownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data
      .subscribe((role) => {
        this.action = role.title;
      })
      ;

    this._service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (res != null) {
        this.history_GUID = res
        this.getDetail();
      }
      else this.back();
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
        if (this.functionUtility.isEmptyObject(res)) this.back();
        this.data = res;
        this.data.effectiveDateBefore = new Date(this.data.effectiveDateBefore)
        this.data.effectiveDateBeforeStr = this.functionUtility.getDateFormat(this.data.effectiveDateBefore);
        if (this.data.effectiveDateAfter != null) {
          this.data.effectiveDateAfter = new Date(this.data.effectiveDateAfter);
          this.data.effectiveDateAfterStr = this.functionUtility.getDateFormat(this.data.effectiveDateAfter);
        }
        this.minEffectiveDate = new Date(this.data.effectiveDateBefore.getTime() + 86400000)
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
    this.getListFactoryAfter();
    this.getListAssignedFactoryAfter();
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
  }

  disableModeEdit = () => this.action == 'Edit';

  onDateChange(value: Date) {
    this.data.effectiveDateAfter = value
    this.data.effectiveDateAfterStr = this.data.effectiveDateAfter ? this.functionUtility.getDateFormat(new Date(this.data.effectiveDateAfter)) : '';
    this.isCheckedEffectiveDate = this.data.effectiveDateAfter?.getTime() >= this.minEffectiveDate.getTime()
  }
  next(isSave: boolean) {
    this.spinnerService.show();
    const callBack: Partial<Observer<OperationResult>> = {
      next: (res: any) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error)
        if (res.isSuccess) this.back();
      }
    }
    isSave
      ? this._service.edit(this.data).subscribe(callBack)
      : this._service.confirm(this.data).subscribe(callBack)
  }

  back = () => this.router.navigate([this.url]);

  //#region OnChange

  onChangeDivisionAfter() {
    this.data.factoryAfter = null;
    this.getListFactoryAfter();
    this.onChangeFactoryAfter();
  }

  onChangeFactoryAfter() {
    this.data.departmentAfter = null;
    this.getListDepartmentAfter();
  }

  onChangeAssignedDivisionAfter() {
    this.data.assignedFactoryAfter = null;
    this.getListAssignedFactoryAfter();
    this.onChangeAssignedFactoryAfter();
  }

  onChangeAssignedFactoryAfter() {
    this.data.assignedDepartmentAfter = null;
    this.getListAssignedDepartmentAfter();
  }

  onChangePositionGradeAfter() {
    this.data.positionTitleAfter = null;
    this.getListPositionTitleAfter();
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
      .getListDepartment(
        this.data.divisionBefore,
        this.data.factoryBefore,

      )
      .subscribe({
        next: (res) => {
          this.listDepartmentBefore = res;
        },
      });
  }

  getListDepartmentAfter() {
    this._service
      .getListDepartment(
        this.data.divisionAfter,
        this.data.factoryAfter,

      )
      .subscribe({
        next: (res) => {
          this.listDepartmentAfter = res;
        },
      });
  }

  getListAssignedDepartmentBefore() {
    this._service
      .getListDepartment(
        this.data.assignedDivisionBefore,
        this.data.assignedFactoryBefore,

      )
      .subscribe({
        next: (res) => {
          this.listAssignedDepartmentBefore = res;
        },
      });
  }

  getListAssignedDepartmentAfter() {
    this._service
      .getListDepartment(
        this.data.assignedDivisionAfter,
        this.data.assignedFactoryAfter,

      )
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
