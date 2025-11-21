import { Component, OnInit } from '@angular/core';
import { Departments } from '@models/manage/departments';
import { DepartmentLangParam } from '@params/manage/departmentLangParam';
import { DepartmentService } from '@services/manage/Department.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { InjectBase } from '@utilities/inject-base-app';

@Component({
  selector: 'app-department-add',
  templateUrl: './department-add.component.html',
  styleUrls: ['./department-add.component.scss'],
})
export class DepartmentAddComponent extends InjectBase implements OnInit {
  areaList: KeyValuePair[] = [];
  buildingList: KeyValuePair[] = [];
  addDepartment: Departments = <Departments>{};
  detpLang: DepartmentLangParam = <DepartmentLangParam>{};
  constructor(
    private _service: DepartmentService,
  ) {
    super();
  }
  ngOnInit(): void {
    this.getAllAreas();
    this.getAllBuldings();
  }
  getAllAreas() {
    this._service.getAllAreas().subscribe({
      next: (res) => {
        this.areaList = res;
      },
    });
  }
  getAllBuldings() {
    this._service.getAllBuildings().subscribe({
      next: (res) => {
        this.buildingList = res;
      },
    });
  }
  save() {
    this.spinnerService.show();
    this._service.add(this.addDepartment).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
          this.router.navigate(['manage/department']);
        } else {
          this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
        }
        this.spinnerService.hide();
      },
      error: () => {
        this.spinnerService.hide();
        this.snotifyService.error(this.translateService.instant('System.Message.SystemError'), this.translateService.instant('System.Caption.Error'));
      },
      complete: () => {
        this.spinnerService.hide();
      }
    });
  }
}
