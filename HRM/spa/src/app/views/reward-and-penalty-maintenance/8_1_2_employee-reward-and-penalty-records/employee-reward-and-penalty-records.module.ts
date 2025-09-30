import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmployeeRewardAndPenaltyRecordsRoutingModule } from './employee-reward-and-penalty-records-routing.module';
import { MainComponent } from './main/main.component';
import { FormComponent } from './form/form.component';
import { ModalComponent } from './modal/modal.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { PipesModule } from 'src/app/_core/pipes/pipes.module';
import { NgxSpinnerModule } from 'ngx-spinner';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { ModalModule } from 'ngx-bootstrap/modal';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
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
    ModalModule.forRoot(),
    BsDatepickerModule.forRoot(),
    TypeaheadModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    SharedModule,
    EmployeeRewardAndPenaltyRecordsRoutingModule,
  ],
})
export class EmployeeRewardAndPenaltyRecordsModule {}
