import { Component, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  AttendanceChangeRecordMaintenanceSource,
  HRMS_Att_Change_RecordDto,
  HRMS_Att_Change_Record_Delete_Params,
  HRMS_Att_Change_Record_Params
} from '@models/attendance-maintenance/5_1_18_attendance-change-record-maintenance';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { CaptionConstants } from '@constants/message.enum';
import { S_5_1_18_AttendanceChangeRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_18_attendance-change-record-maintenance.service'
import { ModalService } from '@services/modal.service';
import { NgForm, FormGroup } from '@angular/forms';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('mainForm') public mainForm: NgForm;

  title: string = '';
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listReasonCode: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];

  params: HRMS_Att_Change_Record_Params = <HRMS_Att_Change_Record_Params>{};
  pagination: Pagination = <Pagination>{
    pageNumber: 1, pageSize: 10, totalPage: 0,
    totalCount: 0
  };
  dataMain: HRMS_Att_Change_RecordDto[] = [];

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM/DD",
    minMode: "day"
  };

  allowGetData: boolean = false

  constructor(
    private service: S_5_1_18_AttendanceChangeRecordMaintenanceService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.processData()
      });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.getData(false)
    })
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }
  getDataFromSource() {
    this.params = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    this.processData()
  }
  processData() {
    if (this.dataMain.length > 0) {
      if (this.functionUtility.checkFunction('Search'))
        this.allowGetData = true
      else {
        this.clear()
        this.allowGetData = false
      }
    }
    this.loadDropdownList();
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListReasonCode();
    this.getListHoliday();
  }
  ngAfterViewChecked() {
    if (this.allowGetData && this.mainForm) {
      const form: FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getData(false);
        this.allowGetData = false
      }
    }
  }
  ngOnDestroy(): void {
    this.service.setSource(<AttendanceChangeRecordMaintenanceSource>{
      pagination: this.pagination,
      param: this.params,
      data: this.dataMain
    });
  }



  search = (isSearch: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch, false) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }
  getData = (isSearch?: boolean, isDelete?: boolean) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.service.getData(this.pagination, this.params).subscribe({
        next: (res) => {
          this.dataMain = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
          if (isDelete)
            this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
          this.spinnerService.hide()
          resolve()
        },
        error: () => { reject() }
      })
    })
  };

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onDelete(item: HRMS_Att_Change_RecordDto, isDelete: boolean) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {

      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: res => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.getData(false, isDelete);
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
          }
        }
      });
    });
  }

  clear() {
    this.dataMain = [];
    this.params = <HRMS_Att_Change_Record_Params>{};
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
  }

  onChangeFactory() {
    this.deleteProperty('department')
    this.getListDepartment();
  }

  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getListDepartment() {
    this.commonService.getListDepartment(this.params.factory).subscribe({
      next: res => {
        this.listDepartment = res;
      }
    });
  }

  getListWorkShiftType() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => {
        this.listWorkShiftType = res;
      }
    });
  }

  getListAttendance() {
    this.commonService.getListAttendanceOrLeave().subscribe({
      next: res => {
        this.listAttendance = res;
      }
    });
  }

  getListReasonCode() {
    this.commonService.getListReasonCode().subscribe({
      next: res => {
        this.listReasonCode = res;
      }
    });
  }

  getListHoliday() {
    this.service.getListHoliday('39', 1, 'Attendance').subscribe({
      next: res => {
        this.listHoliday = res;
      }
    });
  }

  //#region edit mode
  onEdit(item: HRMS_Att_Change_RecordDto) {
    this.spinnerService.show();
    item.holiday = item.holiday.split(" - ")[0];
    this.service.checkExistedData(item)
      .subscribe({
        next: (res: any) => {
          this.spinnerService.hide();
          res.isSuccess
            ? this.modalService.open(item)
            : this.snotifyService.error(
              this.translateService.instant(`EmployeeInformationModule.DocumentManagement.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
        }
      });
  }
  deleteProperty(name: string) {
    delete this.params[name]
  }
  onDateChange(name: string) {
    this.params[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.params[name]))
      ? this.functionUtility.getDateFormat(new Date(this.params[name]))
      : '';
  }
}

