import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { MonthlySalaryMasterFileBackupQueryDto, SalaryDetailDto } from '@models/salary-maintenance/7_1_17_monthly-salary-master-file-backup-query';
import { S_7_1_17_MonthlySalaryMasterFileBackupQueryService } from '@services/salary-maintenance/s_7_1_17_monthly-salary-master-file-backup-query.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  url: string = '';
  action: string = '';

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryMaintenance.MonthlySalaryMasterFileBackupQuery.OnJob' },
    { key: 'N', value: 'SalaryMaintenance.MonthlySalaryMasterFileBackupQuery.Resigned' },
    { key: 'U', value: 'SalaryMaintenance.MonthlySalaryMasterFileBackupQuery.Unpaid' }
  ];
  listPositionTitle: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listTechnicalType: KeyValuePair[] = [];
  listExpertiseCategory: KeyValuePair[] = [];
  data: MonthlySalaryMasterFileBackupQueryDto = <MonthlySalaryMasterFileBackupQueryDto>{}
  salaryDetails: SalaryDetailDto[] = [];
  total_Salary: number;
  yearMonth: Date;
  iconButton = IconButton;
  classButton = ClassButton;

  constructor(private service: S_7_1_17_MonthlySalaryMasterFileBackupQueryService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList()
      this.getSalaryDetails();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
      this.getSource()
    });
  }
  getSource() {
    const source = this.service.paramSearch();
    if (source.selectedData && Object.keys(source.selectedData).length > 0) {
      this.data = structuredClone(source.selectedData)
      this.loadDropdownList()
      this.getSalaryDetails()
    } else this.back()
  }

  //#region getSalaryDetails
  getSalaryDetails() {
    this.spinnerService.show();
    this.service.getSalaryDetails(this.pagination, this.data.probation, this.data.factory, this.data.employee_ID, this.data.yearMonth).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res) {
          this.salaryDetails = res.result;
          this.pagination = res.pagination
          this.total_Salary = this.salaryDetails.reduce((sum, item) => sum + item.amount, 0);
        } else {
          this.salaryDetails = [];
          this.total_Salary = 0;
        }
      }
    });
  }

  //#endregion

  //#region getList
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPositionTitle();
    this.getListPermissionGroup();
    this.getListSalaryType();
    this.getListTechnicalType();
    this.getListExpertiseCategory();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    })
  }

  getListDepartment() {
    if (this.data.factory)
      this.service.getListDepartment(this.data.factory)
        .subscribe({
          next: (res) => {
            this.listDepartment = res;
          },
        });
  }

  getListPositionTitle() {
    this.getListData('listPositionTitle', this.service.getListPositionTitle.bind(this.service));
  }

  getListPermissionGroup() {
    this.getListData('listPermissionGroup', this.service.getListPermissionGroup.bind(this.service));
  }

  getListSalaryType() {
    this.getListData('listSalaryType', this.service.getListSalaryType.bind(this.service));
  }

  getListTechnicalType() {
    this.getListData('listTechnicalType', this.service.getListTechnicalType.bind(this.service));
  }

  getListExpertiseCategory() {
    this.getListData('listExpertiseCategory', this.service.getListExpertiseCategory.bind(this.service));
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      }
    });
  }
  //#endregion

  //#region pageChanged
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getSalaryDetails();
  }
  //#endregion

  back = () => this.router.navigate([this.url]);
}
