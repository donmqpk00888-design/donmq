import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { AccountAuthorizationSetting_Data } from '@models/basic-maintenance/2_1_2_account-authorization-setting';
import { AuthService } from '@services/auth/auth.service';
import { S_2_1_2_AccountAuthorizationSettingService } from '@services/basic-maintenance/s_2_1_2_account-authorization-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  department: KeyValuePair[] = [];
  division: KeyValuePair[] = [];
  factory: KeyValuePair[] = [];
  roleList: KeyValuePair[] = [];
  data: AccountAuthorizationSetting_Data = <AccountAuthorizationSetting_Data>{
    isActive: true,
    listRole: []
  };
  title: string = '';
  url: string = '';
  action: string = '';

  constructor(
    private service: S_2_1_2_AccountAuthorizationSettingService,
    private authService: AuthService,
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDepartment();
      this.getListFactory();
      this.getListDivision();
    });
    this.getDataFromSource()
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => this.action = res.title);
    this.getListDivision();
    this.getListFactory();
    this.getListRole();
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSearch();
      if (this.action == 'Edit') {
        if (source.selectedData && Object.keys(source.selectedData).length > 0) {
          this.data = structuredClone(source.selectedData);
          this.getListDepartment();
        }
        else
          this.back()
      }
    })
  }

  save() {
    if (this.action == 'Add') {
      this.spinnerService.show();
      this.service.create(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          if (result.isSuccess) this.back();
        }
      })
    } else {
      const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
      if (user.account == this.data.account) {
        this.functionUtility.snotifyConfirm('System.Message.ConfirmChangeSameAccount', 'System.Action.Confirm', true, () => {
          this.callUpdate(() => this.authService.logout())
        });
      }
      else this.callUpdate(() => this.back())
    }
  }

  callUpdate(callbackFn: () => any) {
    this.spinnerService.show();
    this.service.update(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          callbackFn()
        }
        else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      }
    })
  }


  onChange() {
    this.deleteProperty('department_ID');
    this.department = [];
    if (!this.functionUtility.checkEmpty(this.data.factory) && !this.functionUtility.checkEmpty(this.data.division)) {
      this.getListDepartment();
    }
  }

  getListRole() {
    this.service.getListRole().subscribe({
      next: (res) => {
        this.roleList = res
        this.functionUtility.getNgSelectAllCheckbox(this.roleList)
      },
    });
  }


  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.division = res;
      },
    });
  }

  getListFactory() {
    this.service.getListListFactory().subscribe({
      next: (res) => {
        this.factory = res;
      },
    });
  }

  getListDepartment() {
    this.service.getListDepartment(this.data.division, this.data.factory).subscribe({
      next: (res) => {
        this.department = res;
      },
    });
  }

  resetPassword() {
    const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
    if (user.account == this.data.account) {
      this.functionUtility.snotifyConfirm('System.Message.ConfirmChangeSameAccount', 'System.Action.Confirm', true, () => {
        this.callResetPassword(() => this.authService.logout())
      });
    }
    else this.callResetPassword(() => this.back())
  }

  callResetPassword(callbackFn: () => any) {
    this.spinnerService.show();
    this.service.resetPassword(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(true, 'System.Message.ChangePasswordOKMsg')
          callbackFn()
        }
        else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      }
    })
  }

  deleteProperty = (name: string) => delete this.data[name]

  back = () => this.router.navigate([this.url]);

}
