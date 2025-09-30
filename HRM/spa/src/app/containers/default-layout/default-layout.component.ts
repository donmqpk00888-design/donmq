import { SignalRService } from '@services/signalr.service';
import { LangConstants } from '@constants/lang-constants';
import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Nav } from '../../_nav';
import { INavData } from '@coreui/angular';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { AuthService } from '@services/auth/auth.service';
import { RouterEvent, Event, ResolveStart, ResolveEnd } from '@angular/router';
import { filter, lastValueFrom } from 'rxjs';
import { InjectBase } from '@utilities/inject-base-app';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-dashboard',
  templateUrl: './default-layout.component.html',
  styleUrls: ['./default-layout.component.scss']
})
export class DefaultLayoutComponent extends InjectBase implements OnInit, AfterViewInit {
  @ViewChild('appBody', { static: false }) appBody: ElementRef;
  onScroll = () => this.showScroll = this.appBody.nativeElement.scrollTop > this.showScrollHeight;
  scrollToTop = () => this.appBody.nativeElement.scroll({ top: 0, left: 0, behavior: 'smooth' });
  showScroll: boolean;
  showScrollHeight = 400;

  langConstants: typeof LangConstants = LangConstants;

  public sidebarMinimized = false;
  public navItems: INavData[] = [];
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  constructor(
    private authService: AuthService,
    private signalRService: SignalRService,
    private navItem: Nav
  ) {
    super()
    this.signalRService.accountChangedEmitter.pipe(takeUntilDestroyed()).subscribe((account: string[]) => {
      this.checkAccount(account)
    });
    this.router.events
      .pipe(filter((e: Event | RouterEvent): e is RouterEvent => e instanceof RouterEvent), takeUntilDestroyed())
      .subscribe((routerEvent: RouterEvent) => {
        if (routerEvent instanceof ResolveStart)
          this.spinnerService.show()
        if (routerEvent instanceof ResolveEnd)
          this.spinnerService.hide()
      });
  }

  async ngOnInit(): Promise<void> {
    this.spinnerService.show()
    await this.initSystem()
    this.navItems = this.navItem.getNav();
    this.signalRService.startConnection();
    this.signalRService.addListeners();
    this.spinnerService.hide()
  }

  ngOnDestroy(): void {
    this.signalRService.stopConnection();
  }

  ngAfterViewInit(): void {
    window.scrollTo(0, 0);
  }

  checkAccount(account: string[]) {
    if (account && account.some(x => this.user.account == x)) {
      this.snotifyService.accept(
        this.translateService.instant('System.Message.ChangedAccount'),
        this.translateService.instant('System.Caption.Warning'),
        () => this.authService.logout());
    }
  }

  toggleMinimize(e: any) {
    this.sidebarMinimized = e;
  }

  async initSystem() {
    const systemInfo = await lastValueFrom(this.commonService.getSystemInfo());
    localStorage.setItem(LocalStorageConstants.SYSTEM_INFO, systemInfo);
  }

  async switchLang(lang: string) {
    const recentLang = localStorage.getItem(LocalStorageConstants.LANG);
    if (recentLang != lang) {
      if (!await this.functionUtility.isExistedTranslation(lang))
        return this.snotifyService.error(
          'Invalid translation file',
          this.translateService.instant('System.Caption.Error')
        );
      localStorage.setItem(LocalStorageConstants.LANG, lang);
      this.translateService.use(lang);
      this.navItems = this.navItem.getNav();
    }
  }
}
