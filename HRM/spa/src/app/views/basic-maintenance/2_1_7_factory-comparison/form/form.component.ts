import { Component } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Basic_Factory_ComparisonAdd } from '@models/basic-maintenance/2_1_7_factory-comparison';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_7_FactoryComparisonService } from '@services/basic-maintenance/s_2_1_7_factory-comparison.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase {
  factoryComparisons: HRMS_Basic_Factory_ComparisonAdd[] = [];
  divisions: KeyValuePair[] = [];
  factories: KeyValuePair[] = [];
  kind: string;
  kinds: KeyValuePair[] = [
    { key: '1', value: 'BasicMaintenance.FactoryComparison.Kind1' },
    { key: 'X', value: 'BasicMaintenance.FactoryComparison.KindX' },
  ];
  isSave: boolean = false;
  title: string = '';
  url: string = '';
  action: string = '';
  iconButton = IconButton;

  constructor(private service: S_2_1_7_FactoryComparisonService) { super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
   }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res =>
      this.action = res.title
    );
    this.getDivisions();
    this.getFactories();
    this.init();
  }

  init() {
    this.factoryComparisons = [
      <HRMS_Basic_Factory_ComparisonAdd>{ kind: this.kind, division: '', factory: '', isValid: false }
    ]
  }

  getDivisions() {
    this.service.getDivisions().subscribe({
      next: result => this.divisions = result
    })
  }

  getFactories() {
    this.service.getFactories().subscribe({
      next: result => this.factories = result
    })
  }

  checkInvalid() {
    if (this.factoryComparisons.length == 0)
      this.isSave = true
    else {
      this.factoryComparisons.forEach((x, i) => {
        x.isValid = this.factoryComparisons.some((e, j) => e.division == x.division && e.factory == x.factory && i != j);
      })
      this.isSave = this.factoryComparisons.some(x => x.isValid);
    }
  }

  back = () => this.router.navigate([this.url]);
  cancel = () => this.back();

  saveChange() {
    // Kiểm tra danh sách thêm vào
    if (this.factoryComparisons.length == 0)
      return this.snotifyService.warning(this.translateService.instant('BasicMaintenance.FactoryComparison.Existed'), this.translateService.instant('System.Caption.Warning'));

    // Kiểm tra danh sách Invalid
    if (this.factoryComparisons.some(x => x.isValid))
      return this.snotifyService.warning(this.translateService.instant('BasicMaintenance.FactoryComparison.Invalid'), this.translateService.instant('System.Caption.Warning'));

    this.factoryComparisons.forEach((item) => {
      item.kind = this.kind;
    });
    // Add
    this.spinnerService.show();
    this.service.create(this.factoryComparisons).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.CreateOKMsg' : result.error, result.isSuccess)
        if (result.isSuccess) this.back();
      }
    })
  }

  onAddFactoryComparison() {
    this.factoryComparisons.push(<HRMS_Basic_Factory_ComparisonAdd>{ kind: this.kind, division: '', factory: '', isValid: false });
  }

  onKindChange = () => this.isSave = this.factoryComparisons.some(x => x.isValid);


  onCheckInvalid(index: number) {
    this.factoryComparisons.forEach((x, i) => {
      // Nếu trùng vị trí , kiểm tra trùng dữ liệu, nếu có thì báo lỗi
      if (i == index) {
        // Kiểm tra trùng dữ liệu
        x.isValid = this.factoryComparisons.some((e, j) => e.division == x.division && e.factory == x.factory && i != j);
        if (x.isValid)
          return this.snotifyService.warning(this.translateService.instant('BasicMaintenance.FactoryComparison.Existed'), this.translateService.instant('System.Caption.Warning'));
      }
    })

    this.checkInvalid();
  }

  onRemoveItem(index: number) {
    this.factoryComparisons = this.factoryComparisons.filter((_, i) => i != index);
    this.checkInvalid()
  }
  resetKind = () => delete this.kind
}
