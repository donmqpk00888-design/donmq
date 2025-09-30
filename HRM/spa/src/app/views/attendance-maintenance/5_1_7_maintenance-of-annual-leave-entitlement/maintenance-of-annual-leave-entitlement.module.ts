import { SharedModule } from '@views/_shared/shared.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MaintenanceofAnnualLeaveEntitlementRoutingModule } from './maintenance-of-annual-leave-entitlement-routing.module';
import { MainComponent } from './main/main.component';
import { FormsModule } from '@angular/forms';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { DragScrollComponent } from 'ngx-drag-scroll';
import { ModalComponent } from './modal/modal.component';
import { ModalModule } from 'ngx-bootstrap/modal';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';

@NgModule({
  declarations: [
    MainComponent,
    ModalComponent
  ],
  imports: [
    CommonModule,
    MaintenanceofAnnualLeaveEntitlementRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    ModalModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    BsDatepickerModule.forRoot(),
    DragScrollComponent,
    NgxMaskDirective,
    NgxMaskPipe,
    TypeaheadModule.forRoot(),
    SharedModule
  ]
})
export class MaintenanceofAnnualLeaveEntitlementModule { }
