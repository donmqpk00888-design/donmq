import { Component, effect, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_2_24_ResignationAnnualLeaveWorktypeAnalysisReportService } from "@services/attendance-maintenance/s_5_2_24_resignation-annual-leave-work-type-analysis-report.service";
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ClassButton, IconButton } from '@constants/common.constants';
import {
  ResignationAnnualLeaveWorktypeAnalysisReportParam,
  ResignationAnnualLeaveWorktypeAnalysisReportSource
} from '@models/attendance-maintenance/5_2_24_resignation-annual-leave-work-type-analysis-report';
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
  param: ResignationAnnualLeaveWorktypeAnalysisReportParam = <ResignationAnnualLeaveWorktypeAnalysisReportParam>{
    permission_Group: [],
  }
  totalPermissionGroup: number = 0;
  dateStart: Date;
  dateEnd: Date;
  listFactory: KeyValuePair[] = [];
  listLevel: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  constructor(private service: S_5_2_24_ResignationAnnualLeaveWorktypeAnalysisReportService) {
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
    this.service.setSource(<ResignationAnnualLeaveWorktypeAnalysisReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListLevel();
    this.getListPermissionGroup();
  }

  setQueryDate() {
    if (this.param.dateStart)
      this.dateStart = new Date(this.param.dateStart)
    if (this.param.dateEnd)
      this.dateEnd = new Date(this.param.dateEnd);
  }

  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.search(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.totalRows = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }

  clear() {
    this.dateStart = null;
    this.dateEnd = null;
    this.totalRows = 0;
    this.param = <ResignationAnnualLeaveWorktypeAnalysisReportParam>{
      permission_Group: []
    }
  }

  download() {
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
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

  onSelectFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onChangePermission() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  onChangeDate(name: string) {
    this.param[name] = this[name] != null ? this[name].toStringDate() : null
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

  getListLevel() {
    this.service.getListLevel().subscribe({
      next: res => this.listLevel = res
    })
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    })
  }
  //#endregion
}
