import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { BankAccountMaintenanceRoutingModule } from './bank_account_maintenance-routing.module';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { SharedModule } from '@views/_shared/shared.module';

@NgModule({
  imports: [
    CommonModule,
    BankAccountMaintenanceRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    BsDatepickerModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    TypeaheadModule.forRoot(),
    SharedModule
  ],
  declarations: [MainComponent, FormComponent]
})
export class BankAccountMaintenanceModule { }
