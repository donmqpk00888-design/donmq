import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { MonthlyDataLockParam, MonthlySalaryGeneration_Memory, MonthlySalaryGenerationParam } from '@models/salary-maintenance/7_1_22_monthly-salary-generation';
import { S_7_1_22_MonthlySalaryGenerationService } from '@services/salary-maintenance/s_7_1_22_monthly-salary-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('salaryGenerationTab', { static: true }) salaryGenerationTab: TemplateRef<any>;
  @ViewChild('dataLockTab', { static: true }) dataLockTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';
  selectedTab: string = ''
  param1: MonthlySalaryGenerationParam = <MonthlySalaryGenerationParam>{}
  param2: MonthlyDataLockParam = <MonthlyDataLockParam>{ salary_Lock: "Y" }
  constructor(private service: S_7_1_22_MonthlySalaryGenerationService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.initTab();
      });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.selectedTab = this.service.programSource().selectedTab;
    this.param1 = this.service.programSource().salaryGeneration_Param;
    this.param2 = this.service.programSource().dataLock_Param;
    this.initTab();
  }
  initTab() {
    this.tabs = [
      {
        id: 'salaryGeneration',
        title: this.translateService.instant('SalaryMaintenance.MonthlySalaryGeneration.MonthlySalaryGeneration'),
        isEnable: true,
        content: this.salaryGenerationTab
      },
      {
        id: 'dataLock',
        title: this.translateService.instant('SalaryMaintenance.MonthlySalaryGeneration.MonthlyDataLock'),
        isEnable: true,
        content: this.dataLockTab
      },
    ]
  }
  ngOnDestroy(): void {
    this.service.setSource(<MonthlySalaryGeneration_Memory>{
      salaryGeneration_Param: this.param1,
      dataLock_Param: this.param2,
      selectedTab: this.selectedTab
    });
  }
}
