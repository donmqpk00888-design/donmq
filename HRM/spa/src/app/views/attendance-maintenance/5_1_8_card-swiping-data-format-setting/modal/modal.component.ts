import { AfterViewInit, Component, EventEmitter, input, OnDestroy, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Swipecard_SetDto } from '@models/attendance-maintenance/5_1_8_hrms_att_swipecard_set';
import { S_5_1_8_CardSwipingDataFormatSettingService } from '@services/attendance-maintenance/s_5_1_8_card-swiping-data-format-setting.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css'
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  backEmitter: EventEmitter<{ factory: string, isSuccess: boolean }> = new EventEmitter();
  iconButton = IconButton;
  title: string = '';
  action: string = '';
  userName: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  factory: string;
  data: HRMS_Att_Swipecard_SetDto = <HRMS_Att_Swipecard_SetDto>{};
  factorys: KeyValuePair[] = [];

  constructor(
    private service: S_5_1_8_CardSwipingDataFormatSettingService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }
  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, factory: this.factory })

  open(data: any): void {
    const source = structuredClone(data);
    this.factory = source.factory
    this.action = source.action
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.data = <HRMS_Att_Swipecard_SetDto>{};
    this.getListFactory();
    if (this.action == 'Edit')
      this.getByFactory();
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.spinnerService.show();
    this.isSave = true
    this.data.update_By = this.userName
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
    let action: Observable<OperationResult> = this.action == 'Edit'
      ? this.service.edit(this.data)
      : this.service.addNew(this.data)
    action.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: any) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          const message = this.action == 'Edit' ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
          this.functionUtility.snotifySuccessError(true, message);
          this.directive.hide()
        } else {
          this.functionUtility.snotifySuccessError(false, result.error)
        }
      }
    });
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  getByFactory() {
    if (this.factory)
      this.service.getByFactory(this.factory).subscribe({
        next: (res) => this.data = res,
      })
  }

  getListFactory() {
    this.service.getByFactoryAddList().subscribe({
      next: res => this.factorys = res
    });
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
