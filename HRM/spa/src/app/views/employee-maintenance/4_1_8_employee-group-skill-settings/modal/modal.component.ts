import {
  AfterViewInit,
  Component,
  EventEmitter,
  input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { EmployeeGroupSkillSettings_Main, EmployeeGroupSkillSettings_Param, EmployeeGroupSkillSettings_SkillDetail } from '@models/employee-maintenance/4_1_8_employee-group-skill-settings';
import { S_4_1_8_EmployeeGroupSkillSettings } from '@services/employee-maintenance/s_4_1_8_employee-group-skill-settings.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
  }
  data: EmployeeGroupSkillSettings_Main;
  modalChange = new EventEmitter<EmployeeGroupSkillSettings_SkillDetail[]>();

  dataMain: EmployeeGroupSkillSettings_SkillDetail[] = []
  paramForm: EmployeeGroupSkillSettings_Param = <EmployeeGroupSkillSettings_Param>{};

  iconButton = IconButton;
  classButton = ClassButton;

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  skillList: KeyValuePair[] = [];
  isAllow: boolean = false
  constructor(
    private service: S_4_1_8_EmployeeGroupSkillSettings,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.dataMain })

  open(data: EmployeeGroupSkillSettings_Main): void {
    this.data = structuredClone(data);
    this.paramForm = <EmployeeGroupSkillSettings_Param>{}
    this.dataMain = []
    this.checkData(false)
    this.getData()
    this.isSave = false
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.isSave = true
    this.directive.hide();
  }
  add() {
    this.checkData(true)
    if (this.isAllow) {
      this.dataMain.push(<EmployeeGroupSkillSettings_SkillDetail>{})
      this.calculateSeq()
    }
    this.isAllow = false
  }
  remove(item: EmployeeGroupSkillSettings_SkillDetail) {
    this.dataMain = this.dataMain.filter(val => val.seq != item.seq)
    this.calculateSeq()
    this.checkData(false)
  }
  getData() {
    this.spinnerService.show()
    this.service.getDropDownList(this.data.division)
      .subscribe({
        next: (res) => {
          this.skillList = res.filter((x: { key: string; }) => x.key == "SK").map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
          this.factoryList = res.filter((x: { key: string; }) => x.key == "FA").map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
          this.divisionList = res.filter((x: { key: string; }) => x.key == "DI").map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
          this.paramForm.division = this.divisionList.find(val => val.key == this.data.division)?.value
          this.paramForm.factory = this.factoryList.find(val => val.key == this.data.factory)?.value
          this.paramForm.employee_Id = this.data?.employee_Id
          this.paramForm.local_Full_Name = this.data?.local_Full_Name
          this.dataMain = this.data.skill_Detail_List.slice()
          this.spinnerService.hide()
        }
      });
  }
  calculateSeq() {
    this.dataMain.map((val, ind) => {
      val.seq = ind + 1 + ''
    })
  }
  checkData(onAdd: boolean) {
    if (this.dataMain.length > 0) {
      const lookup = this.dataMain.reduce((a, e) => {
        a[e.skill_Certification + e.passing_Date_Str] = ++a[e.skill_Certification + e.passing_Date_Str] || 0;
        return a;
      }, {});
      if (this.dataMain.filter(val => !val.passing_Date || !val.skill_Certification).length > 0) {
        this.isAllow = false
        if (onAdd)
          return this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.EmployeeGroupSkillSettings.AddEmptyError')
      }
      else if (this.dataMain.filter(e => lookup[e.skill_Certification + e.passing_Date_Str]).length > 0) {
        this.isAllow = false
        this.functionUtility.snotifySuccessError(false, 'EmployeeInformationModule.EmployeeGroupSkillSettings.AddDuplicateError')
      }
      else this.isAllow = true
    }
    else this.isAllow = true
  }
  onDateChange(item: EmployeeGroupSkillSettings_SkillDetail) {
    item.passing_Date_Str = item.passing_Date ? this.functionUtility.getDateFormat(new Date(item.passing_Date)) : '';
    this.checkData(false)
  }
}
