import { Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { CodeNameParam, Code_LanguageDetail, Code_LanguageParam, Code_Language_Form } from '@models/basic-maintenance/2_1_5_code-language';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_5_CodeLanguageService } from '@services/basic-maintenance/s_2_1_5_code-language.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  action: string = '';
  typeSeqs: KeyValuePair[] = [];
  listCode: KeyValuePair[] = [];
  codeNameParam: CodeNameParam = <CodeNameParam>{}

  codeLanguageDetail: Code_LanguageDetail = <Code_LanguageDetail>{}
  param: Code_LanguageParam = <Code_LanguageParam>{}
  iconButton = IconButton;
  formType: string = '';

  constructor(
    private service: S_2_1_5_CodeLanguageService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getCodeName()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res['title']
      this.action = res.title
    });
    let source = this.service.codeLanguageSource();
    if (this.formType != 'Add') {
      if (source.source == null)
        this.back();
      else {
        this.param.code = source.source.code;
        this.param.code_Name = source.source.code_Name;
        this.param.type_Seq = source.source.type_Seq;
        this.param.type_Name = this.functionUtility.checkEmpty(source.source.type_Name) ? '' : source.source.type_Name;
        this.getDetail(this.param);
      }
    }
    else {
      this.getTypeSeq();
      this.getLanguage();
    }
  }

  cleanTypeSeqChange() {
    this.codeLanguageDetail.code = null
    this.codeLanguageDetail.code_Name = ''
    this.codeLanguageDetail.detail.forEach(x => {
      x.name = ''
    })
  }

  getTypeSeq() {
    this.service.getTypeSeq().subscribe({
      next: (res) => {
        this.typeSeqs = res;
      }
    })
  }

  getCode(item: any) {
    this.spinnerService.show();
    this.cleanTypeSeqChange()
    this.service
      .getCode(item)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.listCode = res;
        }
      });
  }

  onTypeSeqChange(item: any) {
    if (this.functionUtility.checkEmpty(this.codeLanguageDetail.type_Seq)) {
      this.cleanTypeSeqChange();
    }
  }

  onCodeChange() {
    if (this.functionUtility.checkEmpty(this.codeLanguageDetail.code)) {
      this.codeLanguageDetail.code_Name = '',
        this.codeLanguageDetail.detail.forEach(x => {
          x.name = ''
        })
    }
  }

  getCodeName() {
    this.codeNameParam.type_Seq = this.codeLanguageDetail.type_Seq;
    this.codeNameParam.code = this.codeLanguageDetail.code;
    this.spinnerService.show();
    this.service.getCodeName(this.codeNameParam).subscribe({
      next: (res) => {
        this.codeLanguageDetail.code_Name = res[0];
        this.spinnerService.hide();
      }
    })
  }

  getLanguage() {
    this.service.getLanguage().subscribe({
      next: (res) => {
        this.codeLanguageDetail.detail = res.map(x => {
          return <Code_Language_Form>{ language_Code: x.key, name: '' }
        });
      }
    })
  }

  getDetail(param: Code_LanguageParam) {
    this.spinnerService.show();
    this.service.getDetail(param).subscribe({
      next: (res) => {
        this.codeLanguageDetail = res;
        this.spinnerService.hide();
      }
    });
  }

  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();

  save() {
    this.spinnerService.show();
    this.service[this.formType.toLowerCase()](this.codeLanguageDetail).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess,
          (result.isSuccess ? (this.formType == "Add" ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') : result.error),
          result.isSuccess)
        if (result.isSuccess) this.back();
      }
    })
  }
}
