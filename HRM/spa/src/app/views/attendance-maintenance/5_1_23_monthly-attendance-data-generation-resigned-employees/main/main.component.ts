import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService } from '@services/attendance-maintenance/s_5_1_23_monthly-attendance-data-generation-resigned-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('dataGenerationTab', { static: true }) dataGenerationTab: TemplateRef<any>;
  @ViewChild('dataCloseTab', { static: true }) dataCloseTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';
  selectedTab: string = ''

  constructor(private service: S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
        this.initTab();
      });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.initTab();
    this.selectedTab = this.service.tabSelected();
  }

  initTab(){
    this.tabs = [
      {
        id: 'dataGeneration',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceDataGenerationResignedEmployees.MonthlyAttendanceDataGeneration'),
        isEnable: true,
        content: this.dataGenerationTab
      },
      {
        id: 'dataClose',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceDataGenerationResignedEmployees.MonthlyDataClose'),
        isEnable: true,
        content: this.dataCloseTab
      },
    ]
  }

  ngOnDestroy(): void {
    this.service.setTabSelected(this.selectedTab);
  }
}
