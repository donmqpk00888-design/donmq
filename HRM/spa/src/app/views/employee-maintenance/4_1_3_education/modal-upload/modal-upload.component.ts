import { AfterViewInit, Component, ElementRef, input, ViewChild } from '@angular/core';
import { EmployeeMode, IconButton } from '@constants/common.constants';
import { EducationFile, EducationUpload } from '@models/employee-maintenance/4_1_3-education';
import { S_4_1_3_EducationService } from '@services/employee-maintenance/s_4_1_3_education.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-upload-4-1-3',
  templateUrl: './modal-upload.component.html',
  styleUrls: ['./modal-upload.component.scss']
})
export class ModalUploadComponent413 extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  @ViewChild('fileInput') fileInput!: ElementRef;
  id = input<string>(this.modalService.defaultModal)

  type: string
  title: string = '';
  iconButton = IconButton;
  accept: string = '*';
  mode = EmployeeMode;
  model: EducationUpload = <EducationUpload>{
    useR_GUID: '',
    files: [] // Danh sách files up lên
  }

  constructor(
    private service: S_4_1_3_EducationService,
    private modalService: ModalService
  ) {
    super()
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  open(source: any): void {
    const _source = structuredClone(source);
    this.model = _source.data as EducationUpload;
    this.type = _source.type
    this.title = this.functionUtility.getTitle(this.service.functions[0]?.program_Code)
    this.getEducationFiles();
    this.directive.show()
  }

  close() {
    this.directive.hide()
  }

  getEducationFiles() {
    if (!this.functionUtility.checkEmpty(this.model.useR_GUID)) {
      this.spinnerService.show();
      this.service.getEducationFiles(this.model.useR_GUID).subscribe({
        next: result => {
          this.spinnerService.hide()
          this.model.files = result.map(x => {
            return {
              uSER_GUID: this.model.useR_GUID,
              fileID: x.fileID,
              fileName: x.fileName,
              serNum: x.serNum,
              file: null,
              fileSize: x.fileSize,
              isDownload: true
            }
          })
        }
      })
    }
  }

  removeEducationFile(educationFile: EducationFile) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.deleteEducationFile(educationFile).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.getEducationFiles();
        }
      })
    })
  }

  /**
   * Có user Guid
   * Có danh sách Files upload
   */
  validateButtonSave() {
    if (this.model.files.filter(x => x.file != null).length > 0
      && !this.functionUtility.checkEmpty(this.model.useR_GUID))
      return false;
    else return true;
  }

  saveChange() {
    this.spinnerService.show()
    let model = <EducationUpload>{
      useR_GUID: this.model.useR_GUID,
      files: this.model.files.filter(x => x.file != null)
    }

    this.service.uploadFiles(model).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.UploadOKMsg' : `System.Message.${result.error}`)
        if (result.isSuccess) {
          this.model.files = [];
          this.getEducationFiles();
        }
      }
    })
  }

  upload(event: FileResultModel) {
    const fileNames = this.model.files.map(x => x.fileName).concat(event.fileModel.map(x => x.name));
    const lookup = fileNames.reduce((a, e) => {
      a[e] = ++a[e] || 0;
      return a;
    }, {});
    const duplicateFiles = [...new Set(fileNames.filter(e => lookup[e]))]
    if (duplicateFiles.length > 0)
      return this.snotifyService.error(
        `${this.translateService.instant('System.Message.ExistedFile')} : \n${duplicateFiles.join('\n')}`,
        this.translateService.instant('System.Caption.Error')
      );
    this.model.files.push(...event.fileModel.map(x => <EducationFile>{
      uSER_GUID: this.model.useR_GUID,
      fileID: null,
      serNum: null,
      isDownload: false,
      fileName: x.name,
      fileSize: x.size,
      file: x.file,
    }))
  }
  onRemoveFile(index: number, item: EducationFile) {
    if (item.file != null)
      this.model.files = this.model.files.filter((x, i) => i != index);
    else this.removeEducationFile(item);
  }

  onDownload(item: EducationFile) {
    this.spinnerService.show();
    this.service.downloadFile(item)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            var link = document.createElement('a');
            document.body.appendChild(link);
            link.setAttribute("href", result.data.file);
            link.setAttribute("download", result.data.fileName);
            link.click();
          }
          else this.functionUtility.snotifySuccessError(false, result.error, false)

        }
      });
  }
}
