import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { S_7_1_15_ChildcareSubsidyGenerationService } from '@services/salary-maintenance/s_7_1_15_childcare-subsidy-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('childSubsidyIsGeneratedTab', { static: true }) childSubsidyIsGeneratedTab: TemplateRef<any>;
  @ViewChild('reportPrintingTab', { static: true }) reportPrintingTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';
  selectedTab: string = ''

  constructor(private service: S_7_1_15_ChildcareSubsidyGenerationService) {
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
        id: 'childSubsidyIsGenerated',
        title: this.translateService.instant('SalaryMaintenance.ChildcareSubsidyGeneration.ChildSubsidyIsGenerated'),
        isEnable: true,
        content: this.childSubsidyIsGeneratedTab
      },
      {
        id: 'reportPrinting',
        title: this.translateService.instant('SalaryMaintenance.ChildcareSubsidyGeneration.ReportPrinting'),
        isEnable: true,
        content: this.reportPrintingTab
      },
    ]
  }

  ngOnDestroy(): void {
    this.service.setTabSelected(this.selectedTab);
  }

}
