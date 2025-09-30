import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MonthlySalarySummaryReportForFinanceRoutingModule } from './monthly-salary-summary-report-for-finance-routing.module';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MainComponent } from './main/main.component';


@NgModule({
  declarations: [MainComponent],
  imports: [
    CommonModule,
    MonthlySalarySummaryReportForFinanceRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot()
  ]
})
export class MonthlySalarySummaryReportForFinanceModule { }
