import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SalaryAdjustmentMaintenanceRoutingModule } from './salary-adjustment-maintenance-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { AddComponent } from './add/add.component';
import { EditComponent } from './edit/edit.component';
import { MainComponent } from './main/main.component';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { SharedModule } from '@views/_shared/shared.module';


@NgModule({
  declarations: [
    MainComponent,
    AddComponent,
    EditComponent
  ],
  imports: [
    CommonModule,
    SalaryAdjustmentMaintenanceRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    ReactiveFormsModule,
    BsDatepickerModule.forRoot(),
    PaginationModule.forRoot(),
    NgxMaskDirective, NgxMaskPipe,
    TypeaheadModule.forRoot(),
    SharedModule
  ]
})
export class SalaryAdjustmentMaintenanceModule { }
