import { Injectable } from '@angular/core';
import { SnotifyPosition, SnotifyService, SnotifyToastConfig } from 'ngx-snotify-sdteam';

@Injectable({ providedIn: 'root' })
export class NgSnotifyService {
  config: SnotifyToastConfig = {
    bodyMaxLength: 300,
    titleMaxLength: 100,
    backdrop: -1,
    position: SnotifyPosition.rightTop,
    timeout: 3000,
    showProgressBar: true,
    closeOnClick: true,
    pauseOnHover: true
  };

  constructor(private snotifyService: SnotifyService) {
    this.setDefaults();
  }

  setDefaults() {
    this.snotifyService.setDefaults({
      global: {
        maxAtPosition: 10,
        maxOnScreen: 10,
        newOnTop: true,
        filterDuplicates: false,
        closeOnBackgroundClick: true
      }
    });
  }

  success(body: string, title: string) {
    this.snotifyService.success(body, title, this.config);
  }

  info(body: string, title: string) {
    this.snotifyService.info(body, title, this.config);
  }

  error(body: string, title: string) {
    this.snotifyService.error(body, title, this.config);
  }

  warning(body: string, title: string) {
    this.snotifyService.warning(body, title, this.config);
  }

  simple(body: string, title: string) {
    this.snotifyService.simple(body, title, this.config);
  }

  confirm(body: string, title: string, okCallback: (val: boolean) => any, cancelCallBack?: (val: boolean) => any) {
    const config = { ...this.config };
    config.position = SnotifyPosition.centerCenter;
    config.timeout = 0;
    config.backdrop = 0.5;
    config.closeOnClick = false;

    this.snotifyService.confirm(body, title, {
      ...config,
      buttons: [
        {
          text: 'OK',
          action: toast => {
            this.snotifyService.remove(toast.id);
            okCallback(true);
          },
          bold: true
        },
        {
          text: 'Cancel',
          action: toast => {
            this.snotifyService.remove(toast.id);
            if (typeof (cancelCallBack) === 'function') {
              cancelCallBack(false);
            }
          }
        }
      ]
    });
    setTimeout(() => {
      const OkButton = document.querySelector('.snotifyToast__buttons--bold'); // Hoặc '.snotify-confirm-yes'
      if (OkButton instanceof HTMLElement)
        OkButton.focus();
    }, 100);
  }
  confirmOk(body: string, title: string, callBack: () => any) {
    this.snotifyService.setDefaults({
      global: {
        maxAtPosition: 10,
        maxOnScreen: 10,
        newOnTop: true,
        filterDuplicates: true
      }
    });
    const config = { ...this.config };
    config.position = SnotifyPosition.centerCenter;
    config.timeout = 0;
    config.backdrop = 0.5;
    config.closeOnClick = false;
    let snotify = this.snotifyService.confirm(body, title, {
      ...config,
      buttons: [
        {
          text: 'OK',
          action: toast => {
            this.snotifyService.remove(toast.id);
            callBack();
          },
          bold: true
        },
      ]
    });
    snotify.id = 9999;
  }
  accept(body: string, title: string, okCallback: () => any) {
    const config = { ...this.config };
    config.position = SnotifyPosition.centerCenter;
    config.timeout = 0;
    config.backdrop = 0.5;
    config.closeOnClick = false;
    this.snotifyService.confirm(body, title, {
      ...config,
      buttons: [
        {
          text: 'OK',
          action: toast => {
            this.snotifyService.remove(toast.id);
            okCallback();
          },
          bold: true
        }
      ]
    });
  }
  clear() {
    this.snotifyService.clear();
  }
}
