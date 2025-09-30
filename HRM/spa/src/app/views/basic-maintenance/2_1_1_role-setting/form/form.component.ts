import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TreeviewItem } from '@ash-mezdo/ngx-treeview';
import { ClassButton, IconButton } from '@constants/common.constants';
import { RoleSettingParam, RoleSettingDto, RoleSetting } from '@models/basic-maintenance/2_1_1_role-setting';
import { S_2_1_1_RoleSetting } from '@services/basic-maintenance/s_2_1_1_role-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalComponent } from '../modal/modal.component';
import { AuthService } from '@services/auth/auth.service';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = ''
  selectedData: RoleSettingDto = <RoleSettingDto>{
    role_Setting: <RoleSetting>{ direct: '3' },
    role_List: []
  }
  iconButton = IconButton;
  classButton = ClassButton;
  url: string = '';
  action: string = '';
  selectedRole: string = '';

  roleList: TreeviewItem[] = [];
  factoryList: KeyValuePair[] = [];
  salaryCodeList: KeyValuePair[] = [];
  levelList: KeyValuePair[] = []
  levelListStart: KeyValuePair[] = []
  levelListEnd: KeyValuePair[] = []
  selectedLevelStart: KeyValuePair = <KeyValuePair>{}
  selectedLevelEnd: KeyValuePair = <KeyValuePair>{}
  isProgramGroup: boolean = false
  directList: KeyValuePair[] = [
    { key: '1', value: 'BasicMaintenance.RoleSetting.Direct' },
    { key: '2', value: 'BasicMaintenance.RoleSetting.Indirect' },
    { key: '3', value: 'BasicMaintenance.RoleSetting.All' }
  ];
  constructor(
    private activatedRoute: ActivatedRoute,
    private roleSettingService: S_2_1_1_RoleSetting,
    private modalService: ModalService,
    private authService: AuthService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
      this.getProgramGroupTemp()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: TreeviewItem[]) => {
      this.roleList = res
      this.selectedData.role_List = this.roleList[0].getSelection().checkedItems
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.activatedRoute.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.action = role.title
        this.filterList(role.dataResolved)
      })
    this.roleSettingService.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      this.selectedRole = res
      this.action == 'Add'
        ? this.getProgramGroupTemp()
        : res == null ? this.back() : this.getData()
    })
    this.levelListStart = this.levelList.slice();
    this.levelListEnd = this.levelList.slice();
    this.levelListEnd.shift()
  }
  retryGetDropDownList() {
    this.roleSettingService.getDropDownList()
      .subscribe({
        next: (res) => {
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "F")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.salaryCodeList = structuredClone(keys.filter((x: { key: string; }) => x.key == "S")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.levelList = structuredClone(keys.filter((x: { key: string; }) => x.key == "L")).map(x => <KeyValuePair>{ key: x.key = x.value, value: Number(x.value) })
  }
  levelChange(type: string, e: string) {
    if (this.levelList.length > 1) {
      if (type == 'start') {
        if (e == null) {
          this.levelListEnd = this.levelList.slice();
          this.levelListEnd.shift()
        }
        else
          this.levelListEnd = this.levelList.filter(x => x.value > +e).slice();
      }
      else
        this.levelListStart = e == null ? this.levelList.slice() : this.levelList.filter(x => x.value < +e).slice()
    }
  }
  async watch() {
    this.spinnerService.show()
    setTimeout(function (_this: any) {
      _this.modalService.open(_this.roleList);
      _this.spinnerService.hide()
    }, 600, this)
  }
  getData = () => {
    this.spinnerService.show();
    let para: RoleSettingParam = <RoleSettingParam>{ role: this.selectedRole };
    this.roleSettingService
      .getRoleSettingEdit(para)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.selectedData.role_Setting = res.role_Setting
          this.roleList = res.role_List.map(x => new TreeviewItem(x));
          this.selectedData.role_List = this.roleList[0].getSelection().checkedItems
          if (this.action == 'Copy') {
            this.selectedData.role_Setting.role = null
            this.selectedData.role_Setting.description = null
          }
        }
      });
  }
  getProgramGroupTemp = () => {
    this.spinnerService.show();
    this.roleSettingService
      .getProgramGroupTemplate()
      .subscribe({
        next: (res: TreeviewItem[]) => {
          let treeData = res.map(x => new TreeviewItem(x));
          this.roleList.length == 0 ? this.roleList = treeData : this.tradeLang(treeData, this.roleList)
          this.spinnerService.hide();
        }
      });
  };
  tradeLang(data: TreeviewItem[], target: TreeviewItem[]) {
    data.map((d, dIndex) => {
      let t = target[dIndex]
      t.text = d.text
      if (d.children != undefined)
        this.tradeLang(d.children, t.children)
    })
  }
  save() {
    if (this.action == 'Add' || this.action == 'Copy') {
      this.spinnerService.show();
      this.roleSettingService
        .postRole(this.selectedData)
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
                res.error,
                this.translateService.instant('System.Caption.Error')
              );
            }
          }
        })
    }
    else {
      this.spinnerService.show();
      this.roleSettingService
        .checkRole(this.selectedData.role_Setting.role)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              this.functionUtility.snotifyConfirm(
                'System.Message.ConfirmChangeSameRole',
                'System.Action.Confirm', true,
                () => this.putRole(() => this.authService.logout())
              );
            }
            else this.putRole(() => this.back())
          }
        })
    }
  }
  putRole(callbackFn: () => any) {
    this.spinnerService.show();
    this.roleSettingService
      .putRole(this.selectedData)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error, res.isSuccess)
          if (res.isSuccess) callbackFn()
        }
      })
  }
  back = () => this.router.navigate([this.url]);
  deleteProperty = (name: string) => delete this.selectedData.role_Setting[name]
}
