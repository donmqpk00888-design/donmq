import { Component, effect, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_2_22_AnnualLeaveCalculationService } from "@services/attendance-maintenance/s_5_2_22_annual-leave-calculation.service";
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ClassButton, IconButton } from '@constants/common.constants';
import { AnnualLeaveCalculationParam, AnnualLeaveCalculationSource } from '@models/attendance-maintenance/5_2_22_annual-leave-calculation';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { LangChangeEvent } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  totalRows: number = 0;
  param: AnnualLeaveCalculationParam = <AnnualLeaveCalculationParam>{
    permission_Group: [],
  }
  totalPermissionGroup: number = 0;
  start_Year_Month: Date = new Date().toFirstDateOfYear();
  end_Year_Month: Date = new Date();
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  kinds: KeyValuePair[] = [
    { key: 'O', value: 'AttendanceMaintenance.AnnualLeaveCalculation.OnJob' },
    { key: 'R', value: 'AttendanceMaintenance.AnnualLeaveCalculation.Resigned' },
  ];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  constructor(private service: S_5_2_22_AnnualLeaveCalculationService) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });

    effect(() => {
      this.param = this.service.programSource().param;
      this.totalRows = this.service.programSource().totalRows;
      this.loadDropDownList();
      this.setQueryDate();
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  ngOnDestroy(): void {
    this.service.setSource(<AnnualLeaveCalculationSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }



  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }

  setQueryDate(){
    if(this.param.start_Year_Month)
      this.start_Year_Month = new Date(this.param.start_Year_Month)
    if(this.param.end_Year_Month)
      this.end_Year_Month = new Date(this.param.end_Year_Month);
  }

  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.totalRows = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }

  clear(){
    this.start_Year_Month = new Date().toFirstDateOfYear();
    this.end_Year_Month = new Date();
    this.totalRows = 0;
    this.param = <AnnualLeaveCalculationParam> {
      permission_Group: [],
      start_Year_Month: this.start_Year_Month.toStringYearMonth(),
      end_Year_Month: this.end_Year_Month.toStringYearMonth(),
      kind: 'O'
    }
  }

  download(){
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if(res.isSuccess){
          this.totalRows = res.data.totalRows
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.excel, fileName);
        } else {
          this.totalRows = 0
          this.snotifyService.warning(res.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    })
  }

  onSelectFactory(){
    this.deleteProperty('department')
    this.getListDepartment();
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onChangePermission(){
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  onChangeYearMonth(name: string){
    this.param[name] = this[name] != null ? this[name].toStringYearMonth() : null
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  //#region Get List
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.factory).subscribe({
      next: res => this.listDepartment = res
    })
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
  //#endregion
}
