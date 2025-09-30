import { SharedModule } from '@views/_shared/shared.module';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { FormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PayslipDeliverybyEmailMaintenanceRoutingModule } from './payslip-delivery-by-email-maintenance-routing.module';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';


@NgModule({
  declarations: [
    MainComponent,
    FormComponent
  ],
  imports: [
    CommonModule,
    PayslipDeliverybyEmailMaintenanceRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    NgxMaskDirective,
    NgxMaskPipe,
    TypeaheadModule.forRoot(),
    SharedModule
  ]
})
export class PayslipDeliverybyEmailMaintenanceModule { }
