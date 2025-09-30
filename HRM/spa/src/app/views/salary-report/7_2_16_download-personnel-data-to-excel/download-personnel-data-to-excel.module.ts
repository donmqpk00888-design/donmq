import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import {MainComponent} from './main/main.component'
import { DownloadPersonnelDataToExcelRoutingModule } from './download-personnel-data-to-excel-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { NgSelectModule } from '@ng-select/ng-select';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { TypeaheadModule } from 'ngx-bootstrap/typeahead';


@NgModule({
  declarations: [
    MainComponent
  ],
  imports: [
    CommonModule,
    DownloadPersonnelDataToExcelRoutingModule,
        FormsModule,
        TranslateModule,
        NgSelectModule,
        ReactiveFormsModule,
        BsDatepickerModule.forRoot(),
        PaginationModule.forRoot(),
        NgxMaskDirective, NgxMaskPipe,
        TypeaheadModule.forRoot()
  ]
})
export class DownloadPersonnelDataToExcelModule { }
