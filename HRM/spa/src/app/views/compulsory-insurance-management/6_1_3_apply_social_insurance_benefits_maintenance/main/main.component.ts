import { Component, ElementRef, OnDestroy, OnInit, ViewChild, } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import {
  ApplySocialInsuranceBenefitsMaintenance_Basic,
  ApplySocialInsuranceBenefitsMaintenanceDto,
  ApplySocialInsuranceBenefitsMaintenanceParam,
} from '@models/compulsory-insurance-management/6_1_3_apply_social_insurance_benefits_maintenance';
import { S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService } from '@services/compulsory-insurance-management/s_6_1_3_apply_social_insurance_benefits_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  title: string = '';
  programCode: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  param: ApplySocialInsuranceBenefitsMaintenanceParam = <ApplySocialInsuranceBenefitsMaintenanceParam>{};
  data: ApplySocialInsuranceBenefitsMaintenanceDto[] = [];
  selectedData: ApplySocialInsuranceBenefitsMaintenanceDto = <ApplySocialInsuranceBenefitsMaintenanceDto>{}
  pagination: Pagination = <Pagination>{};
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listBenefitsKind: KeyValuePair[] = [];
  employeeList: EmployeeCommonInfo[] = [];

  constructor(
    private service: S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
      this.processData()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.param = this.service.paramSource().param;
    this.pagination = this.service.paramSource().pagination;
    this.data = this.service.paramSource().data;
    this.processData()
  }
  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkValidate()) {
        this.getData(false)
      }
      else
        this.clear()
    }
    this.getListFactory();
    this.getListBenefitsKind();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  getListBenefitsKind() {
    this.service.getListBenefitsKind().subscribe({
      next: (res) => {
        this.listBenefitsKind = res;
      },
    });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    if (!this.param.declaration_Seq) this.deleteProperty('declaration_Seq');
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(
            true,
            'System.Message.QuerySuccess'
          );
        this.spinnerService.hide();
      },
    });
  }

  ngOnDestroy(): void {
    const source = <ApplySocialInsuranceBenefitsMaintenance_Basic>{
      pagination: this.pagination,
      param: this.param,
      selectedData: this.selectedData,
      data: this.data,
    };
    this.service.setSource(source);
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber = 1;
    this.getData(isSearch)
  }

  pageChanged(event: any) {
    if (this.pagination.pageNumber !== event.page) {
      this.pagination.pageNumber = event.page;
      this.getData();
    }
  }
  clear() {
    this.param = <ApplySocialInsuranceBenefitsMaintenanceParam>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
  }
  deleteProperty = (name: string) => delete this.param[name];

  onForm(item: ApplySocialInsuranceBenefitsMaintenanceDto = null) {
    if (item != null) this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }
  delete(item: ApplySocialInsuranceBenefitsMaintenanceDto) {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'),
      () => {
        this.spinnerService.show();
        this.service.delete(item).subscribe({
          next: (result) => {
            this.spinnerService.hide()
            if (result.isSuccess) {
              this.functionUtility.snotifySuccessError(
                result.isSuccess,
                result.error
              );
              this.getData(false);
            } else
              this.functionUtility.snotifySuccessError(
                result.isSuccess,
                result.error
              );
          }
        });
      }
    );
  }
  onChangeSeq(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.value === '0') {
      input.value = '';
    }
  }

  download() {
    this.spinnerService.show();
    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
      },
    });
  }
  onChangeDate(name: string) {
    this.param[`${name}_Str`] = this.param[name] ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }

  checkValidate() {
    return this.param.factory && this.param.start_Year_Month_Str && this.param.end_Year_Month_Str
  }
}
