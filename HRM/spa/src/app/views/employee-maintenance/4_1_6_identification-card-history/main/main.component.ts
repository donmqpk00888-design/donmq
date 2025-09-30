import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import {
  HRMS_Emp_Identity_Card_HistoryDto,
  HRMS_Emp_Identity_Card_HistoryParam,
  HRMS_Emp_Identity_Card_History_Source,
  IdentificationCardHistory
} from '@models/employee-maintenance/4_1_6_identification-card-history';
import { S_4_1_6_IdentificationCardHistoryService } from '@services/employee-maintenance/s_4_1_6_identification-card-history.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  iconButton = IconButton;
  data: HRMS_Emp_Identity_Card_HistoryDto[] = []
  param: HRMS_Emp_Identity_Card_HistoryParam = <HRMS_Emp_Identity_Card_HistoryParam>{}
  listNationality: KeyValuePair[] = [];
  dataTypeahead: string[] = [];
  constructor(
    private service: S_4_1_6_IdentificationCardHistoryService,
    private modalService: ModalService,
  ) {
    super();
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.data = this.service.paramSearch().data;
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear(false)
      }
      this.getListNationality();
      this.getListTypeHeadIdentificationNumber(false);
    });
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListNationality();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) this.getData();
    })
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
   }

  ngOnDestroy(): void {
    this.service.setParamSearch(<IdentificationCardHistory>{ param: this.param, data: this.data });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true,'System.Message.QuerySuccess')
      },
    });
  }
  getListTypeHeadIdentificationNumber(onNationChange: boolean) {
    if (onNationChange)
      this.deleteProperty('identification_Number')
    this.service.getListTypeHeadIdentificationNumber(this.param.nationality).subscribe({
      next: (res) => {
        this.dataTypeahead = res;
      },
    });
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }

  detail(item: HRMS_Emp_Identity_Card_HistoryDto) {
    let source = <HRMS_Emp_Identity_Card_History_Source>{
      param: { ...this.param },
      itemDetail: { ...item },
    };
    this.service.basicCodeSource.set(source);
    this.service.setSource(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/detail`]);
  }

  clear(isClear: boolean) {
    this.deleteProperty('identification_Number')
    this.deleteProperty('nationality')
    isClear
      ? this.data = []
      : this.functionUtility.checkFunction('Search')
        ? this.getData()
        : this.data = [];
  }
  search = (isSearch: boolean) => this.getData(isSearch)

  add() {
    var item: HRMS_Emp_Identity_Card_HistoryDto = this.data[this.data.length - 1];
    this.modalService.open(item);
  }
  checkRequiredParams(): boolean {
    let result = !this.functionUtility.checkEmpty(this.param.nationality) && !this.functionUtility.checkEmpty(this.param.identification_Number)
    return result
  }
  deleteProperty = (name: string) => delete this.param[name];
}
