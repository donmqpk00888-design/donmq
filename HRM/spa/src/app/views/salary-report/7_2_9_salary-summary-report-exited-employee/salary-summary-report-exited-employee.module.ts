import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SalarySummaryReportExitedEmployeeRoutingModule } from './salary-summary-report-exited-employee-routing.module';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MainComponent } from "./main/main.component";


@NgModule({
  declarations: [
    MainComponent
  ],
  imports: [
    CommonModule,
    SalarySummaryReportExitedEmployeeRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
  ]
})
export class SalarySummaryReportExitedEmployeeModule { }
