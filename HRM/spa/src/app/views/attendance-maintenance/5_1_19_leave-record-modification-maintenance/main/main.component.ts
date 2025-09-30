import { Component, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  Leave_Record_Modification_MaintenanceDto,
  HRMS_Att_Leave_MaintainSearchParamDto,
  LeaveRecordModificationMaintenanceSource
} from '@models/attendance-maintenance/5_1_19_leave-record-modification-maintenance';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { CaptionConstants } from '@constants/message.enum';
import { S_5_1_19_LeaveRecordModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_19_leave-record-modification-maintenance.service'
import { ModalService } from '@services/modal.service';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  editMode: boolean = false;
  isEdit: boolean = false;
  colSpan: number = 0;
  itemBackup: Leave_Record_Modification_MaintenanceDto;
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listLeave: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  params: HRMS_Att_Leave_MaintainSearchParamDto = <HRMS_Att_Leave_MaintainSearchParamDto>{};
  pagination: Pagination = <Pagination>{
    pageNumber: 1, pageSize: 10, totalPage: 0,
    totalCount: 0
  };
  dataMain: Leave_Record_Modification_MaintenanceDto[] = [];
  selectedData: Leave_Record_Modification_MaintenanceDto = <Leave_Record_Modification_MaintenanceDto>{};
  source: LeaveRecordModificationMaintenanceSource
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM/DD",
    minMode: "day"
  };

  constructor(
    private service: S_5_1_19_LeaveRecordModificationMaintenanceService,
    private modalService: ModalService
  ) {
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
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }

  ngOnDestroy(): void {
    this.service.setSource(<LeaveRecordModificationMaintenanceSource>{
      param: this.params,
      data: this.dataMain,
      pagination: this.pagination,
      selectedData: this.selectedData
    });
  }

  getDataFromSource() {
    this.params = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    this.processData()
  }
  processData() {
    if (this.dataMain.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.params.factory)
        this.getData(false)
      else
        this.clear()
    }
    this.loadDropdownList();
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
    this.getListLeave();
    this.getListPermissionGroup();
  }
  search = () => {
    this.pagination.pageNumber == 1 ? this.getData(true) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }
  getData = (isSearch?: boolean) => {
    return new Promise<void>((resolve, reject) => {
      // handle input
      if (this.params.date_Start && !this.params.date_End || !this.params.date_Start && this.params.date_End) {
        this.snotifyService.warning('Please enter Maternity Leave Start and End', CaptionConstants.WARNING);
        reject()
      }
      else {
        this.spinnerService.show();
        this.service.getData(this.pagination, this.params).subscribe({
          next: (res) => {
            this.dataMain = res.result;
            this.pagination = res.pagination;
            if (isSearch)
              this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
            this.spinnerService.hide()
            resolve()
          },
          error: () => { reject() }
        })
      }
    })
  };

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onDelete(item: Leave_Record_Modification_MaintenanceDto) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Caption.Confirm'), async () => {
        this.spinnerService.show()
        this.service.delete(item).subscribe({
          next: res => {
            this.spinnerService.hide()
            if (res.isSuccess) {
              this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
              this.getData();
            } else {
              this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
            }
          }
        });
      });
  }

  clear() {
    this.dataMain = [];
    this.params = <HRMS_Att_Leave_MaintainSearchParamDto>{};
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

  getListLeave() {
    this.service.GetListLeave().subscribe({
      next: res => {
        this.listLeave = res;
      }
    });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup().subscribe({
      next: res => {
        this.listPermissionGroup = res;
      }
    });
  }

  //#region edit mode
  onEdit(item: Leave_Record_Modification_MaintenanceDto) {
    this.spinnerService.show();
    this.service.checkExistedData(item)
      .subscribe({
        next: (res: any) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.modalService.open(item);
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`EmployeeInformationModule.DocumentManagement.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }

  save(item: Leave_Record_Modification_MaintenanceDto) {
    this.spinnerService.show();
    this.service.edit(item).subscribe({
      next: async res => {
        if (res.isSuccess) {
          await this.getData(false)
          this.snotifyService.success(res.error,
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(res.error,
            this.translateService.instant('System.Caption.Error')
          );
        }
        this.spinnerService.hide();
      }
    });
  }
  deleteProperty(name: string) {
    delete this.params[name]
  }

  download() {
    if (this.dataMain.length == 0)
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.NoData'),
        this.translateService.instant('System.Caption.Warning'));
    this.spinnerService.show();
    this.service.download(this.params).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }
  onDateChange(name: string) {
    this.params[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.params[name]))
      ? this.functionUtility.getDateFormat(new Date(this.params[name]))
      : '';
  }
}

