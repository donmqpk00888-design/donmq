import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  HRMS_Att_Overtime_TempDto,
  HRMS_Att_Overtime_TempParam,
  OvertimeTemporarySource
} from '@models/attendance-maintenance/5_1_16_overtime_temporary_record_maintenance';
import { Pagination } from '@utilities/pagination-utility';
import { InjectBase } from '@utilities/inject-base-app';
import { IconButton } from '@constants/common.constants';
import { S_5_1_16_OvertimeTemporaryRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_16_overtime_temporary_record_maintenance.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  param: HRMS_Att_Overtime_TempParam = <HRMS_Att_Overtime_TempParam>{};
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10
  }
  source: HRMS_Att_Overtime_TempDto
  data: HRMS_Att_Overtime_TempDto[] = []
  factory: KeyValuePair[] = [];
  department: KeyValuePair[] = [];
  workShiftType: KeyValuePair[] = [];
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD'
  };
  isEdit: boolean
  constructor(private service: S_5_1_16_OvertimeTemporaryRecordMaintenanceService,
    private modalService: ModalService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.processData()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.save(res.data)
    })
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }
  ngOnDestroy(): void {
    this.service.setSource(<OvertimeTemporarySource>{
      isEdit: this.isEdit,
      source: this.source,
      paramQuery: this.param,
      dataMain: this.data,
      pagination: this.pagination
    });
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
  }

  getDataFromSource() {
    this.param = this.service.source().paramQuery;
    this.pagination = this.service.source().pagination;
    this.data = this.service.source().dataMain;
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && !this.functionUtility.checkEmpty(this.param.factory))
        this.getData()
      else
        this.clear(false)
    }
    this.loadDropdownList();
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.data = res.result
        this.pagination = res.pagination
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      },
    })
  }
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  //#region edit mode
  onEdit(item: HRMS_Att_Overtime_TempDto) {
    this.modalService.open(item);
  }
  save(item: HRMS_Att_Overtime_TempDto) {
    this.spinnerService.show();
    item.work_Shift_Type = item.work_Shift_Type.split(' - ')[0]
    this.service.update(item).subscribe({
      next: async res => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.getData(false)
          this.snotifyService.success(res.error,
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(res.error,
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }
  onDelete(item: HRMS_Att_Overtime_TempDto) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'), () => {
        this.spinnerService.show()
        this.service.delete(item).subscribe({
          next: (res) => {
            this.spinnerService.hide()
            if (res.isSuccess) {
              this.getData();
              this.snotifyService.success(
                this.translateService.instant('System.Message.DeleteOKMsg'),
                this.translateService.instant('System.Caption.Success'));
            }
            else {
              this.snotifyService.error(
                this.translateService.instant('System.Message.DeleteErrorMsg'),
                this.translateService.instant('System.Caption.Error'));
            }
          }
        })
      });
  }
  onSelectFactory() {
    this.deleteProperty('department')
    this.getListDepartment()
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.factory = res
      },
    })
  }
  getListDepartment() {
    if (this.param.factory)
      this.service.getListDepartment(this.param.factory).subscribe({
        next: res => {
          this.department = res
        }
      })
  }
  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: res => {
        this.workShiftType = res
      },
    })
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch);
  }

  clear(isClear: boolean) {
    if (isClear)
      this.snotifyService.success(
        this.translateService.instant('System.Message.ClearMsg'),
        this.translateService.instant('System.Caption.Success'));
    this.param = <HRMS_Att_Overtime_TempParam>{}
    this.data = []
    this.pagination.totalCount = 0;
  }

  pageChanged(event) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  startDrag() {
    const dragscroll = document.getElementById('dragscroll');
    if (dragscroll) {
      dragscroll.classList.add('dragging');
    }
  }

  stopDrag() {
    const dragscroll = document.getElementById('dragscroll');
    if (dragscroll) {
      dragscroll.classList.remove('dragging');
    }
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
    this.param[name] = this.functionUtility.isValidDate(this.param[`${name}_Date`])
      ? this.functionUtility.getDateFormat(new Date(this.param[`${name}_Date`]))
      : '';
  }
}
