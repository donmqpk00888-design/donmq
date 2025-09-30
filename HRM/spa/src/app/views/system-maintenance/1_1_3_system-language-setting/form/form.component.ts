import { Component, effect, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { SystemLanguageSetting_Data } from '@models/system-maintenance/1_1_3-system-language-setting';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_1_1_3_SystemLanguageSettingService } from '@services/system-maintenance/s_1_1_3_system-language-setting.service';
import { InjectBase } from '@utilities/inject-base-app';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  param: SystemLanguageSetting_Data = <SystemLanguageSetting_Data>{ isActive: true };
  title: string  = '';
  url: string = '/dashboard'
  action: string = '';

  constructor(
    private service: S_1_1_3_SystemLanguageSettingService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource();
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.action = res.title;
    });
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.basicCodeSource();
      if (this.action == 'Edit') {
        if (source.selectedData && Object.keys(source.selectedData).length > 0) {
          this.param = structuredClone(source.selectedData);
        }
        else
          this.back()
      }
    })
  }

  back = () => this.router.navigate([this.url]);

  save() {
    this.spinnerService.show();
    this.service[this.action == 'Add' ? 'create' : 'update'](this.param).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant(`System.Message.${this.action == 'Add' ? 'CreateOKMsg' : 'UpdateOKMsg'}`),
            this.translateService.instant('System.Caption.Success'));
          this.back();
        }
        else
          this.snotifyService.error(
            this.translateService.instant(`System.Message.${this.action == 'Add' ? 'CreateErrorMsg' : 'UpdateErrorMsg'}`),
            this.translateService.instant('System.Caption.Error'));
      }
    })
  }
}
