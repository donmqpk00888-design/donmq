import { Component, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Org_Work_Type_Headcount, HRMS_Org_Work_Type_HeadcountParam, HRMS_Org_Work_Type_HeadcountUpdate } from '@models/organization-management/3_1_2_work-type-headcount-maintenance';
import { S_3_1_2_WorktypeHeadcountMaintenanceService } from '@services/organization-management/s_3_1_2_work-type-headcount-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase {
  //#region Vaiables
  formType: string = '';
  isSave: boolean = false;
  isDepartmentExist: boolean = false;

  title: string = '';
  url: string = '';
  iconButton = IconButton;
  //#endregion
  totalHeadCount: number = 0;
  totalActualNumber: number = 0;
  maxIntNumer = 2147483647;

  effective_Date_value: Date = null;
  param: HRMS_Org_Work_Type_HeadcountParam = <HRMS_Org_Work_Type_HeadcountParam>{
    division: '',
    factory: '',
    department_Code: '',
    effective_Date: '',

  }

  nextElementFocus: string = '';
  workTypeHeads: HRMS_Org_Work_Type_Headcount[] = [];

  updateWorkTypeHeads: HRMS_Org_Work_Type_HeadcountUpdate = <HRMS_Org_Work_Type_HeadcountUpdate>{
    dataUpdate: [], dataNewAdd: []
  }

  divisions: KeyValuePair[] = [];
  allFactories: KeyValuePair[] = [];
  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];
  departmentName: string = '';

  messageErrorDepartment: string = '';
  workCodeNames: KeyValuePair[] = [];

  constructor(private workTypeHeadcountMaintermanceServices: S_3_1_2_WorktypeHeadcountMaintenanceService) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDivisions();
      this.getFactories();
      if (!this.functionUtility.checkEmpty(this.param.department_Code))
        this.getDepartmentName(this.setParamDeparmentName());
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res['title']
      this.divisions = res.resolverDivisions;
    });
    this.getFactories();
    this.getDepartments();
    this.getWorkCodeNames();
  }

  //#region Methods
  getDataFromSource() {
    effect(() => {
      let source = this.workTypeHeadcountMaintermanceServices.workTypeHeadcountSource();
      if (source && source != null) {
        this.param = { ...source.model, language: ''};
        if (this.formType == 'Edit' && Object.keys(this.param).length == 0)
          this.back()
        else {
          if (!this.functionUtility.checkEmpty(this.param.effective_Date))
            this.effective_Date_value = new Date(this.param.effective_Date);
          if (!this.functionUtility.checkEmpty(this.param.department_Code))
            this.getDepartmentName(this.setParamDeparmentName())
        }
      }
      else this.back();
    })
  }

  getData() {
    if (this.formType == 'Edit')
      this.getDataUpdate();
    else {
      this.workTypeHeads.push(
        <HRMS_Org_Work_Type_Headcount>{
          division: this.param.division,
          factory: this.param.factory,
          department_Code: this.param.department_Code,
          effective_Date: this.param.effective_Date,
          work_Type_Code: '',
          work_Type_Name: '',
          approved_Headcount: 0,
          actual_Headcount: 0,
          isEdit: false
        })
    }
  }

  getDivisions() {
    this.workTypeHeadcountMaintermanceServices.getDivisions().subscribe({
      next: result => {
        this.divisions = result;
      }
    })
  }

  getFactories() {
    if (!this.functionUtility.checkEmpty(this.param.division)) {
      this.workTypeHeadcountMaintermanceServices.getFactoriesByDivision(this.param.division).subscribe({
        next: result => {
          this.factories = result;
          if (result.length == 0)
            this.factories = [...this.allFactories]
        }
      })
    }
    else {
      this.workTypeHeadcountMaintermanceServices.getFactories().subscribe({
        next: result => {
          this.factories = result;
          // Tất cả danh sách Factories
          this.allFactories = [...result];
        }
      })
    }
  }

  getDepartments() {
    if (!this.functionUtility.checkEmpty(this.param.division) && !this.functionUtility.checkEmpty(this.param.factory)) {
      this.workTypeHeadcountMaintermanceServices.getDepartmentsByDivisionFactory(this.param.division, this.param.factory).subscribe({
        next: result => { this.departments = result; }
      })
    }
    else {
      this.workTypeHeadcountMaintermanceServices.getDepartments().subscribe({
        next: result => { this.departments = result; }
      })
    }
  }

  getWorkCodeNames() {
    this.workTypeHeadcountMaintermanceServices.getWorkCodeNames().subscribe({
      next: result => {
        this.workCodeNames = result;
        this.getData();
      }
    })
  }

  getDepartmentName(param: HRMS_Org_Work_Type_HeadcountParam) {
    this.workTypeHeadcountMaintermanceServices.getDepartmentName(param).subscribe({
      next: result => {
        if (result != null)
          this.departmentName = result.department_Name;
        this.validateSave();
      }
    })
  }

  getDataUpdate() {
    this.spinnerService.show();
    this.workTypeHeadcountMaintermanceServices.getListUpdate(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.workTypeHeads = result;
        this.updateWorkTypeHeads.dataUpdate = result;
        this.totalHeadCount = this.workTypeHeads.length == 0 ? 0 : this.workTypeHeads.map(x => +x.approved_Headcount).reduce((a, b) => a + b);
        // Tỉnh tổng ActualHeadcount
        this.totalActualNumber = this.workTypeHeads.length == 0 ? 0 : this.workTypeHeads.map(x => +x.actual_Headcount).reduce((a, b) => a + b);
        this.workTypeHeads.forEach(x => {
          x.isEdit = true;
          let codeName = this.workCodeNames.find(z => z.key == x.work_Type_Code);
          if (codeName != null || codeName != undefined) x.work_Type_Name = codeName.value;
          else x.work_Type_Name = '';
        })
      }
    })
  }

  setParamDeparmentName(): HRMS_Org_Work_Type_HeadcountParam {
    return <HRMS_Org_Work_Type_HeadcountParam>{
      division: this.param.division,
      factory: this.param.factory,
      department_Code: this.param.department_Code
    }
  }

  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();
  //#endregion

  //#region SAVECHANGE
  validate(): boolean {
    if (this.functionUtility.checkEmpty(this.param.division)) {
      this.snotifyService.warning(`Please input ${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.Division')}`, this.translateService.instant('System.Caption.Warning'));
      return false;
    }
    if (this.functionUtility.checkEmpty(this.param.factory)) {
      this.snotifyService.warning(`Please input ${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.Factory')}`, this.translateService.instant('System.Caption.Warning'));
      return false;
    }
    if (this.functionUtility.checkEmpty(this.param.department_Code)) {
      this.snotifyService.warning(`Please input ${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.Department')}`, this.translateService.instant('System.Caption.Warning'));
      return false;
    }
    if (this.functionUtility.checkEmpty(this.param.effective_Date)) {
      this.snotifyService.warning(`Please input ${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.EffectiveDate')}`, this.translateService.instant('System.Caption.Warning'));
      return false;
    }
    return true;
  }
  validateSave() {
    if (!this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.param.department_Code) &&
      !this.functionUtility.checkEmpty(this.departmentName) &&
      this.effective_Date_value != null &&
      this.effective_Date_value.toString() != 'Invalid Date' &&
      this.effective_Date_value.toString() != 'NaN/NaN'
    ) this.isSave = true;
    else
      this.isSave = false;
  }

  saveChange() {
    // Kiểm tra danh sách có WorkTypeCode null
    let checkWTCIsNull = this.workTypeHeads.filter(z => this.functionUtility.checkEmpty(z.work_Type_Code));
    if (checkWTCIsNull.length > 0)
      return this.snotifyService.warning(`Data has ${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.WorkTypeCode')} is empty. please check again`, this.translateService.instant('System.Caption.Warning'));
    // Kiểm tra Trùng Key Work Type Code
    const distinctArray = Array.from(new Set(this.workTypeHeads.map(x => x.work_Type_Code)));
    let filter = distinctArray.filter((x) => this.workTypeHeads.filter(z => z.work_Type_Code == x).length > 1)
    if (filter.length > 0)
      return this.snotifyService.warning(`${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.WorkTypeCode')} : ${filter.join(',')} is exist`, this.translateService.instant('System.Caption.Warning'));
    let checkValidate = this.validate();
    if (checkValidate) {
      this.spinnerService.show();
      // Add
      if (this.formType == 'Add') {
        this.workTypeHeadcountMaintermanceServices.create(this.workTypeHeads).subscribe({
          next: result => {
            this.spinnerService.hide();
            if (result.isSuccess) {
              this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
              this.back();
            }
            else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
          }
        })
      }
      // Update
      else {
        this.updateWorkTypeHeads.dataUpdate = this.workTypeHeads.filter(x => x.isEdit);
        this.updateWorkTypeHeads.dataNewAdd = this.workTypeHeads.filter(x => !x.isEdit);

        this.workTypeHeadcountMaintermanceServices.update(this.updateWorkTypeHeads).subscribe({
          next: result => {
            this.spinnerService.hide();
            if (result.isSuccess) {
              this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
              this.back();
            }
            else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
          }
        })
      }
    }
  }
  //#endregion

  //#region Events
  onTabKey() {
    this.workTypeHeads.push(
      <HRMS_Org_Work_Type_Headcount>{
        division: this.param.division,
        factory: this.param.factory,
        department_Code: this.param.department_Code,
        effective_Date: this.param.effective_Date,
        work_Type_Code: '',
        work_Type_Name: '',
        approved_Headcount: 0,
        actual_Headcount: 0,
        isEdit: false
      })
  }

  onDivisionChange() {
    // Lấy danh sách Factories theo Division
    this.deleteProperty('factory')
    this.deleteProperty('department_Code')
    this.getFactories();
    this.getDepartments();
    this.workTypeHeads.forEach(x => { x.division = this.param.division })
    this.validateSave();
  }

  onFactoryChange() {
    this.workTypeHeads.forEach(x => { x.factory = this.param.factory })
    this.validateSave();
    this.deleteProperty('department_Code')
    this.getDepartments();
  }

  onDepartmentChange() {
    if (
      !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.param.department_Code)) {
      this.workTypeHeadcountMaintermanceServices.getDepartmentName(this.setParamDeparmentName()).subscribe({
        next: result => {
          if (result == null) {
            this.messageErrorDepartment = `${this.translateService.instant('OrganizationManagement.WorkTypeHeadcountMaintenance.Department')} does not exist.`
            this.departmentName = '';
          }
          else {
            this.messageErrorDepartment = '';
            this.departmentName = result.department_Name;
            this.workTypeHeads.forEach(x => { x.department_Code = this.param.department_Code })
          }
          this.validateSave();
        }
      })
    }
    else {
      this.messageErrorDepartment = '';
      this.validateSave();
    }
  }

  onEffectiveDateChange() {
    if (this.effective_Date_value != null &&
      this.effective_Date_value.toString() != 'Invalid Date' &&
      this.effective_Date_value.toString() != 'NaN/NaN')
      this.param.effective_Date = this.effective_Date_value.toDate().toStringYearMonth();
    else this.param.effective_Date = '';
    this.workTypeHeads.forEach(x => { x.effective_Date = this.param.effective_Date })
    this.validateSave();
  }

  onWorkTypeCodeChange(index: number) {
    this.workTypeHeads.forEach((x, i) => {
      if (i == index) {
        let codeName = this.workCodeNames.find(z => z.key == x.work_Type_Code);
        if (codeName != null || codeName != undefined) x.work_Type_Name = codeName.value;
        else x.work_Type_Name = '';
      }
    })
  }

  onApprovedHeadcountChange(index: number) {
    // kiểm tra max value
    this.workTypeHeads.forEach((x, i) => {
      if (i == index) {
        if (this.functionUtility.checkEmpty(x.approved_Headcount))
          x.approved_Headcount = 0;
        if (+x.approved_Headcount > this.maxIntNumer)
          x.approved_Headcount = this.maxIntNumer;
      }
    })
    // Tỉnh tổng ApprovedHeadcount
    this.totalHeadCount = this.workTypeHeads.map(x => +x.approved_Headcount).reduce((a, b) => a + b);
  }

  onActualHeadcountChange(index: number) {
    // kiểm tra max value
    this.workTypeHeads.forEach((x, i) => {
      if (i == index) {
        if (this.functionUtility.checkEmpty(x.actual_Headcount))
          x.actual_Headcount = 0;
        else if (+x.actual_Headcount > this.maxIntNumer)
          x.actual_Headcount = this.maxIntNumer;
        else
          x.actual_Headcount = +x.actual_Headcount;
      }
    })
    // Tỉnh tổng ActualHeadcount
    this.totalActualNumber = this.workTypeHeads.map(x => +x.actual_Headcount).reduce((a, b) => a + b);
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
