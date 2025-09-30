import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { NgxSpinnerService } from 'ngx-spinner';
import { NgSnotifyService } from '@services/ng-snotify.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '@services/auth/auth.service';
// import { CaptionConstants, MessageConstants } from '@constants/message.enum';

@Injectable()
export class GlobalHttpInterceptor implements HttpInterceptor {
  constructor(
    private snotifyService: NgSnotifyService,
    private spinnerService: NgxSpinnerService,
    private translateService: TranslateService,
    private authService: AuthService
  ) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      tap({
        error: (res: HttpErrorResponse) => {
          this.snotifyService.clear()
          switch (res.status) {
            case HttpStatusCode.InternalServerError:
              // this.snotifyService.error(MessageConstants.SYSTEM_ERROR_MSG, CaptionConstants.ERROR);
              this.snotifyService.error(
                res.error,
                this.translateService.instant('System.Caption.Error')
              );
              break;
            case HttpStatusCode.ServiceUnavailable:
              // this.snotifyService.error(MessageConstants.SYSTEM_ERROR_MSG, CaptionConstants.ERROR);
              this.snotifyService.error(
                this.translateService.instant('System.Message.SystemError'),
                this.translateService.instant('System.Caption.Error')
              );
              break;
            case HttpStatusCode.Unauthorized:
              this.authService.logout()
              // this.snotifyService.error(MessageConstants.SESSION_EXPIRED, CaptionConstants.ERROR);
              this.snotifyService.error(
                this.translateService.instant('System.Message.SessionExpired'),
                this.translateService.instant('System.Caption.Error')
              );
              break;
            case HttpStatusCode.Conflict:
              // this.snotifyService.error(MessageConstants.TRANSACTION_ERROR_MSG, CaptionConstants.ERROR);
              this.snotifyService.error(
                this.translateService.instant('System.Message.TransactionError'),
                this.translateService.instant('System.Caption.Error')
              );
              break;
            default:
              // this.snotifyService.error(MessageConstants.UN_KNOWN_ERROR, CaptionConstants.ERROR);
              this.snotifyService.error(
                this.translateService.instant('System.Message.UnknowError'),
                this.translateService.instant('System.Caption.Error')
              );
              break;
          }
          this.spinnerService.hide();
        }
      })
    );
  }
}
