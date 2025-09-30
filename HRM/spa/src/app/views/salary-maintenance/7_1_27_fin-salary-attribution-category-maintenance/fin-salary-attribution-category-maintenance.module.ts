import { PipesModule } from '../../../_core/pipes/pipes.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgxSpinnerModule } from 'ngx-spinner';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { FinSalaryAttributionCategoryMaintenanceRoutingModule } from './fin-salary-attribution-category-maintenance-routing.module';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { ModalModule } from 'ngx-bootstrap/modal';
import { ModalComponent } from './modal/modal.component';
import { SharedModule } from '@views/_shared/shared.module';

@NgModule({
  declarations: [
    MainComponent,
    FormComponent,
    ModalComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    PipesModule,
    ReactiveFormsModule,
    NgxSpinnerModule,
    PaginationModule.forRoot(),
    BsDatepickerModule.forRoot(),
    ModalModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    NgxMaskDirective,
    NgxMaskPipe,
    FinSalaryAttributionCategoryMaintenanceRoutingModule,
    SharedModule
  ]
})
export class FinSalaryAttributionCategoryMaintenanceModule { }
