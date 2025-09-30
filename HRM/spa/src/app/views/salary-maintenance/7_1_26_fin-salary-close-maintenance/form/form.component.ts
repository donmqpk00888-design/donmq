import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { FinSalaryCloseMaintenance_MainData, FinSalaryCloseMaintenance_UpdateParam } from '@models/salary-maintenance/7_1_26_fin-salary-close-maintenance';
import { S_7_1_26_FinSalaryCloseMaintenanceService } from '@services/salary-maintenance/s-7-1-26-fin-salary-close-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = ''
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }

  url: string = ''
  action: string = ''
  list_Close_Status: KeyValuePair[] = [
    {
      key: 'Y',
      value: 'Y',
    },
    {
      key: 'N',
      value: 'N',
    },
  ];

  data: FinSalaryCloseMaintenance_MainData = <FinSalaryCloseMaintenance_MainData>{}
  iconButton = IconButton;
  classButton = ClassButton;

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  constructor(private _service: S_7_1_26_FinSalaryCloseMaintenanceService) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList()
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url)
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title
      this.getSource()
    })
  }
  getSource() {
    const source = this._service.programSource();
    if (source.selectedData && Object.keys(source.selectedData).length > 0) {
      this.data = structuredClone(source.selectedData)
      this.loadDropdownList()
    } else this.back()
  }

  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  getListFactory() {
    this._service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    })
  }

  getListDepartment() {
    if (this.data.factory)
      this._service.GetDepartment(this.data.factory)
        .subscribe({
          next: (res) => {
            this.listDepartment = res;
          },
        });
  }

  getListPermissionGroup() {
    this._service.getListPermissionGroup(this.data.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
      }
    })
  }
  back = () => this.router.navigate([this.url])
  save() {
    this.spinnerService.show();
    this._service.update(this.data).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
        if (res.isSuccess)
          this.back()
      }
    })
  }
}
