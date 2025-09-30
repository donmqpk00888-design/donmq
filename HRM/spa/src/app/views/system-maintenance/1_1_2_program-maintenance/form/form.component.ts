import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  ProgramMaintenance_Data,
  ProgramMaintenance_Memory,
} from '@models/system-maintenance/1_1_2-program-maintenance';
import { S_1_1_2_ProgramMaintenanceService } from '@services/system-maintenance/s_1_1_2_program-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { AuthService } from '@services/auth/auth.service';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  source: ProgramMaintenance_Memory = <ProgramMaintenance_Memory>{};
  iconButton = IconButton;
  listFunction_Code: KeyValuePair[] = [];
  ListDirectory: KeyValuePair[] = [];
  param: ProgramMaintenance_Data = <ProgramMaintenance_Data>{ functions: [] };
  title: string = '';
  url: string = '';
  action: string = '';
  constructor(
    private service: S_1_1_2_ProgramMaintenanceService,
    private authService: AuthService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource();
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListFuntion();
    this.getDirectory();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.action = res.title;
    });
  }
  getDataFromSource() {
    effect(() => {
      let source = this.service.param();
      if (this.action == 'Edit') {
        if (source.selectedData && Object.keys(source.selectedData).length > 0) {
          this.param = structuredClone(source.selectedData);
        }
        else
          this.back()
      }
    })
  }
  cancel = () => this.back();

  back = () => this.router.navigate([this.url]);

  getListFuntion = () => this.service.getFunction_ALL().subscribe({
    next: (res) => {
      this.listFunction_Code = res
      this.functionUtility.getNgSelectAllCheckbox(this.listFunction_Code)
    }
  });

  getDirectory = () => this.service.getDirectory().subscribe({ next: (res) => this.ListDirectory = res });

  saveChange() {
    this.param.program_Name = this.param.program_Name.trim();
    this.param.program_Code = this.param.program_Code.trim();
    if (this.functionUtility.checkEmpty(this.param.seq))
      this.param.seq = null
    this.spinnerService.show();
    if (this.action != 'Edit') {
      this.service.addNew(this.param).subscribe({
        next: (result) => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
            this.back();
          } else
            this.snotifyService.error(
              this.translateService.instant('System.Message.CreateErrorMsg'),
              this.translateService.instant('System.Caption.Error')
            );
        }
      });
    } else {
      const systemInfo = this.commonService.systemInfo
      this.spinnerService.hide();
      if (systemInfo.programs.some(x => x.program_Code == this.param.program_Code)) {
        this.snotifyService.confirm(
          this.translateService.instant('System.Message.ConfirmChangeSameAccount'),
          this.translateService.instant('System.Action.Confirm'),
          () => this.callUpdate(() => this.authService.logout()));
      }
      else
        this.callUpdate(() => this.back())
    }
  }
  callUpdate(callbackFn: () => any) {
    this.spinnerService.show();
    this.service.edit(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.UpdateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          callbackFn()
        } else
          this.snotifyService.error(
            this.translateService.instant(result.error),
            this.translateService.instant('System.Caption.Error')
          );
      }
    });
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
