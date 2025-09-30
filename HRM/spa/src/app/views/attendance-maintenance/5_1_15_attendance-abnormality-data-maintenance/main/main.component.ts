import {
  AttendanceAbnormalityDataMaintenanceParam,
  AttendanceAbnormalityDataMaintenanceSource,
  HRMS_Att_Temp_RecordDto,
} from '@models/attendance-maintenance/5_1_15_attendance-abnormality-data-maintenance';
import { S_5_1_15_AttendanceAbnormalityDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_15_attendance-abnormality-data-maintenance.service';
import { AfterViewChecked, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { Observable } from 'rxjs';
import {
  BsDatepickerConfig,
  BsDatepickerViewMode,
} from 'ngx-bootstrap/datepicker';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalService } from '@services/modal.service';
import { NgForm, FormGroup } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('mainForm') public mainForm: NgForm;

  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
  };
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listUpdateReason: KeyValuePair[] = [];
  data: HRMS_Att_Temp_RecordDto[] = [];
  param: AttendanceAbnormalityDataMaintenanceParam = <AttendanceAbnormalityDataMaintenanceParam>{};
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode,
  };
  allowGetData: boolean = false

  constructor(
    private service: S_5_1_15_AttendanceAbnormalityDataMaintenanceService,
    private modalService: ModalService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.processData()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.save(res.data as HRMS_Att_Temp_RecordDto);
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }

  getSource() {
    const { param, data, pagination } = this.service.paramSearch();
    this.param = param;
    this.pagination = pagination;
    this.data = data;
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search'))
        this.allowGetData = true
      else {
        this.clear()
        this.allowGetData = false
      }
    }
    this.getDropdownList()
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<AttendanceAbnormalityDataMaintenanceSource>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
    });
  }
  getDropdownList() {
    this.getListFactory();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListUpdateReason();
    this.getListDepartment();
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
  //#region getList
  getListFactory() {
    this.getList(
      () => this.service.getListFactoryByUser(),
      this.listFactory
    );
  }

  getListDepartment() {
    if (this.param.factory)
      this.getList(
        () => this.service.getListDepartment(this.param.factory),
        this.listDepartment
      );
  }

  getListWorkShiftType() {
    this.getList(
      () => this.service.getListWorkShiftType(),
      this.listWorkShiftType
    );
  }
  getListAttendance() {
    this.service.getListAttendance().subscribe({
      next: (res) => {
        this.listAttendance = res;
        this.functionUtility.getNgSelectAllCheckbox(this.listAttendance)
        this.param.list_Attendance = this.listAttendance.map(x => x.key)
      },
    });
  }
  getListUpdateReason() {
    this.getList(
      () => this.service.getListUpdateReason(),
      this.listUpdateReason
    );
  }

  getList(
    serviceMethod: () => Observable<KeyValuePair[]>,
    resultList: KeyValuePair[]
  ) {
    serviceMethod().subscribe({
      next: (res) => {
        resultList.length = 0;
        resultList.push(...res);
      }
    });
  }
  //#endregion

  //#region onChange
  onFactoryChange() {
    this.deleteProperty('department');
    this.getListDepartment();
  }
  //#endregion

  //#region getData
  getData(isSearch?: boolean, isDelete?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch) this.handleSuccess('System.Message.QuerySuccess');
        if (isDelete) this.handleSuccess('System.Message.DeleteOKMsg');
      },
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch);
  }
  //#endregion

  //#region clear
  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <AttendanceAbnormalityDataMaintenanceParam>{};
    this.listDepartment = [];
    this.data = [];
  }
  //#endregion

  //#region add & edit & delete
  add = () => this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);

  edit = (item: HRMS_Att_Temp_RecordDto) => this.modalService.open(item);

  save(item: HRMS_Att_Temp_RecordDto) {
    this.spinnerService.show();
    this.service.edit(item).subscribe({
      next: async (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.getData(false);
          this.snotifyService.success(
            res.error,
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(
            res.error,
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }
  delete(item: HRMS_Att_Temp_RecordDto, isDelete: boolean) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service.delete(item).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) this.getData(false, isDelete);
            else this.handleError('System.Message.DeleteErrorMsg');
          }
        });
      }
    );
  }
  //#endregion

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  handleSuccess(message: string) {
    this.spinnerService.hide();
    this.snotifyService.success(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Success')
    );
  }

  handleError(message: string) {
    this.spinnerService.hide();
    this.snotifyService.error(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Error')
    );
  }

  startDrag(): void {
    document.getElementById('dragscroll')?.classList.add('dragging');
  }

  stopDrag(): void {
    document.getElementById('dragscroll')?.classList.remove('dragging');
  }

  deleteProperty(name: string) {
    delete this.param[name];
  }

  download() {
    if (this.data.length == 0)
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.param[name]))
      ? this.functionUtility.getDateFormat(new Date(this.param[name]))
      : '';
  }
}
