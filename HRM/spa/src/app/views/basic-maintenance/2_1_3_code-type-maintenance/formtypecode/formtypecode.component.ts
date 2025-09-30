import { Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  HRMS_Basic_Code_TypeDto,
  HRMS_Type_Code_Source,
  languageDto,
  languageSource,
} from '@models/basic-maintenance/2_1_3_type-code-maintenance';
import { S_2_1_3_CodeTypeMaintenanceService } from '@services/basic-maintenance/s_2_1_3_code-type-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { OperationResult } from '@utilities/operation-result';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-formtypecode',
  templateUrl: './formtypecode.component.html',
  styleUrls: ['./formtypecode.component.scss'],
})
export class FormtypecodeComponent extends InjectBase implements OnInit {
  source = <HRMS_Type_Code_Source>(<null>{ contractClients: [] });
  title = '';
  url: string = '';
  isEdit: boolean = false;
  param: HRMS_Basic_Code_TypeDto = <HRMS_Basic_Code_TypeDto>{};
  iconButton: typeof IconButton = IconButton;
  constructor(
    private service: S_2_1_3_CodeTypeMaintenanceService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.isEdit = role.title == 'Edit'
        this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
        this.getSource()
      })
  }
  getSource() {
    if (this.isEdit) {
      let source = this.service.typeCodeSource();
      if (source && Object.keys(source).length > 0)
        this.param = source.source;
      else
        this.back()
    }
  }
  saveChange() {
    this.spinnerService.show();
    this.service[!this.isEdit ? 'createTypeCode' : 'update'](this.param).subscribe({
      next: (result: OperationResult) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess,
          result.isSuccess ? (!this.isEdit ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') : result.error,
          result.isSuccess)
        if (result.isSuccess) this.back();
      }
    });
  }

  onEdit(item: languageDto) {
    let data: languageSource = <languageSource>{
      isEdit: this.isEdit,
      source: item
    };
    this.modalService.open(data);
  }

  back = () => this.router.navigate([this.url]);

}
