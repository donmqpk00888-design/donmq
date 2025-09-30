import { AfterViewInit, Component, input, ViewChild } from '@angular/core';
import { Language, LanguageParams, languageSource } from '@models/organization-management/3_1_1-department-maintenance';
import { ModalService } from '@services/modal.service';
import { S_3_1_1_DepartmentMaintenanceService } from '@services/organization-management/s_3_1_1_department-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './form-language.component.html',
  styleUrls: ['./form-language.component.scss']
})
export class FormLanguageComponent extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)

  department: KeyValuePair[] = [];
  language: Language = <Language>{}
  title: string = '';
  action: string = '';
  url: string = '';
  isEdit: boolean = false;
  isSave: boolean = false;
  callBack: boolean = false;

  constructor(
    private modalService: ModalService,
    private service: S_3_1_1_DepartmentMaintenanceService
  ) { super() }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({
    isSave: this.isSave,
    department_Name: this.language.detail.find(x => x.language_Code === "TW").department_Name
  })
  open(source: languageSource): void {
    const _source = structuredClone(source);
    this.callBack = _source.callBack
    this.language = _source.data;
    this.isEdit = this.id() == 'Edit'
    this.isSave = false
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.action = `System.Action.${this.id()}`
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.isEdit ? this.getDetail() : this.getListDepartment()
    this.directive.show()
  }
  save() {
    this.isSave = true
    this.language.department_Name = this.language.detail.find(x => x.language_Code === "TW").department_Name
    this.spinnerService.show();
    if (!this.isEdit) {
      this.service.addLanguage(this.language).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.CreateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.directive.hide();
            if (this.callBack)
              this.back()
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
    else {
      this.service.editLanguage(this.language).subscribe({
        next: result => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            this.snotifyService.success(this.translateService.instant(
              'System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success'));
            this.directive.hide();
          }
          else this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Error'));
        }
      })
    }
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }
  getListDepartment() {
    this.service.getListDepartment(this.language.division, this.language.factory).subscribe({
      next: (res) => {
        this.department = res;
        if (this.language.department_Code == null || this.language.department_Code == undefined)
          this.language.department_Code = this.department[0].key;
        this.getLanguage();
      }
    });
  }

  getLanguage() {
    this.service.getLanguage().subscribe({
      next: (res) => {
        this.language.detail = res.map(x => <LanguageParams>{
          language_Code: x.key,
          department_Name: x.key === 'TW' ? this.department.find(y => y.key == this.language.department_Code).value : ''
        });
      }
    })
  }
  getDetail() {
    this.service.getDetail(this.language.department_Code, this.language.division, this.language.factory)
      .subscribe({
        next: (res) => {
          this.language.detail = res
          for (const detailItem of this.language.detail)
            if (detailItem.language_Code === 'TW' && detailItem.department_Name == null)
              detailItem.department_Name = this.language.department_Name;
        }
      })
  }

  back = () => this.router.navigate([this.url]);

  onSelectChange = () => this.getLanguage();

  onClear() {
    this.language.department_Code = this.department[0].key;
    this.onSelectChange();
  }

  shouldShowClearButton(): boolean {
    if (this.language.department_Code === null || this.department.length === 0)
      return false;
    return this.language.department_Code !== this.department[0].key;
  }

  isSaveButtonDisabled(): boolean {
    if (this.language.detail && this.language.detail.length > 0) {
      for (const item of this.language?.detail)
        if (item.language_Code === 'TW' && !item.department_Name)
          return true;
    }
    return false;
  }
}
