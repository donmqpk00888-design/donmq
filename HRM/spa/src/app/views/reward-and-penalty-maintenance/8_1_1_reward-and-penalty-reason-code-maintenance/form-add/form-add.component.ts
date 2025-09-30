import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { RewardandPenaltyMaintenance, RewardandPenaltyMaintenance_Form, RewardandPenaltyMaintenanceParam } from '@models/reward-and-penalty-maintenance/8_1_1_reward-and-penalty-reason-code-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService } from '@services/reward-and-penalty-maintenance/s_8_1_1_reward-and-penalty-reason-code-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form-add',

  templateUrl: './form-add.component.html',
  styleUrls: ['./form-add.component.scss']
})
export class FormAddComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  param: RewardandPenaltyMaintenanceParam = <RewardandPenaltyMaintenanceParam>{};
  dataForm: RewardandPenaltyMaintenance_Form = <RewardandPenaltyMaintenance_Form>{};
  data: RewardandPenaltyMaintenance[] = [];

  title: string = '';
  url: string = '';
  formType: string = '';
  isDuplicated: boolean = false;
  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = []

  constructor(
    private service: S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList();
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
    });
    this.loadDropdownList();
  }

  loadDropdownList() {
    this.getListFactory();
  }
  add() {
    const current = new Date().toStringDateTime();
    this.data.push(<RewardandPenaltyMaintenance>{
      code: null,
      code_Name: null,
      factory: null,
      update_By: this.user.id,
      update_Time: current,
    })

  }
  remove(index: number) {
    this.data.splice(index, 1)
    this.checkDuplicate()
  }
  checkEmpty() {
    return (this.functionUtility.checkEmpty(this.param.factory))
  }
  checkDataEmpty() {
    return this.data.every(item =>
     item.code !== undefined && item.code !== null && item.code.toString() !== ''
      && item.code_Name !== undefined && item.code_Name !== null && item.code_Name.toString() !== ''
    );
  }
  save() {
    this.dataForm.param = this.param
    this.dataForm.data = this.data
    this.spinnerService.show();
    this.service.create(this.dataForm).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.CreateOKMsg'), this.translateService.instant('System.Caption.Success'));
          this.back();
        }
        else
          this.snotifyService.error(this.translateService.instant(res.error) ??this.translateService.instant('System.Message.CreateErrorMsg'), this.translateService.instant('System.Caption.Error'));
      }
    });
  }

  onChange(index: number) {
    this.checkDuplicate()
    this.onDataChange(this.data[index])

  }

  onDataChange(item: RewardandPenaltyMaintenance) {
    item.update_By = this.user.id;
    item.update_Time = new Date().toStringDateTime();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  onChangeFactory() {
    this.checkDuplicate();
  }

  async checkDuplicate() {
    this.isDuplicated = false;
    if (this.data.length > 0) {
      this.data.map(x => x.isDuplicate = false);

      const lookup = this.data.reduce((a, e) => {
        const key = `${e.code}`.trim().toUpperCase();
        a[key] = ++a[key] || 0;
        return a;
      }, {});

      const duplicateValues = this.data.filter(e => {
        const key = `${e.code}`.trim().toUpperCase();
        return lookup[key];
      });

      if (duplicateValues.length > 1) {
        this.isDuplicated = true;

        duplicateValues.map(x => x.isDuplicate = true);

      }
      await Promise.all(this.data.map(async (item) => {
        item.factory = this.param.factory
        if (item.factory && item.code && await this.isDuplicatedData(item)) this.isDuplicated = item.isDuplicate = true;
      }))
      if (this.isDuplicated) {
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'RewardandPenaltyMaintenance.RewardandPenaltyReasonCodeMaintenance.Duplicates')
      }
    }
  }
  isDuplicatedData(item: RewardandPenaltyMaintenance): Promise<boolean> {
    return new Promise((resolve) => {
      this.service.isDuplicatedData(item)
        .subscribe({
          next: (res) => {
            resolve(res.isSuccess)
          }
        });
    })
  }
  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.data[name]
}
