import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ChildcareSubsidyGenerationParam, ChildcareSubsidyGenerationSourceTab2 } from '@models/salary-maintenance/7_1_15_childcare-subsidy-generation';
import { S_7_1_15_ChildcareSubsidyGenerationService } from '@services/salary-maintenance/s_7_1_15_childcare-subsidy-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'report-printing',
  templateUrl: './tab-2.component.html',
  styleUrl: './tab-2.component.scss'
})
export class Tab2Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: ChildcareSubsidyGenerationParam = <ChildcareSubsidyGenerationParam>{
    permissionGroupMultiple: [],
    kind_Tab2: 'S',
  };
  year_Month: Date;
  totalRows: number = 0;

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  listFactory: KeyValuePair[] = [];
  listKind: KeyValuePair[] = [
    {
      key: 'S',
      value:
        'SalaryMaintenance.ChildcareSubsidyGeneration.MonthlyDepartmentSubtotal',
    },
    {
      key: "D",
      value: 'SalaryMaintenance.ChildcareSubsidyGeneration.MonthlyChildcareSubsidyDetail'
    },
  ];
  listPermissionGroup: KeyValuePair[] = [];


  constructor(private _service: S_7_1_15_ChildcareSubsidyGenerationService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.loadDropDownList();
      });
  }

  ngOnInit(): void {
    this.getDataFromSource()
  }

  ngOnDestroy(): void {
    this._service.setSource1(<ChildcareSubsidyGenerationSourceTab2>{
      param: this.param,
      totalRows: this.totalRows,
      year_Month: this.year_Month
    })
  }

  getDataFromSource() {
    this.param = this._service.programSource2().param;
    this.totalRows = this._service.programSource2().totalRows;
    this.year_Month = this._service.programSource2().year_Month;
    this.loadDropDownList()
  }

  reportPrintingExecute() {
    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmExecution'),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.download();
        this.getTotalRows();
      });
  }

  //#region download
  download() {
    this.spinnerService.show();
    this.param.yearMonth = this.year_Month.toStringYearMonth();
    this._service.downloadExcel(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.param.kind_Tab2 == 'S'
            ? this.functionUtility.getFileName('ChildcareSubsidyGenerationMonthlyDepartmentSubtotal_Download')
            : this.functionUtility.getFileName('ChildcareSubsidyGenerationMonthlyChildcareSubsidyDetail_Download')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        else this.functionUtility.snotifySuccessError(false, res.error)
        this.spinnerService.hide();
      }
    });
  }
  //#endregion

  clear() {
    this.param = <ChildcareSubsidyGenerationParam>{
      kind_Tab2: "S",
    }
    this.year_Month = null;
  }

  onFactoryClear(): void {
    this.param.permissionGroupMultiple = null;
    this.listPermissionGroup = [];
  }

  onFactoryChange(): void {
    this.param.permissionGroupMultiple = null;
    this.listPermissionGroup = [];

    if (this.param.factory) {
      this.getListPermissionGroup();
    }
  }

  loadDropDownList() {
    this.getListFactoryByUser();
    this.getListPermissionGroup();
  }

  //#region Get List
  getListFactoryByUser() {
    this._service.getListFactoryByUser().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  //#endregion

  //#region Get Total Rows
  getTotalRows() {
    this.param.yearMonth = this.year_Month.toStringYearMonth();
    this._service.getTotalTab2(this.param).subscribe({
      next: (res) => {
        this.totalRows = res;
      },
    });
  }
  //#endregion

  //#region Get List PermissionGroup
  getListPermissionGroup() {
    this._service.getListPermissionGroupByFactory(this.param.factory).subscribe({
      next: (res) => {
        this.listPermissionGroup = res;
        this.selectAllForDropdownItems(this.listPermissionGroup)
      },
    });
  }
  //#endregion

  //#region Select All For Dropdown Items
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }
  //#endregion
}
