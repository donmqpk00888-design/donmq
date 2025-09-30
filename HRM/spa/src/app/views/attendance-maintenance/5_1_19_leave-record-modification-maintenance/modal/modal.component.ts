import {
  AfterViewInit,
  Component,
  EventEmitter,
  input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Leave_Record_Modification_MaintenanceDto } from '@models/attendance-maintenance/5_1_19_leave-record-modification-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { S_5_1_19_LeaveRecordModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_19_leave-record-modification-maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
  };
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  data: Leave_Record_Modification_MaintenanceDto = <Leave_Record_Modification_MaintenanceDto>{};
  modalChange = new EventEmitter<Leave_Record_Modification_MaintenanceDto>();


  iconButton = IconButton;
  classButton = ClassButton;

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listLeave: KeyValuePair[] = [];

  constructor(
    private service: S_5_1_19_LeaveRecordModificationMaintenanceService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.data })

  open(data: Leave_Record_Modification_MaintenanceDto): void {
    this.data = structuredClone(data);
    this.data.leave_Date = new Date(this.data.leave_Date_Str)
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.loadDropdownList();
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.isSave = true
    this.directive.hide();
  }

  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
    this.getListLeave();
  }

  getListFactory() {
    this.commonService.getFactoryMain().subscribe({
      next: res => this.listFactory = res
    });
  }

  getListDepartment() {
    this.commonService.getListDepartment(this.data.factory).subscribe({
      next: res => this.listDepartment = res
    });
  }

  getListWorkShiftType() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => this.listWorkShiftType = res
    });
  }
  getListLeave() {
    this.service.GetListLeave().subscribe({
      next: res => this.listLeave = res
    });
  }
  onDataChange() {
    this.data.update_By = this.user.id
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date())
  }

  deleteProperty(name: string) {
    delete this.data[name]
  }
}
