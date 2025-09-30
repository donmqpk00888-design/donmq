import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateModule } from '@ngx-translate/core';
import { MainComponent } from "./main/main.component";
import { SwipeCardDataUploadRoutingModule } from './swipe-card-data-upload-routing.module';
import { SharedModule } from '@views/_shared/shared.module';

@NgModule({
  declarations: [
    MainComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    SwipeCardDataUploadRoutingModule,
    TranslateModule,
    NgSelectModule,
    SharedModule
  ]
})
export class SwipeCardDataUploadModule { }
