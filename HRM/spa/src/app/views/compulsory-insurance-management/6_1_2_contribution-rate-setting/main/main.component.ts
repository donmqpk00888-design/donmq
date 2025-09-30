import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ContributionRateSettingDto, ContributionRateSettingParam, ContributionRateSettingSource } from '@models/compulsory-insurance-management/6_1_2_contribution-rate-setting';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_6_1_2_ContributionRateSettingService } from '@services/compulsory-insurance-management/s_6_1_2_contribution-rate-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  classButton = ClassButton;
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  param: ContributionRateSettingParam = <ContributionRateSettingParam>{
  }
  data: ContributionRateSettingDto[] = [];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  checkSearch: boolean = false
  title: string = ''
  totalPermissionGroup: number = 0;
  effective_Month: Date

  listFactory: KeyValuePair[] = [];
  listInsuranceType: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  constructor(
    private service: S_6_1_2_ContributionRateSettingService
  ) {
    super();
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListPermissionGroup();
      if (!this.functionUtility.checkEmpty(this.param.factory) && !this.functionUtility.checkEmpty(this.effective_Month)) {
        this.param.effective_Month = this.functionUtility.getDateFormat(new Date(this.param.effective_Month))
      }
      if (this.functionUtility.checkFunction('Search') && this.data.length > 0)
        this.getData();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
    this.getListPermissionGroup();
  }

  ngOnDestroy() {
    this.service.setSource(<ContributionRateSettingSource>{
      source: this.sourceItem,
      paramQuery: { ...this.param },
      dataMain: this.data,
      pagination: this.pagination,
    });
  }

  getDataFromSource() {
    this.service.source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(source => {
      if (source && source != null) {
        this.pagination = source.pagination;
        this.param = source.paramQuery;
        this.getListFactory();
        this.getListPermissionGroup();
        if (this.functionUtility.isValidDate(new Date(this.param.effective_Month)))
          this.effective_Month = new Date(this.param.effective_Month);
        if (source.dataMain.length > 0) {
          if (this.functionUtility.checkFunction('Search')
            && !this.functionUtility.checkEmpty(this.param.factory)
            && !this.functionUtility.checkEmpty(this.effective_Month)
            && this.param.permission_Group.length != 0) {
            this.checkDisabledButton()
            this.getData();
          }
        }
      }
    })
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }

  redirectToForm = (isEdit: boolean = false) => this.router.navigate([`${this.router.routerState.snapshot.url}/${isEdit ? 'edit' : 'add'}`]);
  sourceItem: ContributionRateSettingDto;
  onForm(item: ContributionRateSettingDto = null) {
    this.sourceItem = item != null ? { ...item } : <ContributionRateSettingDto>{}
    this.redirectToForm(item != null);
  }

  delete(item: ContributionRateSettingDto) {
    item.effective_Month_Str = this.functionUtility.getDateFormat(new Date(item.effective_Month))
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show()
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          if (res.isSuccess) {
            this.getData();
            this.snotifyService.success(this.translateService.instant('System.Message.DeleteOKMsg'), this.translateService.instant('System.Caption.Success'));
          }
          else {
            this.snotifyService.error(this.translateService.instant('System.Message.DeleteErrorMsg'), this.translateService.instant('System.Caption.Error'));
          }
        }
      })
    });
  }

  search(isSearch: boolean) {
    this.checkDisabledButton()
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }

  clear() {
    this.data = []
    this.param = <ContributionRateSettingParam>{}
    this.pagination.totalCount = 0
    this.pagination.pageNumber = 0
    this.effective_Month = null
    this.getListFactory()
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }

  onChangeFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onChangePermission() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  onChangeEffectiveMonth() {
    this.param.effective_Month = (!this.functionUtility.checkEmpty(this.effective_Month)
      && (this.effective_Month.toString() != 'Invalid Date' && this.effective_Month.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.effective_Month)
      : "";
  }


  checkDisabledButton() {
    this.service.checkSearch(this.param).subscribe({
      next: (res: boolean) => this.checkSearch = res
    });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
      }
    })
  }

  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  validateYearMonth(event: KeyboardEvent): void {
    const inputField = event.target as HTMLInputElement;
    let input = inputField.value;
    const key = event.key;

    const allowedKeys = ['Backspace', 'Tab', 'ArrowLeft', 'ArrowRight'];
    if (allowedKeys.includes(key)) return;

    if (inputField.selectionStart !== inputField.selectionEnd) {
      return;
    }

    if (!/^\d$/.test(key) && key !== '/') {
      event.preventDefault();
      return;
    }

    if (key === '/') {
      if (input.length !== 4 || input.includes('/')) {
        event.preventDefault();
      }
    }

    if (input.includes('/') && input.length > 4 && input.split('/')[1].length >= 2) {
      event.preventDefault();
    }

    if (input === '000' && key === '0') {
      this.resetToCurrentDate(inputField);
      event.preventDefault();
    }

    if (input.includes('/') && input.length === 6) {
      const monthPart = input.split('/')[1] + key;
      const month = parseInt(monthPart, 10);

      if (month < 1 || month > 12 || monthPart === '00') {
        this.resetToCurrentDate(inputField);
        event.preventDefault();
      }
    }
  }

  resetToCurrentDate(inputField: HTMLInputElement): void {
    const currentDate = new Date();
    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    inputField.value = `${year}/${month}`;
  }
  deleteProperty = (name: string) => delete this.param[name]
}
