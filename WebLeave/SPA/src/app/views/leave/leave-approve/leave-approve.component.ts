import { Component, OnInit, TemplateRef } from '@angular/core';
import * as moment from 'moment';
import { LocalStorageConstants } from '@constants/local-storage.enum';
import { SearchApproveParams } from '@params/leave/leave-approve-params';
import { LeaveDataApprove } from '@models/leave/leave-data-approve';
import { LeaveApproveService } from '@services/leave/leave-approve.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { LeaveDataApproveViewOnTime } from '@models/leave/leave-data-approve-view-on-time';
import { LangConstants } from '@constants/lang.constants';
import { DestroyService } from '@services/destroy.service';
import { takeUntil } from 'rxjs';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { CommonConstants } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';

@Component({
  selector: 'app-leave-approve',
  templateUrl: './leave-approve.component.html',
  styleUrls: ['./leave-approve.component.scss'],
  providers: [DestroyService]
})
export class LeaveApproveComponent extends InjectBase implements OnInit {

  searchApproveParams: SearchApproveParams = <SearchApproveParams>{
    isSearch: false
  };
  leaveDataApprove: LeaveDataApprove[] = [];
  leaveDataApproveUpdate: LeaveDataApprove[] = [];
  category: KeyValuePair[] = [];
  dateFrom = new Date();
  dateTo = new Date();
  dateComment: Date = null;
  languageCurrent: string = localStorage.getItem(LocalStorageConstants.LANG) ?? LangConstants.VN;
  pagination: Pagination = {
    pageNumber: 1,
    pageSize: 20,
  } as Pagination;
  paginationOnView: Pagination = {
    pageNumber: 1,
    pageSize: 20,
  } as Pagination;
  isAllItemChecked: boolean = false;
  modalRef?: BsModalRef;
  commentLeaveData: string = '';
  userComment: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).fullName;
  config: ModalOptions = { class: 'modal-xl' };
  leaveDataApproveViewOnTime: LeaveDataApproveViewOnTime = {} as LeaveDataApproveViewOnTime;
  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    containerClass: 'theme-dark-blue',
    dateInputFormat: 'DD/MM/YYYY'
  }
  commonConstants = CommonConstants;
  langConstants = LangConstants;
  constructor(
    private leaveaApproveService: LeaveApproveService,
    private modalService: BsModalService,
  ) {
    super();
  }

  ngOnInit(): void {

    this.leaveaApproveService.dataSource.subscribe({
      next: result => {
        if (result) {
          this.searchApproveParams = result;
        }
        else {
          this.searchApproveParams.userCurrent = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).userID;
          this.searchApproveParams.categoryId = 0;
        }
      }
    })

    this.openCategory();
    this.getLeaveData();
    //check lang change when click button
    this.translateService.onLangChange.pipe(takeUntil(this.destroyService.destroys$)).subscribe(res => {
      this.languageCurrent = res.lang;
      this.getCategory(this.languageCurrent);
    });
  }
  exportExcel() {
    this.spinnerService.show();
    this.initLang();
    this.leaveaApproveService.exportExcel(this.pagination, this.searchApproveParams).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        result.isSuccess ? this.functionUtility.exportExcel(result.data, 'Leave_Approve')
          : this.snotifyService.error(this.translateService.instant('System.Message.SystemError'),
            this.translateService.instant('System.Caption.Error'));
      },
      error: () => {
        this.snotifyService.error(this.translateService.instant('System.Message.SystemError'),
          this.translateService.instant('System.Caption.Error'));
        this.spinnerService.hide();
      }
    })
  }
  private initLang(): void {
    this.searchApproveParams.label_Employee = this.translateService.instant('Leave.LeaveApprove.EmpNumberExcel');
    this.searchApproveParams.label_Fullname = this.translateService.instant('Leave.LeaveApprove.EmpNameExcel');
  }

  getCategory(lang: string) {
    this.searchApproveParams.lang = lang;
    this.leaveaApproveService.getCategory(this.searchApproveParams.lang)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe({
        next: (data) => {
          this.category = data;
          this.category.unshift({ key: 0, value: this.translateService.instant('Leave.HistoryLeave.AllCategory') });
        }
      });
  }

  btnBack() {
    this.leaveaApproveService.dataSource.next(this.searchApproveParams)
    this.router.navigate(['/leave/']);
  }
  openCategory() {
    this.getCategory(this.languageCurrent);
  }
  clearCategory() {
    this.searchApproveParams.categoryId = 0;
  }
  pageChanged(event: any): void {
    this.pagination.pageNumber = event.page;
    this.isAllItemChecked = false;
    this.getLeaveData();
  }
  getLeaveData() {
    this.spinnerService.show();
    this.searchApproveParams.onView = false;
    this.leaveaApproveService.getLeaveData(this.searchApproveParams, this.pagination).subscribe({
      next: (data) => {
        this.leaveDataApprove = data.result;
        this.pagination = data.pagination;
        this.spinnerService.hide();
        this.leaveDataApprove.forEach(element => {
          if (element.approved === 1) {
            element.status = this.translateService.instant('Leave.LeaveApprove.Pending');
          }
          if (element.cateID === 4 || element.cateID === 13 || element.cateID === 5) {
            element.isMarker = true;
          }
          if (element.status_Lock && (element.mailContent_Lock === null || element.mailContent_Lock === ''))
            element.mailContent_Lock = this.translateService.instant('Leave.LeaveApprove.isMarkerMessenger');
        });
      },
      error: () => {
        this.spinnerService.hide();
        this.snotifyService.error(this.translateService.instant('System.Message.SystemError'),
          this.translateService.instant('System.Caption.Error'));
      }
    });
  }
  btnSearch() {
    if (this.dateFrom > this.dateTo)
      return this.snotifyService.warning(this.translateService.instant('System.Message.CompareDate'),
        this.translateService.instant('System.Caption.Warning'));
    let startTime = this.dateFrom ? this.functionUtility.getDateFormat(this.dateFrom) : this.functionUtility.getDateFormat(new Date());
    let endTime = this.dateTo ? this.functionUtility.getDateFormat(this.dateTo) : this.functionUtility.getDateFormat(new Date());
    this.searchApproveParams.startTime = startTime;
    this.searchApproveParams.endTime = endTime;
    this.searchApproveParams.isSearch = true;
    this.pagination.pageNumber = 1;

    this.getLeaveData();
  }
  handleCheckItem(item: LeaveDataApprove) {
    item.accept = !item.accept;
    this.checkIfAllItemChecked();
  }
  checkIfAllItemChecked() {
    this.isAllItemChecked = !this.leaveDataApprove.some(x => !x.accept && !x.status_Lock);
  }
  handleAllItemChanges() {
    this.isAllItemChecked = !this.isAllItemChecked;
    if (this.isAllItemChecked) {
      this.leaveDataApprove.forEach(i => {
        if (!i.status_Lock && !i.lock_Leave)
          i.accept = true;
      });
    } else {
      this.leaveDataApprove.forEach(i => {
        i.accept = false;
      });
    }
  }

  openModal(template: TemplateRef<any>) {
    this.leaveDataApproveUpdate = this.leaveDataApprove.filter(x => x.accept === true);
    if (this.leaveDataApproveUpdate.length >= 1) {
      this.modalRef = this.modalService.show(template);
    } else {
      return this.snotifyService.warning(this.translateService.instant('System.Message.SelectRecord'),
        this.translateService.instant('System.Caption.Warning'));
    }
  }
  closeModal() {
    this.commentLeaveData = '';
    this.leaveDataApproveUpdate = [];
    this.modalRef?.hide()
  }

  getServerTimeAsPromise(): Promise<any> {
    return new Promise((resolve, reject) => {
      this.commonService.getServerTime().subscribe({
        next: resolve,
        error: reject,
      });
    });
  }
  async updateLeaveData(check: boolean) {
    this.dateComment = await this.getServerTimeAsPromise();
    if (check) {
      this.leaveDataApproveUpdate.forEach(item => {
        item.approvedBy = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).userID;
        item.comment = `${item.comment}-[${moment(this.dateComment, 'YYYY/MM/DD').format('DD/MM/YYYY HH:mm:ss')}] duocduyetboi ${this.userComment} (${this.commentLeaveData})`;
      });
    } else {
      this.leaveDataApproveUpdate.forEach(item => {
        item.approvedBy = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).userID;
        item.comment = `${item.comment}-[${moment(this.dateComment, 'YYYY/MM/DD').format('DD/MM/YYYY HH:mm:ss')}] tuchoiboi ${this.userComment} (${this.commentLeaveData})`;
        item.commentLeave = this.commentLeaveData;
      });
    }
    this.spinnerService.show();
    this.leaveaApproveService.updateLeaveData(this.leaveDataApproveUpdate, check)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe({
        next: (result) => {
          this.spinnerService.hide();
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.closeModal();
            this.pagination.pageNumber = 1;
            this.isAllItemChecked = false;
            this.getLeaveData();
          } else {
            this.snotifyService.error(this.translateService.instant('System.Message.UpdateErrorMsg'),
              this.translateService.instant('System.Caption.Error'));
          }
        },
        error: () => {
          this.spinnerService.hide();
          this.snotifyService.error(this.translateService.instant('System.Message.SystemError'),
            this.translateService.instant('System.Caption.Error'));
        }
      });
  }
  closeModalView() {
    this.modalRef?.hide();
    this.leaveDataApproveViewOnTime = {} as LeaveDataApproveViewOnTime;
  }
  openModalView(template: TemplateRef<any>, item: LeaveDataApprove) {
    this.modalRef = this.modalService.show(template, this.config);
    this.leaveDataApproveViewOnTime.userViewOnTime = `${item.empName} - ${item.empNumber}`;
    if (this.languageCurrent === LangConstants.ZH_TW || this.languageCurrent === 'zh')
      this.leaveDataApproveViewOnTime.categoryViewOnTime = item.categoryLangZH;
    else if (this.languageCurrent === LangConstants.EN)
      this.leaveDataApproveViewOnTime.categoryViewOnTime = item.categoryLangEN;
    else
      this.leaveDataApproveViewOnTime.categoryViewOnTime = item.categoryLangVN;
    this.leaveDataApproveViewOnTime.leaveTimeViewOnTime = `${item.leaveDayString}d - ${item.leaveHourString}h`;
    this.leaveDataApproveViewOnTime.timeViewOnTime = `${item.timeStart} ${item.dateStart} - ${item.timeEnd} ${item.dateEnd}`;
    this.searchApproveParams.leaveID = item.leaveID;
    this.leaveDataApproveViewOnTime.leaveIDViewOnTime = item.leaveID;
    this.getLeaveDataViewOnTime();
  }
  getLeaveDataViewOnTime() {
    this.spinnerService.show();
    this.searchApproveParams.onView = true;
    this.leaveaApproveService.getLeaveData(this.searchApproveParams, this.paginationOnView)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe({
        next: (data) => {
          this.spinnerService.hide();
          let index;
          data.result.forEach((element, i) => {
            if (element.approved === 1) {
              element.status = this.translateService.instant(this.commonConstants.COMMON_STATUS1); //pending
            } else if (element.approved === 2) {
              element.status = this.translateService.instant(this.commonConstants.COMMON_STATUS2); //approved
            } else if (element.approved === 3) {
              element.status = this.translateService.instant(this.commonConstants.COMMON_STATUS3); //refuse
            } else {
              element.status = this.translateService.instant(this.commonConstants.COMMON_STATUS4); //complete
            }
            if (element.leaveID === this.leaveDataApproveViewOnTime.leaveIDViewOnTime)
              index = i;
          });

          data.result.splice(index, 1);
          this.leaveDataApproveViewOnTime.listLeaveDataApprove = data.result;
        },
        error: () => {
          this.spinnerService.hide();
          this.leaveDataApproveViewOnTime.listLeaveDataApprove = [];
          this.snotifyService.error(this.translateService.instant('System.Message.SystemError'),
            this.translateService.instant('System.Caption.Error'));
        }
      });
  }
  toDetail(leaveID: number) {
    this.router.navigate([`/leave/detail/${leaveID}`]).then(
      () => {
        this.leaveaApproveService.dataSource.next(this.searchApproveParams)
      },
      (error) => { }
    );
  }
}
