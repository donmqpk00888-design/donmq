import { Component, Input, OnInit } from '@angular/core';
import { IconButton, ClassButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import {
  ExistedDataParam,
  SAPCostCenterSettingDto
} from '@models/salary-maintenance/7_1_8_sap-cost-center-setting';
import { S_7_1_8_SapCostCenterSettingService } from '@services/salary-maintenance/s_7_1_8_sap-cost-center-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  @Input('title') formType?: string;
  isEdit: boolean = false;
  listFactory: KeyValuePair[] = [];
  listKind: KeyValuePair[] = [];
  title: string = '';
  tempUrl: string = '';
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  minMode: BsDatepickerViewMode = 'year';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY',
    minMode: this.minMode
  };
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  data: SAPCostCenterSettingDto = <SAPCostCenterSettingDto>{};
  item: SAPCostCenterSettingDto;

  // #region constructor
  constructor(private service: S_7_1_8_SapCostCenterSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getDropDownList()
      });
  }
  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      } else this.back()
    }
    this.getDropDownList()
  }
  getDropDownList() {
    this.getListFactory();
    this.getListKind();
  }

  // #region ngOnInit
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getSource()
  }

  // #region getListFactory
  getListFactory() {
    this.commonService.getListAccountAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  // #region GetListKind
  getListKind() {
    this.service.getListKind().subscribe({
      next: (res) => {
        this.listKind = res;
      },
    });
  }

  // #region onFactoryChange
  onFactoryChange() {
    this.data.company_Code = '';
    this.onUpdateTimeChangeAlways();
  }

  // #region checkExistSAPCompanyCode
  checkExistSAPCompanyCode() {
    this.onUpdateTimeChangeAlways();
    if (this.functionUtility.checkEmpty(this.data.company_Code)) return;
    this.service.checkExistedDataCompanyCode(this.data.factory, this.data.company_Code).subscribe({
      next: (res) => {
        // true is duplicate
        if (res.isSuccess) {
          this.functionUtility.snotifySuccessError(false, "SalaryMaintenance.SAPCostCenterSetting.ExistedCompanyCode");
          this.data.company_Code = '';
        }
      },
    });
  }

  // #region checkExistedCostCenter
  checkExistedCostCenter() {
    this.onUpdateTimeChangeAlways();
    if (this.functionUtility.checkEmpty(this.data.company_Code) || this.functionUtility.checkEmpty(this.data.cost_Code) || this.functionUtility.checkEmpty(this.data.year)) return;
    const _data: ExistedDataParam = <ExistedDataParam>{
      company_Code: this.data.company_Code,
      cost_Code: this.data.cost_Code,
      cost_Year: this.data.year
    };
    this.service.checkExistedCostCenter(_data).subscribe({
      next: (res) => {
        if (!res.isSuccess) {
          this.functionUtility.snotifySuccessError(false, "SalaryMaintenance.SAPCostCenterSetting.ExistedCostCenter");
          this.data.company_Code = '';
        }
      },
    });
  }

  back = () => this.router.navigate([this.tempUrl]);

  // #region save
  save(isNext?: boolean) {
    let action: Observable<OperationResult>
    if (this.isEdit) {
      action = this.service.edit(this.data)
    } else
      action = this.service.addNew(this.data)
    this.spinnerService.show();
    action.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
          this.functionUtility.snotifySuccessError(true, message)
          isNext ? this.data = <SAPCostCenterSettingDto>{ factory: this.data.factory } : this.back();
        } else {
          this.functionUtility.snotifySuccessError(false, result.error)
        }
      }
    })
  }

  onUpdateTimeChangeAlways() {
    this.data.update_By = this.user.id
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
  onDateChange() {
    this.data.year = this.functionUtility.isValidDate(this.data.year_Date) ? this.data.year_Date.getFullYear().toString() : '';
    this.onUpdateTimeChangeAlways()
  }
}

