import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SAPCostCenterSettingRoutingModule } from './sap-cost-center-setting-routing.module';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { DragScrollComponent } from 'ngx-drag-scroll';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { SharedModule } from '@views/_shared/shared.module';


@NgModule({
  declarations: [
    MainComponent,
    FormComponent
  ],
  imports: [
    CommonModule,
    SAPCostCenterSettingRoutingModule,
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
export class SAPCostCenterSettingModule { }
