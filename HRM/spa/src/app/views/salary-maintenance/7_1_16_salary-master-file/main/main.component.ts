import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { SalaryMasterFile_Main, SalaryMasterFile_Main_Memory, SalaryMasterFile_Param } from '@models/salary-maintenance/7_1_16_salary-master-file';
import { S_7_1_16_SalaryMasterFileService } from '@services/salary-maintenance/s_7_1_16_salary-master-file.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPositionTitle: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryMaintenance.SalaryMasterFile.Onjob' },
    { key: 'N', value: 'SalaryMaintenance.SalaryMasterFile.Resigned' },
    { key: 'U', value: 'SalaryMaintenance.SalaryMasterFile.Unpaid' },
  ];
  param: SalaryMasterFile_Param = <SalaryMasterFile_Param>{};
  data: SalaryMasterFile_Main[] = [];
  selectedData: SalaryMasterFile_Main;
  pagination: Pagination = <Pagination>{}
  iconButton = IconButton;
  classButton = ClassButton;
  constructor(private service: S_7_1_16_SalaryMasterFileService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
        this.loadDropdownList();
        this.processData()
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.getSource();
  }

  ngOnDestroy(): void {
    this.service.signalDataMain.set(<SalaryMasterFile_Main_Memory>{
      data: this.data,
      pagination: this.pagination,
      paramSearch: this.param,
      selectedData: this.selectedData
    });
  }

  getSource() {
    this.param = this.service.signalDataMain().paramSearch;
    this.pagination = this.service.signalDataMain().pagination;
    this.data = this.service.signalDataMain().data;
    this.loadDropdownList()
    this.processData()
  }

  processData() {
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }

  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory);
  }

  onChangeFactory() {
    this.deleteProperty("department");
    this.listDepartment = [];
    if (this.param.factory) {
      this.getDepartments();
    }
  }


  deleteProperty(name: string) {
    delete this.param[name]
  }

  getPermissionGroups() {
    this.service.getListPermissionGroup().subscribe({
      next: result => {
        this.permissionGroups = result;
        this.selectAllForDropdownItems(this.permissionGroups)
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

  query(item: SalaryMasterFile_Main) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }


  getData(isFlag?: boolean) {
    this.spinnerService.show();
    this.service.getDataPagination(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isFlag)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  search(isFlag: boolean) {
    this.pagination.pageNumber == 1 ? this.getData(isFlag) : this.pagination.pageNumber = 1;
  }

  loadDropdownList() {
    this.getFactorys();
    this.getPositionTitles();
    this.getSalaryTypes();
    this.getPermissionGroups();
    if (this.param.factory) {
      this.getDepartments();
    }
  }

  getFactorys() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getDepartments() {
    this.service.getDepartments(this.param.factory).subscribe({
      next: res => {
        this.listDepartment = res;
      }
    });
  }

  getPositionTitles() {
    this.service.getPositionTitles().subscribe({
      next: res => {
        this.listPositionTitle = res;
      }
    });
  }

  getSalaryTypes() {
    this.service.getSalaryTypes().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }

  preventPaste(event: ClipboardEvent) {
    event.preventDefault();
    return false;
  }

  validateDecimal(event: KeyboardEvent, maxValue: number): boolean {
    const inputChar = event.key;

    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.includes(inputChar))
      return true;

    if (!/^\d$/.test(inputChar) && inputChar !== '.') {
      event.preventDefault();
      return false;
    }

    const input = event.target as HTMLInputElement;
    const currentValue = input.value;
    const newValue = currentValue.substring(0, input.selectionStart!) + inputChar + currentValue.substring(input.selectionEnd!);

    const parts = newValue.split('.');
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (decimalPartLength > 1) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^[0-9]{0,3}(\.[0-9]{0,1})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    const newValueNum = parseFloat(newValue);
    if (newValueNum > maxValue) {
      event.preventDefault();
      return false;
    }

    if (newValueNum === maxValue && inputChar === '.') {
      event.preventDefault();
      return false;
    }

    return true;
  }

  clear() {
    this.param = <SalaryMasterFile_Param>{};
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }

  clearPermissionGroup() {
    this.deleteProperty('permission_Group');
  }

  download() {
    this.spinnerService.show();
    this.service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
        ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
      },
    });
  }
}
