import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { DirectoryMaintenance_Data } from '@models/system-maintenance/1_1_1_directory-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { AuthService } from '@services/auth/auth.service';
import { S_1_1_1_DirectoryMaintenanceService } from '@services/system-maintenance/s_1_1_1_directory-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  action: string = '';
  url: string = '';
  data: DirectoryMaintenance_Data = <DirectoryMaintenance_Data>{};
  iconButton = IconButton;
  listParentDirectoryCode: KeyValuePair[] = [];
  constructor(
    private service: S_1_1_1_DirectoryMaintenanceService,
    private authService: AuthService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource()
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getParent();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
    });
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.directorySource();
      if (this.action == 'Edit') {
        if (source.selectedData && Object.keys(source.selectedData).length > 0)
          this.data = structuredClone(source.selectedData);
        else
          this.back()
      }
    })
  }

  getParent() {
    this.service.getParent().subscribe({
      next: (res) => {
        this.listParentDirectoryCode = res
      }
    });
  }

  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();

  save() {
    this.data.seq = this.data.seq.toString();
    // Add Data
    this.spinnerService.show();
    if (this.action != 'Edit') {
      this.service.add(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
    // Edit Data
    else {
      const systemInfo = this.commonService.systemInfo
      this.spinnerService.hide()
      if (systemInfo.directories.some(x => x.directory_Code == this.data.directory_Code)) {
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
    this.service.edit(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
          callbackFn()
        }
        else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
      }
    })
  }
}
