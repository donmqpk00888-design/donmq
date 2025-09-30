import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Swipe_Card_Upload } from '@models/attendance-maintenance/5_1_13_swipe-card-data-upload';
import { S_5_1_13_SwipeCardDataUploadService } from '@services/attendance-maintenance/s_5_1_13_swipe-card-data-upload.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('fileInput') fileInput!: ElementRef;
  currentUser = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));

  //#region Data
  factories: KeyValuePair[] = [];
  //#endregion

  //#region object
  param: HRMS_Att_Swipe_Card_Upload = <HRMS_Att_Swipe_Card_Upload>{
    factory: '',
    fileUpload: null
  }
  //#endregion

  //#region Vaiables
  accept: string = '.txt';
  title: string = '';
  processedRecords: number = null;
  iconButton = IconButton;
  //#endregion

  constructor(private _services: S_5_1_13_SwipeCardDataUploadService) {
    super();
    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getFactories();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getFactories();
  }

  //#region Methods
  getFactories() {
    this._services.getFactories().subscribe({
      next: result => {
        this.factories = result;
      }
    })
  }
  //#endregion
  //#endregion

  //#region Events
  upload(event: FileResultModel) {
    this.snotifyService.confirm(
      this.translateService.instant('AttendanceMaintenance.SwipeCardDataUpload.ConfirmExecution'),
      this.translateService.instant('System.Action.Confirm'),
      () => {
        this.spinnerService.show();
        this.param.fileUpload = event.fileModel[0].file
        this._services.excute(this.param).subscribe({
          next: (result) => {
            this.spinnerService.hide();
            if (result.isSuccess) {
              this.functionUtility.snotifySuccessError(result.isSuccess, this.translateService.instant('System.Message.UploadOKMsg'))
              this.processedRecords = result.data;
            }
            else {
              this.processedRecords = null;
              if (result.data != null) {
                const fileName = this.functionUtility.getFileName('SwipeCardDataReport_Report')
                this.functionUtility.exportExcel(result.data, fileName);
              }
              else this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
            }
          }
        })
      }
    )
  }
  //#endregion
}
