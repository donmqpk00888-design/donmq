import { Component, OnInit } from '@angular/core';
import { LocalStorageConstants } from '@constants/local-storage.enum';
import { Part } from '@models/manage/team-management/part';
import { TeamManagementService } from '@services/manage/team-management.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-create-or-update',
  templateUrl: './create-or-update.component.html',
  styleUrls: ['./create-or-update.component.scss']
})
export class CreateOrUpdateComponent extends InjectBase implements OnInit {
  modalRef?: BsModalRef;
  part: Part = <Part>{
    deptID: 'All'
  };
  listDept: KeyValuePair[] = [];
  language: string = localStorage.getItem(LocalStorageConstants.LANG)?.toLowerCase();

  constructor(
    private teamManagementService: TeamManagementService,
    private modalService: BsModalService
  ) {
    super()
   }

  ngOnInit(): void {
    this.getAllDepartment();
  }

  getAllDepartment() {
    this.teamManagementService.getAllDepartment()
      .subscribe({
        next: (res) => {
          this.listDept = res;
          this.listDept.unshift({ key: 'All', value: this.translateService.instant('Manage.TeamManager.SelectDept') as string });
          this.reset();
        }, error: () => {
          this.snotifyService.error(this.translateService.instant('System.Message.UnknowError'), this.translateService.instant('System.Caption.Error'));
          this.spinnerService.hide();
        }
      })
  }

  saveAdd(type: string) {
    this.spinnerService.show();
    this.teamManagementService.create(this.part)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
            type === 'SAVE' ? this.close("SAVE") : this.reset();
          }
          else {
            this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
          }
        }, error: () => {
          this.snotifyService.error(this.translateService.instant('System.Message.UnknowError'), this.translateService.instant('System.Caption.Error'));
          this.spinnerService.hide();
        }
      })
  }

  saveUpdate() {
    this.spinnerService.show();
    this.teamManagementService.update(this.part)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.close("SAVE");
          }
          else {
            this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
          }
        }, error: () => {
          this.snotifyService.error(this.translateService.instant('System.Message.UnknowError'), this.translateService.instant('System.Caption.Error'));
          this.spinnerService.hide();
        }
      })
  }

  close(type: string) {
    type === "CLOSE" ? this.teamManagementService.emitDataChange(false) : this.teamManagementService.emitDataChange(true);
    this.modalService.hide();
  }

  reset() {
    this.teamManagementService.currentPart.subscribe(data => {
      this.part = { ...data };
      if (this.part.type == 'UPDATE')
        this.part.deptID = this.listDept.find(x => x.key == data.deptID)?.key;
      else
        this.part.deptID = 'All';
    });
  }
}
