import { OperationResult } from './../../_core/utilities/operation-result';
import { Component, OnInit } from "@angular/core";
import { InjectBase } from "@utilities/inject-base-app";
import { ResultResponse, UserLoginParam } from '@models/auth/auth';
import { AuthService } from '@services/auth/auth.service';
import { KeyValuePair } from "@utilities/key-value-pair";
import { lastValueFrom } from "rxjs";
import { DirectoryInfomation, ProgramInfomation } from "@models/common";
import { LocalStorageConstants } from "@constants/local-storage.constants";

@Component({
  selector: "app-dashboard",
  templateUrl: "login.component.html",
  styleUrls: ["./login.component.scss"],
})
export class LoginComponent extends InjectBase implements OnInit {
  user: UserLoginParam = <UserLoginParam>{};
  listFactory: KeyValuePair[] = [];
  constructor(private authService: AuthService) {
    super();
  }

  ngOnInit() {
    if (this.authService.loggedIn())
      this.router.navigate(["/dashboard"])
    this.getListFactory()
  }
  login() {
    this.spinnerService.show();
    this.authService.login(this.user).subscribe({
      next: async (res: OperationResult) => {
        if (res.isSuccess) {
          const data = res.data as ResultResponse;
          localStorage.setItem(LocalStorageConstants.TOKEN, data.token);
          localStorage.setItem(LocalStorageConstants.USER, JSON.stringify(data.user));
          const password_reset = await lastValueFrom(this.commonService.getPasswordReset());
          if (password_reset) {
            const systemInfo = this.commonService.systemInfo
            const user_directory: DirectoryInfomation[] = systemInfo.directories || [];
            const roleOfUser: ProgramInfomation[] = systemInfo.programs || [];
            const parent = user_directory[1]?.directory_Name.toLowerCase().replace(' ', '-')
            const child = roleOfUser.find(x => x.program_Code == '2.1.8').program_Name.toLowerCase().replace(' ', '-')
            this.snotifyService.warning(
              this.translateService.instant('System.Message.PasswordReset'),
              this.translateService.instant('System.Caption.Warning'))
            this.router.navigate([`/${parent}/${child}`])
          }
          else {
            this.snotifyService.success(
              this.translateService.instant('System.Message.LogIn'),
              this.translateService.instant('System.Caption.Success'))
            this.router.navigate(["/dashboard"]);
          }
        } else {
          this.snotifyService.error(
            this.translateService.instant('System.Message.LogInFailed'),
            this.translateService.instant('System.Caption.Error'))
        }
        this.spinnerService.hide();
      }
    });
  }
  getListFactory() {
    this.authService.getListFactory().subscribe({
      next: res => {
        this.listFactory = res;
      }
    })
  }
}
