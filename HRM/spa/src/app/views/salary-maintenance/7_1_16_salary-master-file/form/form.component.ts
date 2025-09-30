import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { SalaryItem, SalaryMasterFile_Main } from '@models/salary-maintenance/7_1_16_salary-master-file';
import { S_7_1_16_SalaryMasterFileService } from '@services/salary-maintenance/s_7_1_16_salary-master-file.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  method: string = "";
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPositionTitle: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listTechnicalType: KeyValuePair[] = [];
  listExpertiseCategory: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  data: SalaryMasterFile_Main = <SalaryMasterFile_Main>{};
  iconButton = IconButton;
  classButton = ClassButton;
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryMaintenance.SalaryMasterFile.Onjob' },
    { key: 'N', value: 'SalaryMaintenance.SalaryMasterFile.Resigned' },
    { key: 'U', value: 'SalaryMaintenance.SalaryMasterFile.Unpaid' },
  ];
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  total_Salary: number = 0;
  salaryItems: SalaryItem[] = [];
  constructor(private service: S_7_1_16_SalaryMasterFileService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList();
      this.getDataQueryPage();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.method = role.title
        this.getSource();
      });
  }
  getSource() {
    const source = this.service.signalDataMain();
    if (source.selectedData && Object.keys(source.selectedData).length > 0) {
      this.data = structuredClone(source.selectedData)
      this.loadDropdownList()
      this.getDataQueryPage()
    } else this.back()
  }

  back = () => this.router.navigate([this.url]);

  loadDropdownList() {
    this.getFactorys();
    this.getPositionTitles();
    this.getSalaryTypes();
    this.getTechnicalTypes();
    this.getExpertiseCategorys();
    this.getPermissionGroups();
    if (this.data.factory) {
      this.getDepartments();
    }
  }

  getTechnicalTypes() {
    this.service.getTechnicalTypes().subscribe({
      next: res => {
        this.listTechnicalType = res;
      }
    });
  }

  getExpertiseCategorys() {
    this.service.getExpertiseCategorys().subscribe({
      next: res => {
        this.listExpertiseCategory = res;
      }
    });
  }

  getFactorys() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getPermissionGroups() {
    this.service.getListPermissionGroup().subscribe({
      next: result => {
        this.permissionGroups = result;
      }
    })
  }

  getDepartments() {
    this.service.getDepartments(this.data.factory,).subscribe({
      next: res => {
        this.listDepartment = res;
      }
    });
  }

  getPositionTitles() {
    this.service.getPositionTitles().subscribe({
      next: res => {
        this.listPositionTitle = res;
      }
    });
  }

  getSalaryTypes() {
    this.service.getSalaryTypes().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }

  getDataQueryPage() {
    this.spinnerService.show();
    this.service.getDataQueryPage(this.pagination, this.data.factory, this.data.employee_ID,).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.salaryItems = res.salaryItemsPagination.result;
        this.pagination = res.salaryItemsPagination.pagination;
        this.total_Salary = res.total_Salary;
      }
    })
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getDataQueryPage();
  }

}
