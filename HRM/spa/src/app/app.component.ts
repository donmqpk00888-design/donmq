import { Component, OnInit } from '@angular/core';
import '../app/_core/utilities/extension-methods';
import { IconSetService } from '@coreui/icons-angular';
import { freeSet } from '@coreui/icons';
import './_core/utilities/extension-methods'
import { InjectBase } from '@utilities/inject-base-app';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LangConstants } from '@constants/lang-constants';
import { lastValueFrom } from 'rxjs';
import { AuthService } from '@services/auth/auth.service';

@Component({
  // tslint:disable-next-line
  selector: 'body',
  template: `
    <router-outlet></router-outlet>
    <ng-snotify></ng-snotify>
    <ng-progress></ng-progress>
    <ngx-spinner bdColor="rgba(51,51,51,0.8)" size="medium" color="#fff" type="ball-scale-multiple"></ngx-spinner>
    <theme-switch></theme-switch>
  `,
  providers: [IconSetService],
})
export class AppComponent extends InjectBase implements OnInit {

  defaultLang: string = LangConstants.EN

  constructor(public iconSet: IconSetService, private authService: AuthService) {
    super()
    // iconSet singleton
    iconSet.icons = { ...freeSet };
  }

  async ngOnInit(): Promise<void> {
    await this.initTranslate();
  }

  async initTranslate() {
    this.spinnerService.show()
    const langs = await lastValueFrom(this.authService.getListLangs());
    if (langs.length > 0)
      this.translateService.addLangs(langs);
    let lang: string = localStorage.getItem(LocalStorageConstants.LANG);
    if (!lang || (lang && (!langs.includes(lang) || !await this.functionUtility.isExistedTranslation(lang)))) {
      if (!langs.includes(this.defaultLang))
        langs.push(this.defaultLang)
      for (let i = 0; i < langs.length; i++) {
        if (await this.functionUtility.isExistedTranslation(langs[i])) {
          lang = langs[i]
          break
        }
      }
    }
    lang ? this.setLang(lang) : this.snotifyService.error('An error occurred while initializing language', 'Error!');
    this.spinnerService.hide()
  }

  private setLang(lang: string) {
    localStorage.setItem(LocalStorageConstants.LANG, lang);
    this.translateService.setDefaultLang(lang);
    this.translateService.use(lang);
  }
}
