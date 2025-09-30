import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { SalaryApprovalForm_Param, SalaryApprovalForm_Source } from '@models/salary-report/7_2_1_SalaryApprovalForm';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_7_2_1_SalaryApprovalFormService } from '@services/salary-report/s_7_2_1_salary-approval-form.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy{
  title: string = '';
  programCode: string = '';
  totalRows: number = 0;
  totalPermissionGroup: number = 0;
  iconButton = IconButton;
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
    minMode: 'month'
  };
  multidate: Date;
  listPosition: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];


  kind: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'SalaryReport.SalaryApprovalForm.Onjob',
    },
    {
      key: "R",
      value: 'SalaryReport.SalaryApprovalForm.Resigned'
    },
    {
      key: "A",
      value: 'SalaryReport.SalaryApprovalForm.All'
    },
  ];

  param: SalaryApprovalForm_Param = <SalaryApprovalForm_Param>{
    kind: 'O',
    permission_Group: [],
  }

  constructor(private service: S_7_2_1_SalaryApprovalFormService) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }
  ngOnDestroy(): void {
    this.service.setSource(<SalaryApprovalForm_Source>{
      param: this.param,
      totalRows: this.totalRows
    })
  }
  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
  }

  onSelectFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
    this.getListDepartment();
  }
  onChangePermission() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }
  onReportformatChange(){
  }
  private loadDropDownList() {
    this.getListFactory();
    this.getListPermissionGroup();
    this.getListDepartment();
    this.getPositionTitles();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
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
  getListDepartment() {
    this.service.getListDepartment(this.param.factory).subscribe({
      next: res => {
        this.listDepartment = res
      }
    })
  }
  getPositionTitles() {
    this.service.getPositionTitles().subscribe({
      next: res => this.listPosition = res
    })
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
  download() {
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        if (res.isSuccess) {
          this.functionUtility.exportExcel(res.data.fileData, fileName, 'pdf');
          this.totalRows = res.data.totalRows;
        } else {
          this.totalRows = 0
          this.snotifyService.warning(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Warning'));
        }
      }
    })
  }
  clear() {
    this.multidate = null;
    this.totalRows = 0;
    this.param = <SalaryApprovalForm_Param>{
      kind : 'O',
      permission_Group: []
    }
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
