import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import {
  DirectoryProgramLanguageSetting_Param,
  DirectoryProgramLanguageSetting_Data,
  Language
} from '@models/system-maintenance/1_1_4_directory-program-language';
import { S_1_1_4_DirectoryProgramLanguageSettingService } from '@services/system-maintenance/s_1_1_4_directory-program-language-setting.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { LangChangeEvent } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  iconButton = IconButton;
  programDirectory: DirectoryProgramLanguageSetting_Param = <DirectoryProgramLanguageSetting_Param>{};
  title: string = ''
  action: string = '';
  url: string  = '/dashboard';
  data: DirectoryProgramLanguageSetting_Data = <DirectoryProgramLanguageSetting_Data>{
    kind: 'P',
    code: '',
    langs: []
  };
  listProgram: KeyValuePair[] = [];
  key: KeyValuePair[] = [
    { key: 'P', value: 'SystemMaintenance.DirectoryProgramLanguageSetting.Program' },
    { key: 'D', value: 'SystemMaintenance.DirectoryProgramLanguageSetting.Directory' }
  ];
  constructor(private service: S_1_1_4_DirectoryProgramLanguageSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    this.getDataFromSource()
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title
    });
  }


  onSelectChange() {
    if (this.data.kind === 'P') {
      this.getProgram();
    } else if (this.data.kind === 'D') {
      this.getDirectory();
    }
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.programSource();
      if (this.action == 'Edit') {
        if (source.selectedData && Object.keys(source.selectedData).length > 0) {
          this.programDirectory = structuredClone(source.selectedData);
          this.getDetail(this.programDirectory.kind, this.programDirectory.code);
        }
        else {
          this.back()
        }
      } else {
        this.getLanguage();
        this.getProgram();
      }
    })
  }
  // Lấy danh sách dữ liệu có trong DB để đổ lên onEdit
  getDetail(kind: string, code: string) {
    this.data.kind = this.programDirectory.kind;
    this.service.getDetail(kind, code).subscribe({
      next: (res) => {
        this.data = res
      }
    })
  }

  // Lấy danh sách Language Code + Name
  getLanguage() {
    this.service.getLanguage().subscribe({
      next: (res) => {
        this.data.langs = res.map(x => { return <Language>{ lang_Code: x.key, lang_Name: '' } });
      }
    })
  }
  // Lấy danh sách Code Program
  getProgram() {
    this.service.getProgram().subscribe({
      next: (res) => {
        this.listProgram = res
        this.data.code = this.listProgram[0].key
      }
    })
  }
  // Lấy danh sách Code Directory
  getDirectory() {
    this.service.getDirectory().subscribe({
      next: (res) => {
        this.listProgram = res
        this.data.code = this.listProgram[0].key
      }
    })
  }

  back = () => this.router.navigate([this.url]);

  saveChange() {
    this.spinnerService.show();
    if (this.action != 'Edit') {
      this.service.addNew(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
    else {
      this.service.edit(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
            this.back();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
  }
}

