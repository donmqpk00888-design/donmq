import { AfterViewChecked, Component, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { FemaleEmpMenstrualMain, FemaleEmpMenstrualMemory, FemaleEmpMenstrualParam } from '@models/attendance-maintenance/5_1_26_female-employee-menstrual-leave-hours-maintenance';
import { S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService } from '@services/attendance-maintenance/s_5_1_26_female-employee-menstrual-leave-hours-maintenance.service';
import { ModalService } from '@services/modal.service';
import { NgForm, FormGroup } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('mainForm') public mainForm: NgForm;
  allowGetData: boolean = false

  title: string = '';
  programCode: string = '';
  params: FemaleEmpMenstrualParam = <FemaleEmpMenstrualParam>{};
  pagination: Pagination = <Pagination>{};
  datas: FemaleEmpMenstrualMain[] = [];

  iconButton = IconButton;
  classButton = ClassButton;

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService,
    private modalService: ModalService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      if (this.params.factory) {
        this.getListDepartment();
        if (this.functionUtility.checkFunction('Search'))
          this.getDataPagination(false);
      }
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.getDataPagination(false)
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
  }

  getDataFromSource() {
    const source = this._service.paramSearch();
    this.params = source.params;
    this.pagination = source.pagination;
    this.datas = source.datas;
    this.processData()
  }

  processData() {
    if (this.datas.length > 0) {
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
    this.getListFactory();
    this.getListDepartment();
  }

  ngAfterViewChecked() {
    if (this.allowGetData && this.mainForm) {
      const form: FormGroup = this.mainForm.form
      const values = Object.values(form.value)
      const isLoaded = !values.every(x => x == undefined)
      if (isLoaded) {
        if (form.valid)
          this.getDataPagination(false);
        this.allowGetData = false
      }
    }
  }

  ngOnDestroy(): void {
    this._service.setParamSearch(<FemaleEmpMenstrualMemory>{
      datas: this.datas,
      pagination: this.pagination,
      params: this.params
    })
  }

  getListFactory() {
    this._service.getListFactoryByUser()
      .subscribe({
        next: (res) => {
          this.factories = res;
        }
      });
  }

  changeFactory() {
    this.departments = [];
    this.deleteProperty('department');
    this.getListDepartment();
  }

  getListDepartment() {
    if (this.params.factory)
      this._service.getListDepartment(this.params.factory)
        .subscribe({
          next: (res) => this.departments = res
        });
  }

  //#region query data
  getDataPagination(isSearch: boolean) {
    this.spinnerService.show();
    this._service.getDataPagination(this.pagination, this.params)
      .subscribe({
        next: (res) => {
          this.datas = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.SearchOKMsg')

          this.spinnerService.hide();
        }
      });
  }

  search = (isSearch?: boolean) => {
    this.pagination.pageNumber == 1 ? this.getDataPagination(isSearch ?? true) : this.pagination.pageNumber = 1;
  };

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getDataPagination(false);
  }
  //#endregion
  onDateChange(name: string) {
    this.params[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.params[name]))
      ? this.functionUtility.getDateFormat(new Date(this.params[name]))
      : '';
  }
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  edit(item: FemaleEmpMenstrualMain) {
    this.modalService.open(item);
  }

  delete(item: FemaleEmpMenstrualMain) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this._service.delete(item).subscribe({
        next: res => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) this.getDataPagination(false);
        }
      });
    });
  }
  download() {
    this.spinnerService.show();
    this._service
      .downloadExcel(this.params)
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
      });
  }

  clear() {
    this.params = <FemaleEmpMenstrualParam>{}
    this.departments = [];
    this.datas = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0
    };
  }
  deleteProperty = (name: string) => delete this.params[name]
}
