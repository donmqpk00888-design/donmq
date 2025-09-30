import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MonthlySalaryMaintenanceExitedEmployeesRoutingModule } from './monthly-salary-maintenance-exited-employees-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { CollapseModule } from 'ngx-bootstrap/collapse';
import { DragScrollComponent } from 'ngx-drag-scroll';
import { SharedModule } from '@views/_shared/shared.module';


@NgModule({
  declarations: [
    MainComponent,
    FormComponent
  ],
  imports: [
    CommonModule,
    MonthlySalaryMaintenanceExitedEmployeesRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    ReactiveFormsModule,
    BsDatepickerModule.forRoot(),
    PaginationModule.forRoot(),
    NgxMaskDirective, NgxMaskPipe,
    CollapseModule.forRoot(),
    TypeaheadModule.forRoot(),
    DragScrollComponent,
    SharedModule
  ]
})
export class MonthlySalaryMaintenanceExitedEmployeesModule { }
