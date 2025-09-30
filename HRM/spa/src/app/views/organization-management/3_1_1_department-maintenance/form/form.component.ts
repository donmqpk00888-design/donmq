import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Org_Department, Language, ListUpperVirtual, languageSource } from '@models/organization-management/3_1_1-department-maintenance';
import { S_3_1_1_DepartmentMaintenanceService } from '@services/organization-management/s_3_1_1_department-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  currentDate: Date = new Date();
  approvedHeadcount: string = ''
  title: string = '';
  url: string = '';
  action: string = '';
  checkDepartment: boolean = true;
  checkFactory: boolean = true;
  checkDept: boolean;
  time_end: Date = null;
  time_start: Date = null;
  division: string;
  iconButton = IconButton;
  divisions: KeyValuePair[] = [];
  factory: KeyValuePair[] = [];
  department: KeyValuePair[] = [];
  level: KeyValuePair[] = [];
  listUpperVirtual: ListUpperVirtual[] = [];
  param: HRMS_Org_Department = <HRMS_Org_Department>{
    isActive: true,
    attribute: 'Directly',
    supervisor_Type: 'A'
  }
  bsConfig: BsDatepickerConfig = <BsDatepickerConfig>{
    isAnimated: true,
    dateInputFormat: "YYYY/MM/DD",
  };
  attribute: KeyValuePair[] = [
    { key: 'Directly', value: 'OrganizationManagement.DepartmentMaintenance.Directly' },
    { key: 'Staff', value: 'OrganizationManagement.DepartmentMaintenance.Staff' },
    { key: 'Non-Directly', value: 'OrganizationManagement.DepartmentMaintenance.NonDirectly' }
  ];
  key: KeyValuePair[] = [
    { key: true, value: "OrganizationManagement.DepartmentMaintenance.Enabled" },
    { key: false, value: "OrganizationManagement.DepartmentMaintenance.Disabled" }
  ];
  list_Supervisor_Type: KeyValuePair[] = [
    { key: "A", value: "OrganizationManagement.DepartmentMaintenance.A" },
    { key: "B", value: "OrganizationManagement.DepartmentMaintenance.B" },
    { key: "C", value: "OrganizationManagement.DepartmentMaintenance.C" },
    { key: "D", value: "OrganizationManagement.DepartmentMaintenance.D" }
  ];
  formType: string = ''
  constructor(
    private service: S_3_1_1_DepartmentMaintenanceService,
    private modalService: ModalService
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListLevel();
      this.getListUpper(this.param.department_Code, this.param.division, this.param.factory);
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave && this.formType == 'Edit')
        this.param.department_Name = res.department_Name
    })
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res.title
      this.action = `System.Action.${this.formType}`
      this.getSource()
    })
    this.getListDivision();
    this.getListFactory();
    this.getListLevel();
  }
  getSource() {
    if (this.formType == 'Edit') {
      let source = this.service.programSource();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.param = structuredClone(source.selectedData);
        this.time_start = this.functionUtility.checkEmpty(this.param.effective_Date) ? null : new Date(this.param.effective_Date);
        this.time_end = this.functionUtility.checkEmpty(this.param.expiration_Date) ? null : new Date(this.param.expiration_Date);
        this.approvedHeadcount = this.param.approved_Headcount == null ? '' : this.param.approved_Headcount.toString();
        this.getListUpper(this.param.department_Code, this.param.division, this.param.factory);
      }
      else
        this.back()
    }
  }

  onClick = () => this.getListUpper(this.param.department_Code, this.param.division, this.param.factory);

  onSelectChange() {
    if (this.param.attribute === 'Directly')
      this.param.virtual_Department = ''
    if (this.param.attribute === 'Staff')
      this.param.virtual_Department = ''
    if (this.param.attribute === 'Non-Directly')
      this.param.upper_Department = ''
  }
  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => this.divisions = res
    });
  }
  getListFactory() {
    if (this.functionUtility.checkEmpty(this.param.division))
      this.division = ''
    else
      this.division = this.param.division
    this.service.getListFactory(this.division).subscribe({
      next: (res) => this.factory = res
    });
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory,).subscribe({
      next: (res) => {
        this.department = res;
      }
    });
  }
  onSelectGetFactory() {
    this.getListFactory()
    this.param.factory = null
    this.param.department_Code = ''
    this.checkDepartment = true

    this.checkFactory = this.functionUtility.checkEmpty(this.param.division)
  }
  onSelectGetDepartmentCode() {
    this.getListDepartment()
    this.param.department_Code = ''
    this.checkDepartment = this.functionUtility.checkEmpty(this.param.factory)
  }
  onSelectCheckDeptCode() {
    if (this.department.some(item => item.key === this.param.department_Code))
      this.snotifyService.error(
        this.translateService.instant('OrganizationManagement.DepartmentMaintenance.DuplicateDeptCode'),
        this.translateService.instant('System.Caption.Error'))
    this.service.CheckListDeptCode(this.param.division, this.param.factory, this.param.department_Code).subscribe({
      next: (res) => this.checkDept = res
    });
  }

  getListLevel() {
    this.service.getListLevel().subscribe({
      next: (res) => this.level = res
    });
  }
  getListUpper(department_Code: string, division: string, factory: string) {
    this.service.getListUpperVirtual(department_Code, division, factory).subscribe({
      next: (res) => this.listUpperVirtual = res
    });
  }
  setParam() {
    this.param.approved_Headcount = this.approvedHeadcount !== "" ? parseInt(this.approvedHeadcount) : null
    this.param.update_By = "admin"
    this.param.update_Time = this.currentDate.toDate().toUTCDate().toJSON();
    // Set thời gian
    this.param.effective_Date = this.time_start == null ? null : this.time_start.toDate().toUTCDate().toJSON()
    this.param.expiration_Date = this.time_end == null ? null : this.time_end.toDate().toUTCDate().toJSON()
  }
  saveAndContinue() {
    this.setParam();
    if (this.formType != 'Edit') {
      this.spinnerService.show();
      this.service.add(this.param).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.onEdit(this.param)
            this.clear();
            this.onSelectCheckDeptCode()
          }
          else {
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
          }
        }
      })
    }
  }
  clear() {
    this.param = <HRMS_Org_Department>{
      supervisor_Type: 'A',
      isActive: true,
      attribute: 'Directly'
    }
    this.approvedHeadcount = '';
    this.time_start = null;
    this.time_end = null;
  }
  save() {
    this.setParam();
    if (this.formType != 'Edit') {
      this.spinnerService.show();
      this.param.supervisor_Employee_ID = this.param.supervisor_Employee_ID?.toUpperCase();
      this.service.add(this.param).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.onEdit(this.param, true)
            this.onSelectCheckDeptCode()
          }
          else {
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
          }
        }
      })
    }
    else {
      this.spinnerService.show();
      this.param.supervisor_Employee_ID = this.param.supervisor_Employee_ID?.toUpperCase();
      this.service.edit(this.param).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else {
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
          }
        }
      })
    }
  }

  back = () => this.router.navigate([this.url]);

  onEdit(item: HRMS_Org_Department, callBack: boolean = false) {
    let source: languageSource = <languageSource>{
      data: <Language>{
        division: item.division,
        factory: item.factory,
        department_Code: item.department_Code,
        department_Name: item.department_Name,
        detail: []
      },
      callBack: callBack
    };
    this.modalService.open(source, this.formType);
  }

  checkEmpty() {
    if (this.param.division == null || this.param.factory == null || this.param.org_Level == null || this.param.center_Code == null || this.param.center_Code == ''
      || this.param.cost_Center == null || this.param.cost_Center == '' || this.param.department_Code == null || this.param.department_Code == ''
      || this.param.department_Name == null || this.param.department_Name == '' || this.time_start == null)
      return true
    return false
  }
}
