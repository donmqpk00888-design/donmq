import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ChildcareSubsidyGenerationParam, ChildcareSubsidyGenerationSourceTab1 } from '@models/salary-maintenance/7_1_15_childcare-subsidy-generation';
import { S_7_1_15_ChildcareSubsidyGenerationService } from '@services/salary-maintenance/s_7_1_15_childcare-subsidy-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { log } from 'console';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'child-subsidy-is-generated',
  templateUrl: './tab-1.component.html',
  styleUrl: './tab-1.component.scss'
})
export class Tab1Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: ChildcareSubsidyGenerationParam = <ChildcareSubsidyGenerationParam>{
    permissionGroupMultiple: [],
    kind_Tab1: 'O',
  };

  totalRows: number = 0;
  year_Month: Date;
  dateFrom: Date;
  dateTo: Date;

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  listFactory: KeyValuePair[] = [];
  listKind: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'SalaryMaintenance.ChildcareSubsidyGeneration.Onjob',
    },
    {
      key: "R",
      value: 'SalaryMaintenance.ChildcareSubsidyGeneration.Resigned'
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
    this._service.setSource(<ChildcareSubsidyGenerationSourceTab1>{
      param: this.param,
      dateFrom: this.dateFrom,
      dateTo: this.dateTo,
      totalRows: this.totalRows,
      year_Month: this.year_Month
    })
  }

  getDataFromSource() {
    this.param = this._service.programSource1().param;
    this.dateFrom = this._service.programSource1().dateFrom;
    this.dateTo = this._service.programSource1().dateTo;
    this.totalRows = this._service.programSource1().totalRows;
    this.year_Month = this._service.programSource1().year_Month;
    this.loadDropDownList()
  }


  childcareSubsidyGenerationExecute() {
    this.param.yearMonth = this.year_Month.toStringYearMonth();
    this.param.resignedDate_Start = this.dateFrom ? this.dateFrom.toStringDate() : "";
    this.param.resignedDate_End = this.dateTo ? this.dateTo.toStringDate() : "";

    this.snotifyService.confirm(
      this.translateService.instant('System.Message.ConfirmExecution'),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show();
        this._service.checkParam(this.param).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              if (res.error == 'DeleteData') {
                this.snotifyService.confirm(
                  this.translateService.instant(
                    'SalaryMaintenance.ChildcareSubsidyGeneration.MessageConfirm'
                  ),
                  this.translateService.instant('System.Caption.Confirm'),
                  () => {
                    this.param.is_Delete = true;
                    this.childcareSubsidyGeneration();
                  }
                );
              } else {
                this.param.is_Delete = false;
                this.childcareSubsidyGeneration();
              }
            } else {
              this.snotifyService.error(res.error,
                this.translateService.instant('System.Caption.Error')
              );
            }
          }
        });
      });
  }

  childcareSubsidyGeneration() {
    this.spinnerService.show();
    this._service.insertData(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.totalRows = res.data
        } else {
          this.totalRows = 0;
          this.snotifyService.error(
            res.error ?? this.translateService.instant('System.Message.CreateErrorMsg'),
            this.translateService.instant('System.Caption.Error')
          );
        }
        this.spinnerService.hide();
      },
    });
  }

  isValidDateRange() {
    return (this.dateFrom && this.dateTo) || (!this.dateFrom && !this.dateTo)
  }

  clear() {
    this.param = <ChildcareSubsidyGenerationParam>{
      kind_Tab1: "O"
    }
    this.year_Month = null;
    this.dateFrom = null;
    this.dateTo = null;
  }

  onFactoryClear() {
    this.param.permissionGroupMultiple = [];
  }

  onFactoryChange() {
    this.onFactoryClear();
    this.listPermissionGroup = [];
    this.getListPermissionGroup();
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

  //#region Get List Permission Group
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

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
