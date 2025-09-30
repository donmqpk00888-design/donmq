import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { IncomeTaxFreeSetting_SubData, IncomeTaxFreeSetting_SubParam } from '@models/salary-maintenance/7_1_14_income-tax-free-setting';
import { S_7_1_14_IncomeTaxFreeSettingService } from '@services/salary-maintenance/s_7_1_14_income-tax-free-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  @ViewChild('paramForm') public paramForm: NgForm;
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));

  param: IncomeTaxFreeSetting_SubParam = <IncomeTaxFreeSetting_SubParam>{};

  data: IncomeTaxFreeSetting_SubData[] = []
  listFactory: KeyValuePair[] = [];
  listType: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  title: string = '';
  url: string = '';
  formType: string = '';
  isEdit: boolean = false;
  isDuplicated: boolean;
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  constructor(private service: S_7_1_14_IncomeTaxFreeSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
      this.getSource()
    });
  }
  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.param = structuredClone(source.selectedData)
        this.param.effective_Month_Str = this.functionUtility.getDateFormat(new Date(this.param.effective_Month))
        this.getDetail()
      } else this.back()
    }
    this.loadDropdownList()
  }

  getDetail() {
    this.spinnerService.show();
    this.service.getDetail(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res;
      }
    })
  }

  back = () => this.router.navigate([this.url]);

  async onPropertyChange() {
    if (this.paramForm.form.valid) {
      this.isDuplicated = await this.isDuplicatedData()
      if (this.isDuplicated) {
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput')
      }
      else {
        this.checkData()
      }
    }
  }

  isDuplicatedData(): Promise<boolean> {
    return new Promise((resolve) => {
      this.spinnerService.show()
      this.service.isDuplicatedData(this.param)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.data = res.isSuccess ? [] : res.data
            this.data.map((val: IncomeTaxFreeSetting_SubData) => val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time)))
            resolve(res.isSuccess)
          }
        });
    })
  }

  onKeyChange(item: IncomeTaxFreeSetting_SubData) {
    this.onDataChange(item);
    this.checkData()
  }

  checkData() {
    this.isDuplicated = false
    if (this.data.length > 0) {
      this.data.map(x => x.is_Duplicate = false)
      const lookup = this.data.reduce((a, e) => {
        a[e.type] = ++a[e.type] || 0;
        return a;
      }, {});
      const deplicateValues = this.data.filter(e => lookup[e.type])
      if (deplicateValues.length > 1) {
        this.isDuplicated = true
        deplicateValues.map(x => x.is_Duplicate = true)
        this.snotifyService.clear()
        this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput')
      }
    }
  }

  add() {
    const current = new Date();
    this.data.push(<IncomeTaxFreeSetting_SubData>{
      type: '',
      amount: '0',
      update_By: this.user.id,
      update_Time: current,
      update_Time_Str: this.functionUtility.getDateTimeFormat(current)
    })
  }


  remove(index: number) {
    this.data.splice(index, 1)
    this.checkData()
  }

  //#region Add & Edit
  save() {
    if (!this.isEdit) {
      this.create();
    } else {
      this.update();
    }
  }

  create() {
    this.spinnerService.show();
    this.service
      .create(this.param, this.data)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.back();
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(
                res.error ? this.translateService.instant(`SalaryMaintenance.IncomeTaxFreeSetting.${res.error}`)
                  : 'System.Message.CreateErrorMsg'
              ),
              this.translateService.instant('System.Caption.Error')
            );
          }
        }
      })
  }

  update() {
    this.spinnerService.show();
    this.service.update(this.param, this.data).subscribe({
      next: async (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.back();
          this.snotifyService.success(
            this.translateService.instant('System.Message.UpdateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(
            this.translateService.instant(
              res.error ? this.translateService.instant(`SalaryMaintenance.IncomeTaxFreeSetting.${res.error}`)
                : 'System.Message.UpdateErrorMsg'
            ),
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }
  //#endregion

  //#region On Change
  onEffectiveMonthChange() {
    this.param.effective_Month_Str = this.functionUtility.isValidDate(new Date(this.param.effective_Month)) ? this.functionUtility.getDateFormat(new Date(this.param.effective_Month)) : '';
    this.onPropertyChange();
  }

  onDataChange(item: IncomeTaxFreeSetting_SubData) {
    item.update_By = this.user.id;
    item.update_Time = new Date();
    item.update_Time_Str = this.functionUtility.getDateTimeFormat(
      item.update_Time
    );
  }
  //#endregion

  //#region Load & get list
  loadDropdownList() {
    this.getListFactory();
    this.getListType();
    this.getListSalaryType();
  }

  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getListType() {
    this.service.getListType().subscribe({
      next: res => {
        this.listType = res;
      }
    });
  }

  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }
  //#endregion
  deleteProperty = (item: IncomeTaxFreeSetting_SubData | IncomeTaxFreeSetting_SubParam, name: string) => delete item[name]
}
