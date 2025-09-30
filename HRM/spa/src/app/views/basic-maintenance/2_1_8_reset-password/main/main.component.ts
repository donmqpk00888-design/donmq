import { Component } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ResetPasswordModel, ResetPasswordParam } from '@models/basic-maintenance/2_1_8_reset-password';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_8_resetPasswordService } from '@services/basic-maintenance/s_2_1_8_reset-password.service';
import { InjectBase } from '@utilities/inject-base-app';
import { OperationResult } from '@utilities/operation-result';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase {
  regex = /^(?=.*[0-9])(?=.*[a-zA-Z]).{6,}$/
  model: ResetPasswordModel = <ResetPasswordModel>{
    newPassword: '',
    againNewPassword: ''
  };
  title: string = '';
  iconButton = IconButton;
  constructor(private service: S_2_1_8_resetPasswordService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    const userInfo = JSON.parse(localStorage.getItem(LocalStorageConstants.USER))
    this.model.account = userInfo.id;
    this.model.name = userInfo.name;
  }

  validPassword() {
    if(this.functionUtility.checkEmpty(this.model.newPassword)
    || this.functionUtility.checkEmpty(this.model.againNewPassword))
      return true

    if(!this.regex.test(this.model.newPassword)
      || !this.regex.test(this.model.againNewPassword))
      return true
    return false
  }

  resetPassword() {
    if(this.model.newPassword != this.model.againNewPassword){
      this.functionUtility.snotifySuccessError(false,"System.Message.ConfirmPassword")
      return;
    }
    this.spinnerService.show();
    const param = <ResetPasswordParam> {
      account: this.model.account,
      newPassword: this.model.newPassword
    }
    this.service.resetPassword(param).subscribe({
      next: (result: OperationResult) => {
        this.spinnerService.hide();

        if(result.isSuccess)
        {
          var userInfo = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
          userInfo.password_Reset = false;
          localStorage.setItem(LocalStorageConstants.USER, JSON.stringify(userInfo));
        }
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.ChangePasswordOKMsg' : result.error, result.isSuccess)
      }
    });
  }
}
