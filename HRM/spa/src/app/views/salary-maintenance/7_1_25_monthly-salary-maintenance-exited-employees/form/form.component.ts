import { Component, Input, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain,
  D_7_25_GetMonthlyAttendanceDataDetailParam,
  D_7_25_Query_Sal_Monthly_Detail_Result_Source,
  D_7_25_MonthlySalaryMaintenance_Update,
  D_7_25_GetMonthlyAttendanceDataDetailUpdateParam,
  D_7_25_Query_Sal_Monthly_Detail_Result,
} from '@models/salary-maintenance/7_1_25-monthly-salary-maintenance-exited-employees';
import { S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService } from '@services/salary-maintenance/s_7_1_25_monthly-salary-maintenance-exited-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  data_update: D_7_25_MonthlySalaryMaintenance_Update = {
    param: <D_7_25_GetMonthlyAttendanceDataDetailUpdateParam>{},
    table_details: <D_7_25_Query_Sal_Monthly_Detail_Result>{}
  };
  isValidAmount: boolean = true;
  title: string = '';
  url: string = '';
  action: string = '';
  classButton = ClassButton;
  data: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain = <D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain>{
    salary_Lock: '',
    year_Month: '',
    factory: '',
    department: '',
    department_Name: '',
    employee_ID: '',
    local_Full_Name: '',
    permission_Group: '',
    salary_Type: '',
    fiN_Pass_Status: '',
    transfer: '',
    tax: 0,
    // Monthly attendance data can be expanded/collapsed
    monthly_Attendance: {
      paid_Salary_Days: 0,
      actual_Work_Days: 0,
      new_Hired_Resigned: '',
      delay_Early: 0,
      no_Swip_Card: 0,
      day_Shift_Meal_Times: 0,
      overtime_Meal_Times: 0,
      night_Shift_Allowance_Times: 0,
      night_Shift_Meal_Times: 0
    },
    update_By: '',
    update_Time: ''
  };
  @Input('title') dataTitle?: string;
  isEdit: boolean = false;
  isCollapsed: boolean = true;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{};
  sal_Month_Value: Date;
  dataTypeaHead: string[];
  listDepartment: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  MAX_INT_VALUE = 2147483647;
  dataDetail: D_7_25_Query_Sal_Monthly_Detail_Result_Source = {
    monthly_Attendance_Data: {
      table_Left_Leave: [],
      table_Right_Allowance: []
    },
    monthly_Salary_Detail: {
      salary_Item_Table1: [],
      salary_Item_Table2: [],
      salary_Item_Table3: [],
      salary_Item_Table4: [],
      salary_Item_Table5: [],
      total_Item_Table1: 0,
      total_Item_Table2: 0,
      total_Item_Table3: 0,
      total_Item_Table4: 0,
      total_Item_Table5: 0,
      totalAmountReceived: 0
    }
  };

  constructor(
    private service: S_7_1_25_MonthlySalaryMaintenanceExitedEmployeesService,
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList()
      this.getDetail();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title
      this.getSource();
    });
    if (this.isEdit)
      this.data.probation = "";
  }
  getSource() {
    this.isEdit = this.dataTitle == 'Edit';
    const source = this.service.paramSource();
    if (source.dataItem && Object.keys(source.dataItem).length > 0) {
      this.data = structuredClone(source.dataItem)
      if (this.functionUtility.isValidDate(new Date(this.data.year_Month)))
        this.sal_Month_Value = new Date(this.data.year_Month);
      this.loadDropdownList()
      this.getDetail();
    } else this.back()
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
    this.getListSalaryType();
  }

  // recalculate total table
  calculateTotals() {
    this.onUpdateTimeChangeAlways();
    const totalTable1: number = this.dataDetail.monthly_Salary_Detail.salary_Item_Table1.reduce((sum, item) => sum + (isNaN(item.amount) ? 0 : +item.amount), 0);
    const totalTable2: number = this.dataDetail.monthly_Salary_Detail.salary_Item_Table2.reduce((sum, item) => sum + (isNaN(item.amount) ? 0 : +item.amount), 0);
    const totalTable3: number = this.dataDetail.monthly_Salary_Detail.salary_Item_Table3.reduce((sum, item) => sum + (isNaN(item.amount) ? 0 : +item.amount), 0);
    const totalTable4: number = this.dataDetail.monthly_Salary_Detail.salary_Item_Table4.reduce((sum, item) => sum + (isNaN(item.amount) ? 0 : +item.amount), 0);
    const totalTable5: number = this.dataDetail.monthly_Salary_Detail.salary_Item_Table5.reduce((sum, item) => sum + (isNaN(item.amount) ? 0 : +item.amount), 0);
    // reset total table
    this.dataDetail.monthly_Salary_Detail.total_Item_Table1 = totalTable1;
    this.dataDetail.monthly_Salary_Detail.total_Item_Table2 = totalTable2;
    this.dataDetail.monthly_Salary_Detail.total_Item_Table3 = totalTable3;
    this.dataDetail.monthly_Salary_Detail.total_Item_Table4 = totalTable4;
    this.dataDetail.monthly_Salary_Detail.total_Item_Table5 = totalTable5;
    this.dataDetail.monthly_Salary_Detail.totalAmountReceived = totalTable1 + totalTable2 + totalTable3 - totalTable4 - totalTable5 - this.data.tax;
  }

  recal(source: D_7_25_Query_Sal_Monthly_Detail_Result_Source) {
    const dataDetail = source.monthly_Salary_Detail;
    const monthly_Attendance_Data = source.monthly_Attendance_Data;

    const tables = [
      { key: 'salary_Item_Table1', data: dataDetail.salary_Item_Table1 },
      { key: 'salary_Item_Table2', data: dataDetail.salary_Item_Table2 },
      { key: 'salary_Item_Table3', data: dataDetail.salary_Item_Table3 },
      { key: 'salary_Item_Table4', data: dataDetail.salary_Item_Table4 },
      { key: 'salary_Item_Table5', data: dataDetail.salary_Item_Table5 }
    ];

    const lengths = [
      dataDetail.salary_Item_Table1.length,
      dataDetail.salary_Item_Table2.length,
      dataDetail.salary_Item_Table3.length,
      dataDetail.salary_Item_Table4.length,
      dataDetail.salary_Item_Table5.length
    ];

    if (lengths.some(length => length > 0)) {
      const maxLength: number = Math.max(...lengths);

      tables.forEach(table => {
        const currentLength = table.data.length;

        // Nếu thiếu phần tử thì thêm các phần tử temp cho đủ maxLength
        if (currentLength < maxLength) {
          const missingCount = maxLength - currentLength;
          for (let i = 0; i < missingCount; i++) {
            table.data.push({
              item: '',
              item_Name: '___temp',
              amount: 0,
              type_Seq: '',
              addDed_Type: ''
            });
          }
        }
      });

      this.dataDetail = {
        monthly_Attendance_Data: monthly_Attendance_Data,
        monthly_Salary_Detail: {
          salary_Item_Table1: tables.find(table => table.key === 'salary_Item_Table1')?.data || [],
          salary_Item_Table2: tables.find(table => table.key === 'salary_Item_Table2')?.data || [],
          salary_Item_Table3: tables.find(table => table.key === 'salary_Item_Table3')?.data || [],
          salary_Item_Table4: tables.find(table => table.key === 'salary_Item_Table4')?.data || [],
          salary_Item_Table5: tables.find(table => table.key === 'salary_Item_Table5')?.data || [],
          total_Item_Table1: dataDetail.total_Item_Table1,
          total_Item_Table2: dataDetail.total_Item_Table2,
          total_Item_Table3: dataDetail.total_Item_Table3,
          total_Item_Table4: dataDetail.total_Item_Table4,
          total_Item_Table5: dataDetail.total_Item_Table5,
          totalAmountReceived: dataDetail.totalAmountReceived
        }
      };
    }
  }

  hasItemsInTable1(): boolean {
    return this.dataDetail?.monthly_Salary_Detail?.salary_Item_Table1?.length > 0;
  }
  hasItemsInTable2(): boolean {
    return this.dataDetail?.monthly_Salary_Detail?.salary_Item_Table2?.length > 0;
  }
  hasItemsInTable3(): boolean {
    return this.dataDetail?.monthly_Salary_Detail?.salary_Item_Table3?.length > 0;
  }
  hasItemsInTable4(): boolean {
    return this.dataDetail?.monthly_Salary_Detail?.salary_Item_Table4?.length > 0;
  }
  hasItemsInTable5(): boolean {
    return this.dataDetail?.monthly_Salary_Detail?.salary_Item_Table5?.length > 0;
  }
  getDetail() {
    const _param = <D_7_25_GetMonthlyAttendanceDataDetailParam>{
      probation: this.data.probation,
      tax: this.data.tax,
      employee_ID: this.data.employee_ID,
      factory: this.data.factory,
      permission_Group: this.data.permission_Group,
      salary_Type: this.data.salary_Type,
      year_Month: this.data.year_Month
    }
    this.service.get_MonthlyAttendanceData_MonthlySalaryDetail(_param).subscribe({
      next: (res) => {
        this.dataDetail = res
        if (this.data.probation && ['Y', 'N'].includes(this.data.probation)) {
          if (this.dataDetail.monthly_Attendance_Data.table_Left_Leave)
            this.dataDetail.monthly_Attendance_Data.table_Left_Leave.forEach(item => item.days = 0);
          if (this.dataDetail.monthly_Attendance_Data.table_Right_Allowance)
            this.dataDetail.monthly_Attendance_Data.table_Right_Allowance.forEach(item => item.days = 0);
        }
        this.recal(res);
      }
    });
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }

  saveData() {
    this.spinnerService.show();
    const _param = <D_7_25_GetMonthlyAttendanceDataDetailUpdateParam>{
      tax: this.data.tax,
      employee_ID: this.data.employee_ID,
      factory: this.data.factory,
      update_By: this.updateBy,
      sal_Month: this.data.year_Month
    }
    this.data_update.param = _param;
    this.data_update.table_details = this.dataDetail.monthly_Salary_Detail
    this.service.update(this.data_update).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess)
          this.back();
      }
    });
  }

  onUpdateTimeChangeAlways() {
    this.data.update_By = this.updateBy
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }

  validateAmount(amount: number) {
    if (amount > this.MAX_INT_VALUE || amount < 0) {
      this.isValidAmount = false;
    } else {
      this.isValidAmount = true;
    }
  }

  getListDepartment() {
    this.service.getListDepartment(this.data.factory).subscribe({
      next: (res) => {
        this.listDepartment = res
      },
    });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.data.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
      }
    })
  }

  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }

  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }

  back = () => this.router.navigate([this.url]);

}
