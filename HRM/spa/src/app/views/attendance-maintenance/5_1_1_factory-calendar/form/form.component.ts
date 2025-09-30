import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { FactoryCalendar_Table } from '@models/attendance-maintenance/5_1_1_factory-calendar';
import { UserForLogged } from '@models/auth/auth';
import { S_5_1_1_FactoryCalendar } from '@services/attendance-maintenance/s_5_1_1_factory-calendar.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = ''
  action: string = '';
  url: string = '';
  classNone: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  iconButton = IconButton;
  classButton = ClassButton;

  data: FactoryCalendar_Table = <FactoryCalendar_Table>{}

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  typeCodeList: KeyValuePair[] = [];

  constructor(
    private service: S_5_1_1_FactoryCalendar
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM/DD',
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.action = role.title;
        this.filterList(role.dataResolved)
      })
    this.service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.action == 'Edit') {
        if (res == null)
          this.back()
        else
          this.data = res

        this.classNone = 'custom-none';
      }
      else {
        if (res != null)
          this.data = res
      }
    })
  }
  retryGetDropDownList() {
    this.service.getDropDownList(this.data.division)
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.divisionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.typeCodeList = structuredClone(keys.filter((x: { key: string; }) => x.key == "TY")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  deleteProperty(name: string) {
    delete this.data[name]
    this.editData()
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
  }
  save(isBack: boolean) {
    this.spinnerService.show();
    if (this.action == 'Add') {
      this.service
        .postData(this.data)
        .subscribe({
          next: (res) => {
          this.spinnerService.hide();
            if (res.isSuccess) {
              isBack ? this.back() : this.data = <FactoryCalendar_Table>{}
              this.snotifyService.success(
                this.translateService.instant('System.Message.UpdateOKMsg'),
                this.translateService.instant('System.Caption.Success')
              );
            } else {
              this.snotifyService.error(
                this.translateService.instant(`AttendanceMaintenance.FactoryCalendar.${res.error}`),
                this.translateService.instant('System.Caption.Error'));
            }
          }
        })
    }
    else {
      this.service
        .putData(this.data)
        .subscribe({
          next: (res) => {
          this.spinnerService.hide();
            if (res.isSuccess) {
              this.back()
              this.snotifyService.success(
                this.translateService.instant('System.Message.UpdateOKMsg'),
                this.translateService.instant('System.Caption.Success')
              );
            } else {
              this.snotifyService.error(
                this.translateService.instant(`AttendanceMaintenance.FactoryCalendar.${res.error}`),
                this.translateService.instant('System.Caption.Error')
              );
            }
          }
        })
    }
  }
  editData() {
    this.data.update_By = this.user.id
    this.data.update_Time = new Date
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(this.data.update_Time))
  }
  back = () => this.router.navigate([this.url]);

  onDateChange = (name: string) => this.data[`${name}_Str`] = this.data[name] ? this.functionUtility.getDateFormat(new Date(this.data[name])) : '';
}

