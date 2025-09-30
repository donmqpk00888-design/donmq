import {
  AfterViewInit,
  Component,
  ElementRef,
  input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import {
  DocumentManagement_DownloadFileModel,
  DocumentManagement_FileModel,
  DocumentManagement_ModalInputModel
} from '@models/employee-maintenance/4_1_9_document-management';
import { S_4_1_9_DocumentManagement } from '@services/employee-maintenance/s_4_1_9_document-management.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { FileResultModel } from '@views/_shared/file-upload-component/file-upload.component';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
  host: { 'id': 'modal-4-1-9' }
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive?: ModalDirective;
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  id = input<string>(this.modalService.defaultModal)

  data: DocumentManagement_ModalInputModel = <DocumentManagement_ModalInputModel>{ file_List: [] }
  iconButton = IconButton;
  classButton = ClassButton;
  isSave: boolean = false
  constructor(
    private service: S_4_1_9_DocumentManagement,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.data })
  open(data: DocumentManagement_ModalInputModel): void {
    this.data = structuredClone(data);
    this.isSave = false
    this.directive.show()
  }
  close() {
    this.directive.hide()
  }
  save() {
    this.isSave = true
    this.directive.hide();
  }
  upload(event: FileResultModel) {
    const fileNames = this.data.file_List.map(x => x.name).concat(event.fileModel.map(x => x.name));
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
    this.data.file_List.push(...event.fileModel)
  }
  calculateId() {
    this.data.file_List.map((val, ind) => {
      val.id = ind + 1
    })
  }
  remove(item: DocumentManagement_FileModel) {
    this.data.file_List = this.data.file_List.filter(x => x.name != item.name)
    this.calculateId()
  }
  download(item: DocumentManagement_FileModel) {
    if (item.content && item.content.includes('base64')) {
      var link = document.createElement('a');
      document.body.appendChild(link);
      link.setAttribute("href", item.content);
      link.setAttribute("download", item.name);
      link.click();
    } else {
      this.spinnerService.show();
      let data: DocumentManagement_DownloadFileModel = <DocumentManagement_DownloadFileModel>{
        division: this.data.division,
        factory: this.data.factory,
        ser_Num: this.data.ser_Num,
        employee_Id: this.data.employee_Id,
        file_Name: item.name
      }
      this.service.downloadFile(data)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              var link = document.createElement('a');
              document.body.appendChild(link);
              link.setAttribute("href", res.data.content);
              link.setAttribute("download", res.data.name);
              link.click();
            }
            else this.functionUtility.snotifySuccessError(false, `EmployeeInformationModule.DocumentManagement.${res.error}`)
          }
        });
    }
  }
}
