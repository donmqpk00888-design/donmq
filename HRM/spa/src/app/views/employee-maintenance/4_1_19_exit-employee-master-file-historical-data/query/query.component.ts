import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ExitEmployeeMasterFileHistoricalDataDto } from '@models/employee-maintenance/4_1_19_exit-employee-master-file-historical-data';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_19_ExitEmployeeMasterFileHistoricalDataService } from '@services/employee-maintenance/s_4_1_19_exit-employee-master-historical-data.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-query',
  templateUrl: './query.component.html',
  styleUrls: ['./query.component.scss'],
})
export class QueryComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  data: ExitEmployeeMasterFileHistoricalDataDto = <ExitEmployeeMasterFileHistoricalDataDto>{};
  url: string = '';
  action: string = '';
  useR_GUID: string = '';
  resignDate: string = '';
  title: string;
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
  listWorkTypeShift: KeyValuePair[] = [];
  deletionCode: KeyValuePair[] = [
    { key: 'Y', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Onjob' },
    { key: 'N', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Resigned' },
    { key: 'U', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Unpaid' },
  ]
  employmentStatus: KeyValuePair[] = [
    { key: 'A', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Assigned' },
    { key: 'S', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Supported' },
  ];
  gender: KeyValuePair[] = [
    { key: 'F', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Female' },
    { key: 'M', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Male' },
  ];
  bloodType: KeyValuePair[] = [
    { key: 'A', value: 'A' },
    { key: 'B', value: 'B' },
    { key: 'C', value: 'AB' },
    { key: 'O', value: 'O' },
  ];
  maritalStatus: KeyValuePair[] = [
    { key: 'M', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Married' },
    { key: 'U', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Unmarried' },
    { key: 'O', value: 'EmployeeInformationModule.ExitEmployeeMasterFileHistoricalData.Other' },
  ];
  swipeCardOption: KeyValuePair[] = [
    { key: true, value: 'Y.刷卡' },
    { key: false, value: 'N.不需刷卡' },
  ];
  unionMembership: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' },
  ];
  blacklist: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' },
  ];
  constructor(
    private _service: S_4_1_19_ExitEmployeeMasterFileHistoricalDataService
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
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((role) => {
      this.action = role.title;
    });

    this._service.paramForm
      .subscribe((res) => {
        if (this.action == 'Query') {
          if (res != null) {
            this.useR_GUID = res.useR_GUID;
            this.resignDate = res.resignDate;
            this.getDetail();
          } else this.back();
        }
      })
      ;
    this.loadDropDownList();
  }

  ngOnDestroy(): void {
    this._service.setParamForm(null);
  }

  getDetail() {
    this.spinnerService.show();
    this._service.getDetail(this.useR_GUID, this.resignDate).subscribe({
      next: (res) => {
        if (this.functionUtility.isEmptyObject(res)) this.back();
        this.data = res;
        this.getListFactory();
        this.getListAssignedFactory();
        this.getListDepartmet();
        this.getListAssignedDepartmet();
        this.getListProvinceDirectly();
        this.getListRegisteredCity();
        this.getListMailingCity();
        this.getListPositionTitle();
        this.spinnerService.hide();
      }
    });
  }

  loadDropDownList() {
    this.getListDivision();
    this.getListNationality();
    this.getListPermission();
    this.getListIdentityType();
    this.getListEducation();
    this.getListWorkType();
    this.getListRestaurant();
    this.getListReligion();
    this.getListTransportationMethod();
    this.getListVehicleType();
    this.getListWorkLocation();
    this.getListReasonResignation();
    this.getListPositionGrade();
    this.getListFactory();
    this.getListAssignedFactory();
    this.getListPositionTitle();
    this.getListDepartmet();
    this.getListAssignedDepartmet();
    this.getListWorkTypeShift();
  }

  back = () => this.router.navigate([this.url]);

  //#region Get List
  getListNationality() {
    this._service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }
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

  getListDepartmet() {
    this._service
      .getListDepartment(this.data.division, this.data.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
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
  //#endregion
}
