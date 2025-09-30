import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Emp_Unpaid_LeaveDto, UnpaidLeaveParam, UnpaidLeaveSource } from '@models/employee-maintenance/4_1_11_unpaid-leave';
import { S_4_1_11_UnpaidLeaveService } from '@services/employee-maintenance/s_4_1_11_unpaid-leave.service';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { Observable } from 'rxjs';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listLeaveReason: KeyValuePair[] = [];
  employee_ID: string[] = []
  nameFunction: string[] = [];
  iconButton = IconButton;
  data: HRMS_Emp_Unpaid_LeaveDto[] = [];
  param: UnpaidLeaveParam = <UnpaidLeaveParam>{}
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };
  onboardDate: Date;
  leaveStartFrom: Date;
  leaveStartTo: Date;
  leaveEndFrom: Date;
  leaveEndTo: Date;

  constructor(
    private service: S_4_1_11_UnpaidLeaveService,
    private modalService: ModalService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      this.getListLeaveReason();
      this.getListDepartment();
      if (this.data.length > 0)
        this.getData()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave && (res.isEdit || (!res.isEdit && this.functionUtility.checkFunction('Search') && this.checkRequiredParams())))
        this.getData()
    })
    effect(() => {
      const { param, onboardDate, leaveStartFrom, leaveStartTo, leaveEndFrom, leaveEndTo, data, pagination } = this.service.paramSearch();
      this.param = param;
      this.onboardDate = onboardDate;
      this.leaveStartFrom = leaveStartFrom;
      this.leaveStartTo = leaveStartTo;
      this.leaveEndFrom = leaveEndFrom;
      this.leaveEndTo = leaveEndTo;
      this.pagination = pagination;
      this.data = data;

      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getListDivision();
        this.getListFactory();
        this.getListDepartment();
      }
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear()
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListDivision();
    this.getListLeaveReason();
    this.getListDepartment();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<UnpaidLeaveSource>{
      param: this.param,
      onboardDate: this.onboardDate,
      leaveStartFrom: this.leaveStartFrom,
      leaveStartTo: this.leaveStartTo,
      leaveEndFrom: this.leaveEndFrom,
      leaveEndTo: this.leaveEndTo,
      pagination: this.pagination,
      data: this.data
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.param.onboard_Date = this.formatDate(this.onboardDate);
    this.param.leave_Start_From = this.formatDate(this.leaveStartFrom);
    this.param.leave_Start_To = this.formatDate(this.leaveStartTo);
    this.param.leave_End_From = this.formatDate(this.leaveEndFrom);
    this.param.leave_End_To = this.formatDate(this.leaveEndTo);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  download() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        this.spinnerService.hide();
      }
    });
  }

  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <UnpaidLeaveParam>{};
    this.listFactory = [];
    this.listDepartment = [];
    this.onboardDate = null;
    this.leaveStartFrom = null;
    this.leaveStartTo = null;
    this.leaveEndFrom = null;
    this.leaveEndTo = null;
    this.data = [];
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }


  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  getListDivision() {
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getListLeaveReason() {
    this.getListData('listLeaveReason', this.service.getListLeaveReason.bind(this.service));
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      }
    });
  }

  onDivisionChange() {
    this.deleteProperty('factory');
    this.deleteProperty('department');
    this.listDepartment = [];
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  onFactoryChange() {
    this.deleteProperty('department');
    this.getListDepartment();
  }

  getListDepartment() {
    if (this.param.division && this.param.factory)
      this.service.getListDepartment(this.param.division, this.param.factory)
        .subscribe({
          next: (res) => {
            this.listDepartment = res;
          },
        });
  }

  add() {
    const data = {
      model: <HRMS_Emp_Unpaid_LeaveDto>{
        continuation_of_Insurance: false,
        seniority_Retention: false,
        annual_Leave_Seniority_Retention: false,
        effective_Status: false,
      },
      isEdit: false,
    }
    this.modalService.open(data);
  }

  edit(item: HRMS_Emp_Unpaid_LeaveDto) {
    const data = {
      model: <HRMS_Emp_Unpaid_LeaveDto>{
        ...item,
        leaveStartDate: this.formatDate(new Date(item.leave_Start)),
        leaveEndDate: this.formatDate(new Date(item.leave_End))
      },
      isEdit: true,
    }
    this.modalService.open(data);
  }

  delete(item: HRMS_Emp_Unpaid_LeaveDto) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) this.getData();
        }
      })
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
