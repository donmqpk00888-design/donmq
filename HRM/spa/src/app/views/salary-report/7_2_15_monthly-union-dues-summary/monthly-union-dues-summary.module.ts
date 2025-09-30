import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MonthlyUnionDuesSummaryRoutingModule } from './monthly-union-dues-summary-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { MainComponent } from './main/main.component';


@NgModule({
  declarations: [
    MainComponent,
  ],
  imports: [
    CommonModule,
    MonthlyUnionDuesSummaryRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    ReactiveFormsModule,
    BsDatepickerModule.forRoot(),
    NgxMaskDirective, NgxMaskPipe,
  ]
})
export class MonthlyUnionDuesSummaryModule { }
