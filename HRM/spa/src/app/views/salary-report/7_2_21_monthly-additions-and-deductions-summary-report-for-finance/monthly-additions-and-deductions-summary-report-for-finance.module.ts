import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainComponent } from './main/main.component';
import { MonthlyAdditionsAndDeductionsSummaryReportForFinanceRoutingModule } from './monthly-additions-and-deductions-summary-report-for-finance-routing.module';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';

@NgModule({
  declarations: [ MainComponent ],
  imports: [
    CommonModule,
    MonthlyAdditionsAndDeductionsSummaryReportForFinanceRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
  ]
})
export class MonthlyAdditionsAndDeductionsSummaryReportForFinanceModule { }
