import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  DirectWorkTypeAndSectionSetting,
  DirectWorkTypeAndSectionSettingParam,
  HRMS_Org_Direct_SectionDto
} from '@models/organization-management/3_1_5_organization-management';
import { S_3_1_5_DirectWorkTypeAndSectionSettingService } from '@services/organization-management/s_3_1_5_direct-work-type-and-section-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility'; import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { S_5_1_productionEffiencyService } from '@services/kanban/s_5_1_production-effiency.service';
import { ProductionEfficiencyDetail, ProductionEfficiencyDTO, ProductionEfficiencyParam } from '@models/kanban/5_1_production-effiency';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  listShift: KeyValuePair[] = [];
  iconButton = IconButton;
  date: Date = null;
  play: boolean = false;
  data: ProductionEfficiencyDTO[] = [
    { category: '', detail: [] }
  ];
  detail: ProductionEfficiencyDetail[] = [];
  param: ProductionEfficiencyParam = <ProductionEfficiencyParam>{};

  constructor(
    private service: S_5_1_productionEffiencyService,
  ) {
    super();
  }
  ngOnDestroy(): void {
  }

  ngOnInit() {
    this.search(true);
  }

  checkDate() {
    if (this.date != null)
      this.param.productionDate = this.date.toDate().toStringYearMonth();
    else this.deleteProperty('productionDate');
  }
  charts: { labels: string[], data: ChartDataset[], options: ChartOptions, category: string }[] = [];
  getData(isSearch?: boolean) {
    this.checkDate()
    this.spinnerService.show();
    this.service.getData(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.charts = res.map(x => ({
          labels: x.detail.map(d => d.rmodel),
          data: [{ data: x.detail.map(d => d.qty), label: x.category, stack: 'a' }],
          options: { responsive: true, indexAxis: 'y' },
          category: x.category
        }));
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      },
    });
  }



  clear(isClear: boolean) {
    this.deleteProperty('productionDate')
    this.data = [];
    this.date = null;
  }

  search(isSearch: boolean) {
    this.getData(isSearch);
  }
  intervalId: any;
  playButton() {
    this.play = !this.play;
    console.log('this.play :', this.play);
    if (this.play) {
      this.getData();
      this.intervalId = setInterval(() => {
        this.getData();
      }, 1000);
    } else {
      clearInterval(this.intervalId);
    }
  }

  deleteProperty = (name: string) => delete this.param[name]

  //event change page

  barChartOptions: ChartOptions = {
    responsive: true,
    indexAxis: 'y',

  };
  barChartType: ChartType = 'bar';
  barChartLegend = true;

  barChartData: ChartDataset[] = [
    { data: [], stack: 'a' }
  ];
  barChartLabels: string[] = [];
  // events
  chartClicked({
    event,
    active,
  }: {
    event: MouseEvent;
    active: {}[];
  }): void {
    console.log(event, active);
  }

  chartHovered({
    event,
    active,
  }: {
    event: MouseEvent;
    active: {}[];
  }): void {
    console.log(event, active);
  }

  // randomize(): void {
  //   // Only Change 3 values
  //   const data = [
  //     Math.round(Math.random() * 100),
  //     59,
  //     80,
  //     Math.random() * 100,
  //     56,
  //     Math.random() * 100,
  //     40,
  //   ];
  //   const clone = JSON.parse(JSON.stringify(this.barChartData));
  //   clone[0].data = data;
  //   this.barChartData = clone;
  // }
}
