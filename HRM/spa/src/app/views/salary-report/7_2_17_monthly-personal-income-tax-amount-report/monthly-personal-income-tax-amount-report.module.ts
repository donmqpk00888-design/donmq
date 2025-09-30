import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainComponent } from "./main/main.component";

import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MonthlyPersonalIncomeTaxAmountReportRoutingModule } from './monthly-personal-income-tax-amount-report-routing.module';


@NgModule({
  declarations: [ MainComponent ],
  imports: [
    CommonModule,
    MonthlyPersonalIncomeTaxAmountReportRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
  ]
})
export class MonthlyPersonalIncomeTaxAmountReportModule { }
