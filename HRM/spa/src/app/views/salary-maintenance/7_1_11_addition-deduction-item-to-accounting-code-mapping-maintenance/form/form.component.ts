import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService } from '@services/salary-maintenance/s_7_1_11_addition-deduction-item-to-accounting-code-mapping-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { AdditionDeductionItemToAccountingCodeMappingMaintenanceDto } from '@models/salary-maintenance/7_1_11_addition-deduction-item-to-accouting-code-mapping-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss',
})
export class FormComponent extends InjectBase implements OnInit {
  user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
  data: AdditionDeductionItemToAccountingCodeMappingMaintenanceDto = <AdditionDeductionItemToAccountingCodeMappingMaintenanceDto>{};
  listFactory: KeyValuePair[] = [];
  listAdditionsAndDeductionsItem: KeyValuePair[] = [];
  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  isEdit: boolean = false;
  iconButton = IconButton;
  classButton = ClassButton;

  constructor(
    private service: S_7_1_11_AdditionDeductionItemToAccountingCodeMappingMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.formType = res['title'];
      this.getSource()
    })
  }
  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      } else this.back()
    }
    this.loadDropdownList()
  }

  back = () => this.router.navigate([this.tempUrl]);

  onDataChange() {
    this.data.update_By = this.user.id;
    this.data.update_Time = new Date();
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(this.data.update_Time);
  }

  //#region Add & Edit
  save(isNext: boolean) {
    if (!this.isEdit) {
      this.add(isNext);
    } else {
      this.edit();
    }
  }

  add(isNext: boolean) {
    this.spinnerService.show();
    this.service
      .create(this.data)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            isNext ? this.data = <AdditionDeductionItemToAccountingCodeMappingMaintenanceDto>{ factory: this.data.factory } : this.back();
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(
                res.error ?? 'System.Message.CreateErrorMsg'
              ),
              this.translateService.instant('System.Caption.Error')
            );
          }
        }
      })
  }

  edit() {
    this.spinnerService.show();
    this.service.update(this.data).subscribe({
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
              res.error ?? 'System.Message.UpdateErrorMsg'
            ),
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }
  //#endregion

  //#region Get and load list
  loadDropdownList() {
    this.getListFactory();
    this.getListAdditionsAndDeductionsItem();
  }

  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListAdditionsAndDeductionsItem() {
    this.service.getListAdditionsAndDeductionsItem().subscribe({
      next: (res) => {
        this.listAdditionsAndDeductionsItem = res;
      },
    });
  }
  //#region
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
