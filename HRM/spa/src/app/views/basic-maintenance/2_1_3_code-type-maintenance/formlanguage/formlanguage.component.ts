import { AfterViewInit, Component, input, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  Language_Dto,
  LanguageDetail_Dto,
  languageDto,
  languageSource,
} from '@models/basic-maintenance/2_1_3_type-code-maintenance';
import { S_2_1_3_CodeTypeMaintenanceService } from '@services/basic-maintenance/s_2_1_3_code-type-maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './formlanguage.component.html',
  styleUrls: ['./formlanguage.component.scss'],
})
export class FormLanguageComponent extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  url: string = '';
  data: languageDto = <languageDto>{};
  language: Language_Dto = <Language_Dto>{
    type_Seq: '',
    detail_Dto: []
  };
  iconButton = IconButton
  isEdit: boolean = false;

  constructor(
    private service: S_2_1_3_CodeTypeMaintenanceService,
    private modalService: ModalService
  ) {
    super()
  }

  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  open(data: languageSource): void {
    const source = structuredClone(data);
    this.isEdit = source.isEdit
    this.data = source.source;
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.isEdit ? this.getDetail(this.data.type_Seq) : this.getLanguage();
    this.directive.show()
  }
  close = () => this.directive.hide();
  getLanguage() {
    this.service.getLanguage().subscribe({
      next: (res) => {
        this.language.detail_Dto = res.map(x => {
          return <LanguageDetail_Dto>{ language_Code: x.key, type_Name: x.key === 'TW' ? this.data.type_Name : '' }
        });
      }
    })
  }

  getDetail(type_Seq: string) {
    this.service.getDetail(type_Seq).subscribe({
      next: (res) => {
        this.language = res;
        for (let deltailItem of this.language.detail_Dto) {
          if (deltailItem.language_Code === 'TW')
            deltailItem.type_Name = this.data.type_Name;
          else
            deltailItem.type_Name = deltailItem.type_Name;
        }
      }
    })
  }
  checknullTW() {
    for (let deltailItem of this.language?.detail_Dto) {
      if (deltailItem.language_Code === 'TW' && deltailItem.type_Name === '')
        return true;
    }
    return false;
  }

  back = () => this.router.navigate([this.url]);
  save() {
    this.language.type_Seq = this.data.type_Seq
    this.spinnerService.show();
    this.service[!this.isEdit ? 'createLanguage' : 'EditLanguageCode'](this.language).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess,
          result.isSuccess ? (!this.isEdit ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') : result.error,
          result.isSuccess);
        if (result.isSuccess) {
          this.directive.hide();
          this.back();
        }
      }
    });
  }
}

