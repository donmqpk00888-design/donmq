import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { TabComponent } from './tab-component/tab.component';
import { FileUploadComponent } from './file-upload-component/file-upload.component';
import { ThemeSwitchComponent } from './theme-switch/theme-switch.component';
import { DragDropModule } from '@angular/cdk/drag-drop';

@NgModule({
  declarations: [TabComponent, FileUploadComponent, ThemeSwitchComponent],
  exports: [TabComponent, FileUploadComponent, ThemeSwitchComponent],
  imports: [
    CommonModule,
    DragDropModule,
    TabsModule.forRoot()
  ]
})
export class SharedModule { }
