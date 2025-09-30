import { Injectable, inject } from '@angular/core';
import { CanMatchFn, Route, Router } from '@angular/router';
import { DirectoryInfomation, ProgramInfomation } from '@models/common';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '@services/auth/auth.service';
import { CommonService } from '@services/common.service';
import { NgSnotifyService } from '@services/ng-snotify.service';
import { FunctionUtility } from '@utilities/function-utility';
import { lastValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AppGuard {
  private resetDirId: string = '2'
  private resetPassAddr: string = `${this.resetDirId}.1.8`
  private recentProgram: string = ''
  constructor(
    private router: Router,
    private snotify: NgSnotifyService,
    private translate: TranslateService,
    private authService: AuthService,
    private commonService: CommonService,
    private functionUtility: FunctionUtility,
  ) { }
  async canMatchMain(route: Route): Promise<boolean> {
    try {
      this.recentProgram = route.data['program'];
      if (!this.authService.loggedIn()) {
        this.snotify.error(
          this.translate.instant('System.Message.Logout'),
          this.translate.instant('System.Caption.Error')
        );
        return this.next(false, '/login');
      }
      if (this.authService.loggedInExpired()) {
        this.snotify.error(
          this.translate.instant('System.Message.SessionExpired'),
          this.translate.instant('System.Caption.Error')
        );
        return this.next(false, '/login');
      }
      const systemInfo = this.commonService.systemInfo
      const directoryUser: DirectoryInfomation[] = systemInfo?.directories || [];
      const programUser: ProgramInfomation[] = systemInfo?.programs || [];
      const hasProgramAccess = programUser.some(x => x.program_Code?.trim() === this.recentProgram?.trim());
      if (directoryUser.length == 0 || programUser.length == 0 || !hasProgramAccess)
        return this.next(false, '/dashboard');
      const passwordReset = await lastValueFrom(this.commonService.getPasswordReset());
      if (passwordReset && this.recentProgram !== this.resetPassAddr) {
        const directoryUrl = directoryUser.find(x => x.seq == this.resetDirId)?.directory_Name?.toUrl() || ''
        const programUrl = programUser.find(x => x.program_Code === this.resetPassAddr)?.program_Name?.toUrl() || ''
        if (directoryUrl.isNullOrWhiteSpace() || programUrl.isNullOrWhiteSpace())
          return this.next(false, "/500")
        const resetPasswordUrl = `/${directoryUrl}/${programUrl}`;
        this.snotify.clear();
        this.snotify.warning(
          this.translate.instant('System.Message.PasswordReset'),
          this.translate.instant('System.Caption.Warning')
        );
        return this.next(false, resetPasswordUrl);
      }
      this.functionUtility.setFunction(this.recentProgram)
      return this.next(true);
    } catch {
      return this.next(false, '/500');
    }
  }
  async canMatchForm(route: Route): Promise<boolean> {
    const isExisted = this.functionUtility.checkFunction(route.data.title)
    return this.next(isExisted, !isExisted ? this.router.url : null)
  }
  private next(result: boolean, url?: string) {
    if (!result) url == '/login' ? this.authService.logout() : this.router.navigate([url]);
    return result;
  }
}
export const appGuard: CanMatchFn = (route: Route) => {
  return inject(AppGuard).canMatchMain(route);
};
export const formGuard: CanMatchFn = (route: Route) => {
  return inject(AppGuard).canMatchForm(route);
};
