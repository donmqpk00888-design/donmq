import { SharedModule } from '@views/_shared/shared.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ListofChildcareSubsidyRecipientsMaintenanceRoutingModule } from './list-of-childcare-subsidy-recipients-maintenance-routing.module';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { DragScrollComponent } from 'ngx-drag-scroll';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { PaginationModule } from 'ngx-bootstrap/pagination';


@NgModule({
  declarations: [
    MainComponent,
    FormComponent
  ],
  imports: [
    CommonModule,
    ListofChildcareSubsidyRecipientsMaintenanceRoutingModule,
    PaginationModule.forRoot(),
    FormsModule,
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
    DragScrollComponent,
    TypeaheadModule.forRoot(),
    NgxMaskDirective,
    NgxMaskPipe,
    SharedModule
  ]
})
export class ListofChildcareSubsidyRecipientsMaintenanceModule { }
