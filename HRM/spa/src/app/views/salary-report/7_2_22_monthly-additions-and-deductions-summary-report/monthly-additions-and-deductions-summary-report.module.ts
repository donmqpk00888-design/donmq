import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MainComponent } from './main/main.component';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { MonthlyAdditionsAndDeductionsSummaryReportRoutingModule } from './monthly-additions-and-deductions-summary-report-routing.module';


@NgModule({
  declarations: [MainComponent],
  imports: [
    CommonModule,
    MonthlyAdditionsAndDeductionsSummaryReportRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
    NgxMaskDirective,
    NgxMaskPipe,
  ]
})
export class MonthlyAdditionsAndDeductionsSummaryReportModule { }
