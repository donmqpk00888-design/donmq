import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  MonthlySalaryMaintenance_Update,
  MonthlySalaryMaintenanceDetail,
  MonthlySalaryMaintenanceDto,
  MonthlySallaryDetail_Table
} from '@models/salary-maintenance/7_1_24_monthly-salary-maintenance';
import { S_7_1_24_MonthlySalaryMaintenanceService } from '@services/salary-maintenance/s_7_1_24_monthly-salary-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  action: string = '';
  isEmployee: boolean = false;
  classButton = ClassButton;
  data: MonthlySalaryMaintenanceDto = <MonthlySalaryMaintenanceDto>{};
  dataSave: MonthlySalaryMaintenance_Update = <MonthlySalaryMaintenance_Update>{};
  isQuery: boolean = false;
  isCollapsed = true;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{};
  sal_Month_Value: Date;
  dataTypeaHead: string[]
  listDepartment: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  dataSource: MonthlySalaryMaintenanceDto = <MonthlySalaryMaintenanceDto>{}
  dataDetail: MonthlySalaryMaintenanceDetail = <MonthlySalaryMaintenanceDetail>{
    salaryDetail: {
      totalAmountReceived: 0,
      tax: 0,
      table_1: [],
      table_2: [],
      table_3: [],
      table_4: [],
      table_5: [],
    }
  };

  constructor(
    private service: S_7_1_24_MonthlySalaryMaintenanceService,
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDetail();
      this.getDropdownList()
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title
      this.isQuery = res.title == 'Query';
      this.getSource()
    })
    if (!this.isQuery)
      this.data.probation = '';
  }
  getSource() {
    this.isQuery = this.action == 'Query'
    let source = this.service.paramSource();
    if (source.selectedData && Object.keys(source.selectedData).length > 0) {
      this.data = structuredClone(source.selectedData)
      if (this.functionUtility.isValidDate(new Date(this.data.sal_Month)))
        this.sal_Month_Value = new Date(this.data.sal_Month);
      this.getDetail();
      this.getDropdownList()
    } else this.back()
  }
  getDropdownList() {
    this.getListFactory();
    this.getListSalaryType();
    if (!this.functionUtility.checkEmpty(this.data.factory)) {
      this.getListDepartment();
      this.getListPermissionGroup();
    }
  }
  getDetail() {
    this.spinnerService.show();
    this.service.getDetail(this.data).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        this.dataDetail = res
        this.dataDetail.salaryDetail.tax = this.data.tax

        if (this.data.probation && ['Y', 'N'].includes(this.data.probation)) {
          if (this.dataDetail.leave)
            this.dataDetail.leave.forEach(item => item.monthlyDays = 0);
          if (this.dataDetail.allowance)
            this.dataDetail.allowance.forEach(item => item.monthlyDays = 0);
        }

        const tableLengths = [
          this.dataDetail.salaryDetail.table_1.reduce((sum, item) => sum + item.listItem.length, 0),
          this.dataDetail.salaryDetail.table_2.reduce((sum, item) => sum + item.listItem.length, 0),
          this.dataDetail.salaryDetail.table_3.reduce((sum, item) => sum + item.listItem.length, 0),
          this.dataDetail.salaryDetail.table_4.reduce((sum, item) => sum + item.listItem.length, 0),
          this.dataDetail.salaryDetail.table_5.reduce((sum, item) => sum + item.listItem.length, 0)
        ];

        const maxLength = Math.max(...tableLengths);

        const defaultItem = { item: '...', item_Name: '', amount: 0 };

        const tables = [
          this.dataDetail.salaryDetail.table_1,
          this.dataDetail.salaryDetail.table_2,
          this.dataDetail.salaryDetail.table_3,
          this.dataDetail.salaryDetail.table_4,
          this.dataDetail.salaryDetail.table_5
        ];

        tables.forEach(table => {
          let currentLength = table.reduce((sum, item) => sum + item.listItem.length, 0);

          const itemsToAdd = maxLength - currentLength;

          for (let i = 0; i < itemsToAdd; i++) {
            table.forEach(item => {
              item.listItem.push(defaultItem);
            });
          }
        });
      },
    });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }

  getListDepartment() {
    this.service.getListDepartment(this.data.factory).subscribe({
      next: (res) => {
        this.listDepartment = res
      },
    });
  }
  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }
  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.data.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
      }
    })
  }

  back = () => this.router.navigate([this.url]);

  save() {
    this.spinnerService.show();
    this.changeParamSave();
    this.service.update(this.dataSave).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          this.back();
        } else {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        }
      },

    });
  }
  changeParamSave() {
    this.dataSave.tax = this.dataDetail.salaryDetail.tax;
    this.dataSave.salaryDetail = this.dataDetail.salaryDetail;
    this.dataSave.factory = this.data.factory;
    this.dataSave.employee_ID = this.data.employee_ID;
    this.dataSave.sal_Month = this.data.sal_Month;
    this.dataSave.update_By = this.data.update_By;
    this.dataSave.update_Time = this.data.update_Time;
  }

  onValueChange() {
    this.data.update_By = this.updateBy;
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date());
  }

  calculateTotals() {
    this.onValueChange();
    const totalTable1: number = this.calculateTableTotal(this.dataDetail.salaryDetail.table_1);
    const totalTable2: number = this.calculateTableTotal(this.dataDetail.salaryDetail.table_2);
    const totalTable3: number = this.calculateTableTotal(this.dataDetail.salaryDetail.table_3);
    const totalTable4: number = this.calculateTableTotal(this.dataDetail.salaryDetail.table_4);
    const totalTable5: number = this.calculateTableTotal(this.dataDetail.salaryDetail.table_5);

    this.dataDetail.salaryDetail.table_1[0].sumAmount = totalTable1;
    this.dataDetail.salaryDetail.table_2[0].sumAmount = totalTable2;
    this.dataDetail.salaryDetail.table_3[0].sumAmount = totalTable3;
    this.dataDetail.salaryDetail.table_4[0].sumAmount = totalTable4;
    this.dataDetail.salaryDetail.table_5[0].sumAmount = totalTable5;

    this.dataDetail.salaryDetail.totalAmountReceived = totalTable1 + totalTable2 + totalTable3 - totalTable4 - totalTable5 - this.dataDetail.salaryDetail.tax;
  }
  calculateTableTotal = (table: MonthlySallaryDetail_Table[]): number => {
    return table.reduce((sum: number, item) => {
      const itemTotal: number = item.listItem.reduce((itemSum, listItem) => {
        return +(itemSum) + (isNaN(listItem.amount) ? 0 : +(listItem.amount));
      }, 0);
      return sum + itemTotal;
    }, 0);
  };
}
