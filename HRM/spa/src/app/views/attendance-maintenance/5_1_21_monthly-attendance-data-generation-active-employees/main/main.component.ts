import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_21_monthly-attendance-data-generation-active-employees.service';
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
  @ViewChild('searchAlreadyDeadlineDataTab', { static: true }) searchAlreadyDeadlineDataTab: TemplateRef<any>;
  @ViewChild('dataCloseTab', { static: true }) dataCloseTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';
  selectedTab: string = ''

  constructor(private service: S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService) {
  super();
  this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.initTab();
    this.selectedTab = this.service.tabSelected();
  }

  initTab(){
    this.tabs = [
      {
        id: 'dataGeneration',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceDataGenerationActiveEmployees.MonthlyAttendanceDataGeneration'),
        isEnable: true,
        content: this.dataGenerationTab
      },
      {
        id: 'searchAlreadyDeadlineData',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceDataGenerationActiveEmployees.SearchAlreadyDeadlineData'),
        isEnable: true,
        content: this.searchAlreadyDeadlineDataTab
      },
      {
        id: 'dataClose',
        title: this.translateService.instant('AttendanceMaintenance.MonthlyAttendanceDataGenerationActiveEmployees.MonthlyDataClose'),
        isEnable: true,
        content: this.dataCloseTab
      },
    ]
  }

  ngOnDestroy(): void {
    this.service.setTabSelected(this.selectedTab);
  }

}
