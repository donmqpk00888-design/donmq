import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { DirectWorkTypeAndSectionSettingParam } from '@models/organization-management/3_1_5_organization-management';
import { S_3_1_5_DirectWorkTypeAndSectionSettingService } from '@services/organization-management/s_3_1_5_direct-work-type-and-section-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-add',
  templateUrl: './add.component.html',
  styleUrls: ['./add.component.css'],
})
export class AddComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  iconButton = IconButton;
  isEdit: boolean = false;
  param: DirectWorkTypeAndSectionSettingParam = <DirectWorkTypeAndSectionSettingParam>{
    division: '',
    factory: '',
    effective_Date: '',
    work_Type_Code: '',
    section_Code: '',
    direct_Section: 'Y',
  };
  selectedWorkTypeCodeName: string;
  selectedSectionCodeName: string;
  effective_Date_value: Date = null;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkType: KeyValuePair[] = [];
  listSection: KeyValuePair[] = [];
  isAllow: boolean = false
  directSections: KeyValuePair[] = [
    { key: 'Y', value: 'OrganizationManagement.DirectWorkTypeAndSectionSetting.Direct' },
    { key: 'N', value: 'OrganizationManagement.DirectWorkTypeAndSectionSetting.Indirect' }
  ];
  constructor(
    private service: S_3_1_5_DirectWorkTypeAndSectionSettingService
  ) {
    super(); this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
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
  onEffectiveDateChange() {
    if (this.effective_Date_value)
      this.param.effective_Date = this.effective_Date_value.toDate().toStringYearMonth();
    if (this.effective_Date_value == undefined)
      this.deleteProperty('effective_Date')
    this.checkDuplicate()
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => {
        this.listDivision = res;
      },
    });
  }

  changeGetFactory() {
    this.deleteProperty('division')
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
        this.deleteProperty('factory')
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
  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSearch();
      if (source && source != null)
        this.param = { ...source.selectedData };
      else this.back();
    })
  }
  save() {
    this.spinnerService.show();
    this.service.create(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess) this.back();
      },

    })
  }

  back = () => this.router.navigate([this.url]);

  checkDuplicate() {
    !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.param.effective_Date) &&
      !this.functionUtility.checkEmpty(this.param.work_Type_Code)
      ? this.service.checkDuplicate(this.param).subscribe({
        next: result => {
          if (result.isSuccess) {
            !this.functionUtility.checkEmpty(this.param.section_Code)
              ? this.isAllow = true
              : this.isAllow = false
          }
          else {
            this.isAllow = false
            this.snotifyService.error(this.translateService.instant('OrganizationManagement.DirectWorkTypeAndSectionSetting.RepeatedData'),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      })
      : this.isAllow = false
  }

  deleteProperty = (name: string) => delete this.param[name]
}
