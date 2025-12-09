import { TranslateModule } from '@ngx-translate/core';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DirectWorkTypeAndSectionSettingRoutingModule } from './3_5_direct-work-type-and-section-setting-routing.module';
import { FormsModule } from '@angular/forms';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { NgSelectModule } from '@ng-select/ng-select';
import { MainComponent } from './main/main.component';
import { AddComponent } from './add/add.component';
import { EditComponent } from './edit/edit.component';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NgChartsModule } from 'ng2-charts';

@NgModule({
  imports: [
    CommonModule,
    DirectWorkTypeAndSectionSettingRoutingModule,
    FormsModule,
    PaginationModule.forRoot(),
    BsDatepickerModule.forRoot(),
    TranslateModule,
    NgSelectModule,
    NgChartsModule
  ],
  declarations: [MainComponent, AddComponent, EditComponent]
})
export class DirectWorkTypeAndSectionSettingModule { }
