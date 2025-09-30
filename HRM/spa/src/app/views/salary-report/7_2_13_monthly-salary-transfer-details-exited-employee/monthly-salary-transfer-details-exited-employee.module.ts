import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MonthlySalaryTransferDetailsExitedEmployeeRoutingModule } from './monthly-salary-transfer-details-exited-employee-routing.module';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MainComponent } from './main/main.component';


@NgModule({
  declarations: [
    MainComponent
  ],
  imports: [
    CommonModule,
    MonthlySalaryTransferDetailsExitedEmployeeRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
  ]
})
export class MonthlySalaryTransferDetailsExitedEmployeeModule { }
