import { Component, EventEmitter, input, OnInit, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Emp_BlacklistDto } from '@models/employee-maintenance/4_1_13_exit-employees-blacklist';
import { S_4_1_13_ExitEmployeesBlacklistService } from '@services/employee-maintenance/s_4_1_13_exit-employees-blacklist.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent extends InjectBase implements OnInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  backEmitter: EventEmitter<boolean> = new EventEmitter();
  title: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  model: HRMS_Emp_BlacklistDto = <HRMS_Emp_BlacklistDto>{};
  iconButton = IconButton;
  constructor(
    private service: S_4_1_13_ExitEmployeesBlacklistService,
    private modalService: ModalService
  ) {
    super();
  }

  ngOnInit(): void { }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: HRMS_Emp_BlacklistDto): void {
    this.model = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.spinnerService.show();
    this.isSave = true
    this.service.edit(this.model).subscribe({
      next: res => {
        this.functionUtility.snotifySuccessError(
          res.isSuccess,
          res.isSuccess ? 'System.Message.UpdateOKMsg' : 'System.Message.UpdateErrorMsg')
        if (res.isSuccess) this.directive.hide();
        this.spinnerService.hide();
      }
    })
  }
}
