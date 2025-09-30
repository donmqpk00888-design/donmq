import { AfterViewChecked, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import {
  OvertimeModificationMaintenanceDto,
  OvertimeModificationMaintenanceParam,
  ParamMain5_20
} from '@models/attendance-maintenance/5_1_20_overtime-modification-maintenance';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { S_5_1_20_OvertimeModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_20_overtime-modification-maintenance.service';
import { ModalService } from '@services/modal.service';
import { FormGroup, NgForm } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('mainForm') public mainForm: NgForm;

  title: string = '';
  iconButton = IconButton;
  factorys: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];
  workShiftTypes: KeyValuePair[] = [];
  pagination: Pagination = <Pagination>{ pageNumber: 1, pageSize: 10 };
  data: OvertimeModificationMaintenanceDto[] = [];
  param: OvertimeModificationMaintenanceParam = <OvertimeModificationMaintenanceParam>{};
  bsConfig: Partial<BsDatepickerConfig> = { dateInputFormat: "YYYY/MM/DD" };
  allowGetData: boolean = false

  constructor(
    private service: S_5_1_20_OvertimeModificationMaintenanceService,
    private modalService: ModalService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.processData();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.getData(false);
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }

  getDataFromSource() {
    const source = this.service.signalDataMain();
    this.param = source.paramSearch;
    this.pagination = source.pagination;
    this.data = source.data;
    this.processData()
  }

  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search'))
        this.allowGetData = true
      else {
        this.clear()
        this.allowGetData = false
      }
    }
    this.loadDropdownList();
  }

  loadDropdownList() {
    this.getFactorys();
    this.getDepartments();
    this.getWorkShiftTypes();
  }

  ngAfterViewChecked() {
    if (this.allowGetData && this.mainForm) {
      const form: FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getData(false);
        this.allowGetData = false
      }
    }
  }

  ngOnDestroy(): void {
    this.service.signalDataMain.set(<ParamMain5_20>{
      data: this.data,
      pagination: this.pagination,
      paramSearch: this.param
    })
  }

  getWorkShiftTypes() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => this.workShiftTypes = res
    })
  }

  getFactorys() {
    this.service.getListFactory().subscribe({
      next: res => this.factorys = res
    })
  }

  onChangeFactory() {
    this.deleteProperty('department')
    this.departments = [];
    this.getDepartments();
  }

  getDepartments() {
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.service.getListDepartment(this.param.factory).subscribe({
        next: res => {
          this.departments = res;
        }
      })
    }
  }

  search() {
    this.pagination.pageNumber === 1 ? this.getData() : this.pagination.pageNumber = 1;
  }

  getData(isSearch: boolean = true) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(isSearch, 'BasicMaintenance.2_6_GradeMaintenance.QueryOKMsg');
      }
    });
  }

  clear() {
    this.param = <OvertimeModificationMaintenanceParam>{};
    this.data = [];
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: OvertimeModificationMaintenanceDto) {
    this.modalService.open(item);
  }

  onDelete(item: OvertimeModificationMaintenanceDto) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: res => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg');
          if (res.isSuccess) this.getData(false);
        }
      });
    });
  }
  onDateChange(name: string) {
    this.param[name] = this.functionUtility.isValidDate(new Date(this.param[`${name}_Date`]))
      ? this.functionUtility.getDateFormat(new Date(this.param[`${name}_Date`]))
      : '';
  }
  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData(false);
  }
  deleteProperty = (name: string) => delete this.param[name]
}
