import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { PersonalIncomeTaxNumberMaintenanceRoutingModule } from './personal-income-tax-number-maintenance-routing.module';
import { FormsModule } from '@angular/forms';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { SharedModule } from '@views/_shared/shared.module';


@NgModule({
  declarations: [
    MainComponent,
    FormComponent
  ],
  imports: [
    CommonModule,
    PersonalIncomeTaxNumberMaintenanceRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    BsDatepickerModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    TypeaheadModule.forRoot(),
    SharedModule
  ]
})
export class PersonalIncomeTaxNumberMaintenanceModule { }
