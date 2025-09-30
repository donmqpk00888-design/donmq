import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { DirectWorkTypeAndSectionSettingParam } from '@models/organization-management/3_1_5_organization-management';
import { S_3_1_5_DirectWorkTypeAndSectionSettingService } from '@services/organization-management/s_3_1_5_direct-work-type-and-section-setting.service';
import { FunctionUtility } from '@utilities/function-utility';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html',
  styleUrls: ['./edit.component.css']
})
export class EditComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  iconButton = IconButton;
  isEdit: boolean = false;
  effective_Date_value: Date = null;
  param: DirectWorkTypeAndSectionSettingParam = <DirectWorkTypeAndSectionSettingParam>{};
  inputDate: string;
  selectedWorkTypeCodeName: string;
  selectedSectionCodeName: string;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listSection: KeyValuePair[] = [];
  directSections: KeyValuePair[] = [
    { key: 'Y', value: 'OrganizationManagement.DirectWorkTypeAndSectionSetting.Direct' },
    { key: 'N', value: 'OrganizationManagement.DirectWorkTypeAndSectionSetting.Indirect' }
  ];
  constructor(
    private service: S_3_1_5_DirectWorkTypeAndSectionSettingService,
    private _function: FunctionUtility,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListSection();
      this.getListDivision();
      this.getListSection();
      this.getListWorkType();
      this.changeGetFactory();
    });
    this.getDataFromSource();
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListDivision();
    this.getListSection();
    this.getListWorkType();
    this.changeGetFactory()
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSearch();
      this.param = { ...source.selectedData };
      if (Object.keys(this.param).length == 0)
        this.back()
      else {
        this.inputDate = this._function.getDateTimeFormat(this.param.update_Time.toDate());
        if (!this.functionUtility.checkEmpty(this.param.effective_Date))
          this.effective_Date_value = new Date(this.param.effective_Date);
      }
    })
  }
  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => this.listDivision = res,
    });
  }

  changeGetFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListWorkType() {
    this.service.getListWorkType().subscribe({
      next: (res) => this.listWorkType = res,
    });
  }

  getListSection() {
    this.service.getListSection().subscribe({
      next: (res) => this.listSection = res,
    });
  }
  save() {
    this.spinnerService.show();
    this.service.update(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess) this.back();
      },

    })
  }

  back = () => this.router.navigate([this.url]);
}
