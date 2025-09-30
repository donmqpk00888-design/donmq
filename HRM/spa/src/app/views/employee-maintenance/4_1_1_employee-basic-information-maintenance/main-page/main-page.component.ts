import { Component, ElementRef, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  ClassButton,
  EmployeeMode,
  IconButton,
} from '@constants/common.constants';
import { EmployeeBasicInformationMaintenanceSource } from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { CommonService } from '@services/common.service';
import { S_4_1_1_EmployeeBasicInformationMaintenanceService } from '@services/employee-maintenance/s_4_1_1_employee-basic-information-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-page',
  templateUrl: './main-page.component.html',
  styleUrls: ['./main-page.component.scss'],
})
export class MainPageComponent411 extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('form411', { static: true }) form411: TemplateRef<any>;
  @ViewChild('form412', { static: true }) form412: TemplateRef<any>;
  @ViewChild('form413', { static: true }) form413: TemplateRef<any>;
  @ViewChild('form414', { static: true }) form414: TemplateRef<any>;
  @ViewChild('form415', { static: true }) form415: TemplateRef<any>;

  tabs: TabComponentModel[] = [];
  source: EmployeeBasicInformationMaintenanceSource = <EmployeeBasicInformationMaintenanceSource>{};
  mode = EmployeeMode;
  title: string = '';
  url: string = '';
  action: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  constructor(
    private activatedRoute: ActivatedRoute,
    private _service: S_4_1_1_EmployeeBasicInformationMaintenanceService,
    private elementRef: ElementRef
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab()
    });
    this._service.parentFun.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.back());
    this._service.tranferChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => this.reloadTranfer(event));
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.activatedRoute.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((role) => {
      this.source.mode = role.title.toLowerCase();
      this.action = role.title
    });
    this._service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (
        this.source.mode == this.mode.edit ||
        this.source.mode == this.mode.query ||
        this.source.mode == this.mode.rehire
      ) {
        res != null ? (this.source = res) : this.back();
      }
    });
    this.initTab()
  }
  ngOnDestroy(): void {
    this.elementRef.nativeElement.remove();
  }
  changeTab(event: string) {
    this.functionUtility.setFunction(event)
  }
  initTab() {
    this.tabs = [
      {
        id: '4.1.1',
        title: this.translateService.instant('EmployeeInformationModule.EmployeeBasicInformationMaintenance.TabTitle'),
        isEnable: true,
        content: this.form411
      },
      {
        id: '4.1.2',
        title: this.translateService.instant('EmployeeInformationModule.EmployeeEmergencyContacts.TabTitle'),
        isEnable: this.checkRole('4.1.2'),
        content: this.form412
      },
      {
        id: '4.1.3',
        title: this.translateService.instant('EmployeeInformationModule.Education.TabTitle'),
        isEnable: this.checkRole('4.1.3'),
        content: this.form413
      },
      {
        id: '4.1.4',
        title: this.translateService.instant('EmployeeInformationModule.DependentInformation.TabTitle'),
        isEnable: this.checkRole('4.1.4'),
        content: this.form414
      },
      {
        id: '4.1.5',
        title: this.translateService.instant('EmployeeInformationModule.ExternalExperience.TabTitle'),
        isEnable: this.checkRole('4.1.5'),
        content: this.form415
      },
    ]
  }
  back = () => this.router.navigate([this.url]);

  reloadTranfer(item: EmployeeBasicInformationMaintenanceSource) {
    if (item != null) {
      this.source = item;
      this.initTab()
    }
  }

  checkRole(role: string) {
    const systemInfo = this.commonService.systemInfo
    if (systemInfo.programs == null || this.source?.mode == this.mode.add) return false;
    return systemInfo.programs.some((item) => item.program_Code == role);
  }
}
