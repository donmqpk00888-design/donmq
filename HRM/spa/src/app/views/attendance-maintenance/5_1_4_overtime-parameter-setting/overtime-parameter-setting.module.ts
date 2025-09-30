import { TranslateModule } from '@ngx-translate/core';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainComponent } from './main/main.component';
import { OvertimeParameterSettingRoutingModule } from './overtime-parameter-setting-routing.module';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { AddComponent } from './add/add.component';
import { EditComponent } from './edit/edit.component';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { SharedModule } from '@views/_shared/shared.module';
@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    PaginationModule.forRoot(),
    TranslateModule,
    BsDatepickerModule.forRoot(),
    NgSelectModule,
    OvertimeParameterSettingRoutingModule,
    NgxMaskDirective,
    NgxMaskPipe,
    SharedModule
  ],
  declarations: [MainComponent, AddComponent, EditComponent],
})
export class OvertimeParameterSettingModule { }
