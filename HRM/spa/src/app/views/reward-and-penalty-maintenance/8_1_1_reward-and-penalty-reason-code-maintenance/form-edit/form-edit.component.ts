import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { RewardandPenaltyMaintenance, RewardandPenaltyMaintenance_Form, RewardandPenaltyMaintenanceParam } from '@models/reward-and-penalty-maintenance/8_1_1_reward-and-penalty-reason-code-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService } from '@services/reward-and-penalty-maintenance/s_8_1_1_reward-and-penalty-reason-code-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form-edit',
  templateUrl: './form-edit.component.html',
  styleUrls: ['./form-edit.component.scss']
})
export class FormEditComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  param: RewardandPenaltyMaintenanceParam = <RewardandPenaltyMaintenanceParam>{};
  dataEdit: RewardandPenaltyMaintenance = <RewardandPenaltyMaintenance>{};
  data: RewardandPenaltyMaintenance[] = []

  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  title: string = '';
  url: string = '';
  formType: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = []
  constructor(
    private service: S_8_1_1_RewardAndPenaltyReasonCodeMaintenanceService
  ) {
    super();
    this.getDataFromSource()
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
  getDataFromSource() {
    let source = this.service.paramSource();
    if (source.source && Object.keys(source.source).length > 0) {
      this.dataEdit = structuredClone(source.source);
    }
    else this.back();
  }

  loadDropdownList() {
    this.getListFactory();
  }

  save() {
    this.spinnerService.show();
    this.service.update(this.dataEdit).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          this.snotifyService.success(this.translateService.instant('System.Message.UpdateOKMsg'), this.translateService.instant('System.Caption.Success'));
          this.back();
        }
        else
          this.snotifyService.error(this.translateService.instant(res.error) ??this.translateService.instant('System.Message.UpdateErrorMsg'), this.translateService.instant('System.Caption.Error'));
      }
    });
  }

  onValueChange() {
    this.dataEdit.update_By = this.updateBy
    this.dataEdit.update_Time = new Date().toStringDateTime()
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.data[name]
}
