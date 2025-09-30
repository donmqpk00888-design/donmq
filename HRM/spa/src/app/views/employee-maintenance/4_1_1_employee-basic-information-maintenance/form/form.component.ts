import { Component, Input, OnInit } from '@angular/core';
import {
  ClassButton,
  EmployeeMode,
  IconButton,
} from '@constants/common.constants';
import {
  CheckBlackList,
  DepartmentSupervisorList,
  EmployeeBasicInformationMaintenanceDto,
  EmployeeBasicInformationMaintenanceSource,
  checkDuplicateParam,
} from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { S_4_1_1_EmployeeBasicInformationMaintenanceService } from '@services/employee-maintenance/s_4_1_1_employee-basic-information-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form-4-1-1',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss'],
})
export class FormComponent411 extends InjectBase implements OnInit {
  // Gọi hàm từ parent component
  @Input() tranfer: EmployeeBasicInformationMaintenanceSource;
  invalidCase1: boolean = false;
  invalidCase2: boolean = false;
  invalidCase3: boolean = false;
  invalidCase4: boolean = false;
  invalidCase5: boolean = false;
  isCollapsed = true;
  mode = EmployeeMode;
  maxBirthDay: Date = new Date();
  iconButton = IconButton;
  classButton = ClassButton;
  data: EmployeeBasicInformationMaintenanceDto = <EmployeeBasicInformationMaintenanceDto>{
    company: 'W',
    employmentStatus: 'Y',
    numberOfDependents: null,
    workShiftType: '',
    dateOfResignation: null,
    reasonResignation: null,
    blacklist: null,
    work8hours: false,
    onboardDate: new Date(),
    dateOfGroupEmployment: new Date(),
    seniorityStartDate: new Date(),
    annualLeaveSeniorityStartDate: new Date(),
  };
  tmp: EmployeeBasicInformationMaintenanceDto = <EmployeeBasicInformationMaintenanceDto>{};
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listAssignedFactory: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  listPermission: KeyValuePair[] = [];
  listIdentityType: KeyValuePair[] = [];
  listEducation: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listRestaurant: KeyValuePair[] = [];
  listReligion: KeyValuePair[] = [];
  listTransportationMethod: KeyValuePair[] = [];
  listVehicleType: KeyValuePair[] = [];
  listWorkLocation: KeyValuePair[] = [];
  listReasonResignation: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedDepartment: KeyValuePair[] = [];
  listProvinceDirectly: KeyValuePair[] = [];
  listRegisteredCity: KeyValuePair[] = [];
  listMailingCity: KeyValuePair[] = [];
  listPositionGrade: KeyValuePair[] = [];
  listPositionTitle: KeyValuePair[] = [];
  listDepartmentSupervisor: DepartmentSupervisorList[];
  listWorkTypeShift: KeyValuePair[] = [];
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Onjob' },
    { key: 'N', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Resigned' },
    { key: 'U', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Unpaid' },
  ];
  listCrossFactoryStatus: KeyValuePair[] = [
    { key: 'A', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Assigned', optional: false },
    { key: 'S', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Supported', optional: false },
  ];
  gender: KeyValuePair[] = [
    { key: 'F', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Female' },
    { key: 'M', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Male' },
  ];
  bloodType: KeyValuePair[] = [
    { key: 'A', value: 'A', optional: false },
    { key: 'B', value: 'B', optional: false },
    { key: 'C', value: 'AB', optional: false },
    { key: 'O', value: 'O', optional: false },
  ];
  maritalStatus: KeyValuePair[] = [
    { key: 'M', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Married' },
    { key: 'U', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Unmarried' },
    { key: 'O', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Other' },
  ];
  swipeCardOption: KeyValuePair[] = [
    { key: true, value: 'Y.刷卡' },
    { key: false, value: 'N.不需刷卡' },
  ];
  unionMembership: KeyValuePair[] = [
    { key: true, value: 'Y', optional: false },
    { key: false, value: 'N', optional: true },
  ];
  work8hours: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' },
  ];
  blacklist: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' },
  ];
  isSameRegisteredChecked: boolean = false;

  constructor(
    private _service: S_4_1_1_EmployeeBasicInformationMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.loadDropDownList();
        if (!this.functionUtility.checkEmpty(this.tranfer.useR_GUID))
          this.getDepartmentSupervisor(this.tranfer.useR_GUID);
      });
  }
  ngOnInit() {
    this.loadDropDownList();
  }

  loadDropDownList() {
    this.getListDivision();
    this.getListNationality();
    this.getListPermission(); // can load after init
    this.getListIdentityType(); // can load after init
    this.getListEducation(); // can load after init
    this.getListWorkType(); // can load after init
    this.getListRestaurant(); // can load after init
    this.getListReligion(); // can load after init
    this.getListTransportationMethod(); // can load after init
    this.getListVehicleType(); // can load after init
    this.getListWorkLocation(); // can load after init
    this.getListReasonResignation(); // can load after init
    this.getListPositionGrade(); // can load after init
    this.getListFactory();
    this.getListAssignedFactory();
    this.getListDepartmet();
    this.getListAssignedDepartmet();
    this.getListPositionTitle();
    this.getListWorkTypeShift();
  }

  ngOnChanges() {
    if (
      this.tranfer.mode == this.mode.edit ||
      this.tranfer.mode == this.mode.query ||
      this.tranfer.mode == this.mode.rehire
    ) {
      this.getDepartmentSupervisor(this.tranfer.useR_GUID);
      this.getDetail();
    }
  }

  getDetail() {
    this.spinnerService.show();
    this._service.getDetail(this.tranfer.useR_GUID).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (this.functionUtility.isEmptyObject(res)) this.close();

        if (this.tranfer.mode != this.mode.rehire) {
          this.data = res;
          this.data.employmentStatus =  this.tranfer.employmentStatus;
        } else {
          this.tmp = { ...res } as EmployeeBasicInformationMaintenanceDto;
          this.data = <EmployeeBasicInformationMaintenanceDto>{
            useR_GUID: res.useR_GUID,
            nationality: res.nationality,
            identificationNumber: res.identificationNumber,
            issuedDate: res.issuedDate,
            localFullName: res.localFullName,
            preferredEnglishFullName: res.preferredEnglishFullName,
            chineseName: res.chineseName,
            company: 'W',
            employmentStatus: 'N',
            numberOfDependents: null,
            workShiftType: '',
            dateOfResignation: null,
            reasonResignation: null,
            blacklist: null,
            work8hours: false,
            onboardDate: new Date(),
            dateOfGroupEmployment: new Date(),
            seniorityStartDate: new Date(),
            annualLeaveSeniorityStartDate: new Date(),
          };
          const checkBlackListParam = <CheckBlackList>{
            useR_GUID: res.useR_GUID,
          };
          this.checkBlackList(checkBlackListParam);
        }
        this.listCrossFactoryStatus.forEach(
          (x) => (x.optional = x.key == res.crossFactoryStatus)
        );
        this.bloodType.forEach((x) => (x.optional = x.key == res.bloodType));
        this.work8hours.forEach((x) => (x.optional = x.key == res.work8hours));
        this.getListProvinceDirectly();
        this.getListRegisteredCity();
        this.getListMailingCity();
        this.getListPositionTitle();
        this.getListFactory();
        this.getListAssignedFactory();
        this.getListDepartmet();
        this.getListAssignedDepartmet();
      }
    });
  }

  disableModeView = () => this.tranfer.mode == this.mode.query;

  disableModeViewEdit = () =>
    this.tranfer.mode == this.mode.query || this.tranfer.mode == this.mode.edit;

  checkDuplicateCase1() {
    if (
      this.functionUtility.checkEmpty(this.data.nationality) ||
      this.functionUtility.checkEmpty(this.data.identificationNumber)
    ) {
      this.invalidCase1 = false;
      return;
    }
    if (
      this.data.nationality == this.tmp.nationality &&
      this.data.identificationNumber == this.tmp.identificationNumber
    ) {
      this.invalidCase1 = false;
      this.functionUtility.snotifySuccessError(true, 'Nationality and Identification Number are valid')
      return;
    }
    this.spinnerService.show();
    this._service
      .checkDuplicateCase1(
        this.data.nationality,
        this.data.identificationNumber
      )
      .subscribe({
        next: (isDuplicate) => {
          this.spinnerService.hide();
          this.invalidCase1 = isDuplicate;
          if (!this.invalidCase1) {
            this.functionUtility.snotifySuccessError(true, 'Nationality and Identification Number are valid');
          } else this.functionUtility.snotifySuccessError(false, 'Nationality and Identification Number are exist');

        }
      });
  }

  checkDuplicateCase2() {
    if (
      this.functionUtility.checkEmpty(this.data.division) ||
      this.functionUtility.checkEmpty(this.data.factory) ||
      this.functionUtility.checkEmpty(this.data.employeeID)
    ) {
      this.invalidCase2 = false;
      return;
    }
    if (this.data.employeeID == `${this.data.factory}-`) {
      return;
    }

    this.spinnerService.show();
    const param = <checkDuplicateParam>{
      division: this.data.division,
      factory: this.data.factory,
      employeeID: this.data.employeeID,
    };
    this._service.checkDuplicateCase2(param).subscribe({
      next: (isDuplicate) => {
        this.spinnerService.hide();
        this.invalidCase2 = isDuplicate;
        this.functionUtility.snotifySuccessError(!this.invalidCase2, !this.invalidCase2 ? 'Division, Factory, Employee ID are valid' : 'Division, Factory, Employee ID are exist')
      }
    });
  }

  checkDuplicateCase3() {
    if (
      this.functionUtility.checkEmpty(this.data.assignedDivision) ||
      this.functionUtility.checkEmpty(this.data.assignedFactory) ||
      this.functionUtility.checkEmpty(this.data.assignedEmployeeID)
    ) {
      this.invalidCase3 = false;
      return;
    }

    if (this.data.assignedEmployeeID == `${this.data.assignedFactory}-`) return;

    if (
      this.functionUtility.checkEmpty(this.data.assignedDivision) &&
      this.functionUtility.checkEmpty(this.data.assignedFactory) &&
      this.functionUtility.checkEmpty(this.data.assignedEmployeeID)
    ) {
      this.invalidCase3 = true;
      return;
    }

    this.spinnerService.show();
    const param = <checkDuplicateParam>{
      division: this.data.assignedDivision,
      factory: this.data.assignedFactory,
      employeeID: this.data.assignedEmployeeID,
    };
    this._service.checkDuplicateCase3(param).subscribe({
      next: (isDuplicate) => {
        this.spinnerService.hide();
        this.invalidCase3 = isDuplicate;
        this.functionUtility.snotifySuccessError(!this.invalidCase3, !this.invalidCase3 ?
          'Assigned/Supported Division,Assigned/Supported Factory,Assigned/Supported Employee ID are valid' :
          'Assigned/Supported Division,Assigned/Supported Factory,Assigned/Supported Employee ID are exist')
      }
    });
  }

  checkCase4() {
    if (
      this.functionUtility.checkEmpty(this.data.factory) ||
      this.functionUtility.checkEmpty(this.data.employeeID)
    ) {
      this.invalidCase4 = false;
      return true;
    }
    if (this.tranfer.mode == this.mode.query) return true;

    if (!this.data.employeeID.startsWith(`${this.data.factory}-`)) {
      this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.CantFindTheCorresponding')
      this.invalidCase4 = true;
      return false;
    }
    if (
      this.tranfer.mode == this.mode.rehire &&
      this.data.factory == this.tmp.factory &&
      this.data.employeeID == this.tmp.employeeID
    ) {
      this.functionUtility.snotifySuccessError(false, 'Factory, Employee ID are the same old')
      this.invalidCase4 = true;
      return false;
    }
    this.invalidCase4 = false;
    return true;
  }

  checkCase5() {
    if (this.functionUtility.checkEmpty(this.tmp.assignedEmployeeID))
      return true;
    if (
      this.functionUtility.checkEmpty(this.data.assignedFactory) ||
      this.functionUtility.checkEmpty(this.data.assignedEmployeeID)
    ) {
      this.invalidCase5 = false;
      return true;
    }

    if (this.tranfer.mode == this.mode.query) return true;

    if (!this.data.assignedEmployeeID.startsWith(`${this.data.assignedFactory}-`)) {
      this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.CantFindTheCorresponding')
      this.invalidCase4 = true;
      return false;
    }
    if (
      this.tranfer.mode == this.mode.rehire &&
      this.data.assignedFactory == this.tmp.assignedFactory &&
      this.data.assignedEmployeeID == this.tmp.assignedEmployeeID
    ) {
      this.invalidCase5 = true;
      this.functionUtility.snotifySuccessError(false, ' Assigned/Supported Factory, Assigned/Supported Employee ID are the same old')
      return false;
    }
    this.invalidCase5 = false;
    return true;
  }

  checkBlackList(param: CheckBlackList) {
    this._service.checkBlackList(param).subscribe({
      next: (existBlackList) => {
        if (existBlackList) this.functionUtility.snotifySuccessError(false, 'This person is blacklisted');
      }
    });
  }
  //#region OnChange

  onChangeNationality() {
    this.data.registeredProvinceDirectly = null;
    this.data.mailingProvinceDirectly = null;
    this.getListProvinceDirectly();
    if (
      !this.functionUtility.checkEmpty(this.data.nationality) &&
      !this.functionUtility.checkEmpty(this.data.identificationNumber) &&
      this.tranfer.mode == this.mode.add
    ) {
      const checkBlackListParam = <CheckBlackList>{
        nationality: this.data.nationality,
        identification_Number: this.data.identificationNumber,
      };
      this.checkBlackList(checkBlackListParam);
    }
  }

  onChangeIdentificationNumber() {
    if (
      !this.functionUtility.checkEmpty(this.data.nationality) &&
      !this.functionUtility.checkEmpty(this.data.identificationNumber) &&
      this.tranfer.mode == this.mode.add
    ) {
      const checkBlackListParam = <CheckBlackList>{
        nationality: this.data.nationality,
        identification_Number: this.data.identificationNumber,
      };
      this.checkBlackList(checkBlackListParam);
    }
    this.checkDuplicateCase1();
  }

  onChangeDivision() {
    this.data.factory = null;
    this.getListFactory();
    this.onChangeFactory();
  }

  onChangeFactory() {
    this.data.department = null;
    this.getListDepartmet();
    this.data.employeeID = !this.functionUtility.checkEmpty(this.data.factory)
      ? `${this.data.factory}-`
      : '';
    if (this.checkCase4()) this.checkDuplicateCase2();
  }

  onChangeAssignedDivision() {
    this.data.assignedFactory = null;
    this.getListAssignedFactory();
    this.onChangeAssignedFactory();
  }

  onChangeAssignedFactory() {
    this.data.assignedDepartment = null;
    this.getListAssignedDepartmet();
    this.data.assignedEmployeeID = !this.functionUtility.checkEmpty(
      this.data.assignedFactory
    )
      ? `${this.data.assignedFactory}-`
      : '';
    if (this.checkCase5()) this.checkDuplicateCase3();
  }

  onChangeRegisteredProvinceDirectly() {
    this.data.registeredCity = null;
    this.getListRegisteredCity();
    if (this.isSameRegisteredChecked) {
      this.data.mailingCity = null;
      this.data.mailingProvinceDirectly = this.data.registeredProvinceDirectly;
      this.getListMailingCity();
    }
  }

  onChangeRegisteredCity() {
    if (this.isSameRegisteredChecked) {
      this.data.mailingCity = this.data.registeredCity;
    }
  }

  onChangeRegisteredAddress() {
    if (this.isSameRegisteredChecked) {
      this.data.mailingAddress = this.data.registeredAddress;
    }
  }

  checkMailingDifferentFromRegistered() {
    return (
      this.data.mailingProvinceDirectly !== this.data.registeredProvinceDirectly ||
      this.data.mailingCity !== this.data.registeredCity ||
      this.data.mailingAddress !== this.data.registeredAddress
    );
  }

  onSameRegisteredChange(event: any) {
    this.isSameRegisteredChecked = event.target.checked;
    if (this.isSameRegisteredChecked) {
      this.data.mailingProvinceDirectly = this.data.registeredProvinceDirectly;
      this.data.mailingCity = this.data.registeredCity;
      this.data.mailingAddress = this.data.registeredAddress;
      this.getListMailingCity();
    } else {
      this.data.mailingProvinceDirectly = null;
      this.data.mailingCity = null;
      this.data.mailingAddress = null;
    }
  }

  onChangeMailingProvinceDirectly() {
    this.data.mailingCity = null;
    this.getListMailingCity();
    if (this.checkMailingDifferentFromRegistered())
      this.isSameRegisteredChecked = false;
    else
      this.isSameRegisteredChecked = true;
  }

  onChangeMailingCity() {
    if (this.checkMailingDifferentFromRegistered())
      this.isSameRegisteredChecked = false;
    else
      this.isSameRegisteredChecked = true;
  }

  onChangeMailingAddress() {
    if (this.checkMailingDifferentFromRegistered())
      this.isSameRegisteredChecked = false;
    else
      this.isSameRegisteredChecked = true;
  }

  onChangePositionGrade() {
    this.data.positionTitle = null;
    this.getListPositionTitle();
  }

  onChangeSwipeCardOption() {
    if (this.data.swipeCardOption) this.data.swipeCardNumber = '';
    else this.data.swipeCardNumber = 'NA';
  }

  onChangeEmployeeID() {
    this.data.employeeID = this.data.employeeID?.toUpperCase();
    if (this.checkCase4()) this.checkDuplicateCase2();
  }

  onChangeAssginedEmployeeID() {
    this.data.assignedEmployeeID = this.data.assignedEmployeeID?.toUpperCase();
    if (this.checkCase5()) this.checkDuplicateCase3();
  }

  onChangeCrossFactoryStatus(event: any, key: any) {
    this.listCrossFactoryStatus.forEach((x) => {
      x.optional = x.key === key && event.srcElement.checked;
    });
    this.data.crossFactoryStatus =
      this.listCrossFactoryStatus.find((x) => x.optional)?.key ?? null;
  }

  onChangeBloodType(event: any, key: any) {
    this.bloodType.forEach((x) => {
      x.optional = x.key === key && event.srcElement.checked;
    });
    this.data.bloodType = this.bloodType.find((x) => x.optional)?.key ?? null;
  }

  onChangeUnionMembership(event: any, key: any) {
    this.unionMembership.forEach((x) => {
      x.optional = x.key === key && event.srcElement.checked;
    });
    this.data.unionMembership = this.unionMembership.find((x) => x.optional)?.key ?? null;
  }
  //#endregion

  save() {
    this.data.issuedDateStr = this.functionUtility.getDateFormat(
      new Date(this.data.issuedDate)
    );
    this.data.dateOfBirthStr = this.functionUtility.getDateFormat(
      new Date(this.data.dateOfBirth)
    );
    this.data.onboardDateStr = this.functionUtility.getDateFormat(
      new Date(this.data.onboardDate)
    );
    this.data.seniorityStartDateStr = this.functionUtility.getDateFormat(
      new Date(this.data.seniorityStartDate)
    );
    this.data.dateOfGroupEmploymentStr = this.functionUtility.getDateFormat(
      new Date(this.data.dateOfGroupEmployment)
    );
    this.data.annualLeaveSeniorityStartDateStr =
      this.functionUtility.getDateFormat(
        new Date(this.data.annualLeaveSeniorityStartDate)
      );

    if (this.tranfer.mode == this.mode.add) {
      this.add();
    } else if (this.tranfer.mode == this.mode.edit) {
      this.edit();
    } else {
      this.rehire();
    }
  }

  add() {
    if (this.invalidCase1) {
      this.functionUtility.snotifySuccessError(false, 'Nationality and Identification Number are not available');
      return;
    }
    if (this.invalidCase2) {
      this.functionUtility.snotifySuccessError(false, 'Division, Factory, Employee ID are not available');
      return;
    }
    if (this.invalidCase3) {
      this.functionUtility.snotifySuccessError(false, 'Assigned/Supported Division,Assigned/Supported Factory,Assigned/Supported Employee ID are not available');
      return;
    }

    this.data.dateOfResignationStr = !this.functionUtility.checkEmpty(
      this.data.dateOfResignation
    )
      ? this.functionUtility.getDateFormat(
        new Date(this.data.dateOfResignation)
      )
      : '';
    this.spinnerService.show();
    this._service.add(this.data).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.CreateOKMsg' : res.error)

        if (res.isSuccess) {
          this.tranfer.mode = this.mode.edit;
          this.tranfer.useR_GUID = res.data;
          this.tranfer.nationality = this.data.nationality;
          this.tranfer.identificationNumber = this.data.identificationNumber;
          this.tranfer.division = this.data.division;
          this.tranfer.factory = this.data.factory;
          this.tranfer.employee_ID = this.data.employeeID;
          this.tranfer.localFullName = this.data.localFullName;
          this._service.tranferChange.emit(this.tranfer);
          this.getDetail();
        }
      }
    });
  }

  edit() {
    this.spinnerService.show();
    this._service.edit(this.data).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error)
      }
    });
  }

  rehire() {
    this.spinnerService.show();
    this._service.rehire(this.data).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error)
      }
    });
  }

  close() {
    this._service.parentFun.emit();
  }

  //#region Get List
  getListDivision() {
    this._service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }
  getListFactory() {
    this._service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListAssignedFactory() {
    this._service
      .getListFactory(this.data.assignedDivision)
      .subscribe({
        next: (res) => {
          this.listAssignedFactory = res;
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
  getListPermission() {
    this._service.getListPermission().subscribe({
      next: (res) => {
        this.listPermission = res;
      },
    });
  }
  getListIdentityType() {
    this._service.getListIdentityType().subscribe({
      next: (res) => {
        this.listIdentityType = res;
      },
    });
  }
  getListEducation() {
    this._service.getListEducation().subscribe({
      next: (res) => {
        this.listEducation = res;
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
  getListRestaurant() {
    this._service.getListRestaurant().subscribe({
      next: (res) => {
        this.listRestaurant = res;
      },
    });
  }
  getListReligion() {
    this._service.getListReligion().subscribe({
      next: (res) => {
        this.listReligion = res;
      },
    });
  }
  getListTransportationMethod() {
    this._service.getListTransportationMethod().subscribe({
      next: (res) => {
        this.listTransportationMethod = res;
      },
    });
  }
  getListVehicleType() {
    this._service.getListVehicleType().subscribe({
      next: (res) => {
        this.listVehicleType = res;
      },
    });
  }
  getListWorkLocation() {
    this._service.getListWorkLocation().subscribe({
      next: (res) => {
        this.listWorkLocation = res;
      },
    });
  }
  getListReasonResignation() {
    this._service.getListReasonResignation().subscribe({
      next: (res) => {
        this.listReasonResignation = res;
      },
    });
  }

  getListDepartmet() {
    this._service
      .getListDepartment(this.data.division, this.data.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }

  getListAssignedDepartmet() {
    this._service
      .getListDepartment(
        this.data.assignedDivision,
        this.data.assignedFactory,

      )
      .subscribe({
        next: (res) => {
          this.listAssignedDepartment = res;
        },
      });
  }
  getListProvinceDirectly() {
    this._service
      .getListProvinceDirectly(this.data.nationality)
      .subscribe({
        next: (res) => {
          this.listProvinceDirectly = res;
        },
      });
  }

  getListRegisteredCity() {
    this._service
      .getListCity(this.data.registeredProvinceDirectly)
      .subscribe({
        next: (res) => {
          this.listRegisteredCity = res;
        },
      });
  }

  getListMailingCity() {
    this._service
      .getListCity(this.data.mailingProvinceDirectly)
      .subscribe({
        next: (res) => {
          this.listMailingCity = res;
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

  getListPositionTitle() {
    this._service
      .getListPositionTitle(this.data.positionGrade ?? -1)
      .subscribe({
        next: (res) => {
          this.listPositionTitle = res;
        },
      });
  }

  getDepartmentSupervisor(useR_GUID: string) {
    this._service.getDepartmentSupervisor(useR_GUID).subscribe({
      next: (res) => {
        this.listDepartmentSupervisor = res;
      },
    });
  }

  getListWorkTypeShift() {
    this._service
      .getListWorkTypeShift()
      .subscribe({
        next: (res) => {
          this.listWorkTypeShift = res;
        },
      });
  }
  //#endregion
}
