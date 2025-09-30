import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { SessionStorageConstants } from '@constants/local-storage.constants';
import { FunctionInfomation } from '@models/common';
import { HRMS_Emp_Blacklist_MainMemory, HRMS_Emp_BlacklistDto, HRMS_Emp_BlacklistParam } from '@models/employee-maintenance/4_1_13_exit-employees-blacklist';
import { S_4_1_13_ExitEmployeesBlacklistService } from '@services/employee-maintenance/s_4_1_13_exit-employees-blacklist.service';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };

  colSpanOperation: number;
  listNationality: KeyValuePair[] = [];
  identificationNumber: KeyValuePair[] = [];
  nameFunction: string[] = [];
  iconButton = IconButton;
  classButton = ClassButton;
  data: HRMS_Emp_BlacklistDto[] = [];
  param: HRMS_Emp_BlacklistParam = <HRMS_Emp_BlacklistParam>{}

  constructor(
    private service: S_4_1_13_ExitEmployeesBlacklistService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListNationality();
      this.getListIdentificationNumber();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave ) this.getData();
    })
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.data.length > 0) {
        if (!this.functionUtility.checkFunction('Search'))
          this.clear();
        else
          this.getData();
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListNationality();
    this.getListIdentificationNumber();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<HRMS_Emp_Blacklist_MainMemory>{ param: this.param, pagination: this.pagination, data: this.data });
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      }
    });
  }

  getListIdentificationNumber() {
    this.service.getIdentificationNumber().subscribe({
      next: (res) => {
        this.identificationNumber = res;
      }
    })
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true,'System.Message.QuerySuccess')
      }
    });
  }

  setColSpanOperation() {
    let functionCodes = ['Query', 'Edit', 'Delete'];
    const functions: FunctionInfomation[] = JSON.parse(
      sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)
    );
    this.colSpanOperation = functions?.filter(val => functionCodes.includes(val.function_Code)).length;
  }

  onEdit(item: HRMS_Emp_BlacklistDto) {
    this.modalService.open(item);
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  clear() {
    this.param = <HRMS_Emp_BlacklistParam>{}
    this.data = []
    this.pagination.totalCount = 0;
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
