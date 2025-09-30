import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { DepartmenttoSAPCostCenterMappingMaintenanceRoutingModule } from './department-to-sap-cost-center-mapping-routing.module';
import { NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
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
    DepartmenttoSAPCostCenterMappingMaintenanceRoutingModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    ReactiveFormsModule,
    BsDatepickerModule.forRoot(),
    PaginationModule.forRoot(),
    NgxMaskDirective, NgxMaskPipe,
    SharedModule
  ],
  providers: [provideNgxMask(),]
})
export class DepartmenttoSAPCostCenterMappingMaintenanceModule { }
