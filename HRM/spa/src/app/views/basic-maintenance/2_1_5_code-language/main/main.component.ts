import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { Code_Language, Code_LanguageParam, Code_LanguageSource } from '@models/basic-maintenance/2_1_5_code-language';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_5_CodeLanguageService } from '@services/basic-maintenance/s_2_1_5_code-language.service';
import { InjectBase } from '@utilities/inject-base-app';
import { OperationResult } from '@utilities/operation-result';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit {
  listData: Code_Language[] = [];


  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }

  param: Code_LanguageParam = <Code_LanguageParam>{}

  iconButton = IconButton;
  title: string = '';
  programCode: string = '';

  constructor(
    private service: S_2_1_5_CodeLanguageService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
    effect(() => {
      this.param = this.service.codeLanguageSource().param;
      this.pagination = this.service.codeLanguageSource().pagination;
      this.listData = this.service.codeLanguageSource().data;
      if (this.listData.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clearSearch()
        else this.getData()
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  sourceSet() {
    this.service.codeLanguageSource().data = this.listData;
    this.service.codeLanguageSource().pagination = this.pagination;
    this.service.codeLanguageSource().param = this.param;
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.changeParamSearch(this.param);
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.listData = res.result;
        this.pagination = res.pagination;
        this.spinnerService.hide();

        this.sourceSet();
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');

      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  clearSearch() {
    this.param = <Code_LanguageParam>{}
    this.pagination.totalCount = 0
    this.listData = []
    this.sourceSet()
  }

  delete(item: any) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.deleteData(item).subscribe({
        next: (result: OperationResult) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.getData();
        }
      });
    });
  }
  setParamSource(item: Code_Language) {
    // set param
    return <Code_LanguageSource>{
      param: { ...this?.param },
      pagination: this.pagination,
      source: { ...item },
      data: this.listData
    }
  }

  onForm(item: Code_Language = null) {
    this.service.setSource(this.setParamSource(item ?? <Code_Language>{}));
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item == null ? 'add' : 'edit'}`]);
  }

  onDetail(item: Code_Language) {
    this.service.setSource(this.setParamSource(item));
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  export() {
    this.spinnerService.show();
    this.service.exportExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide()
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        this.functionUtility.exportExcel(result.data, fileName);
      }
    })

  }

}
