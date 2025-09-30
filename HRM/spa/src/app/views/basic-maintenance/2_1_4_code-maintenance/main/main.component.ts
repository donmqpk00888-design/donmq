import { Component, OnDestroy, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { CodeMaintenanceParam, HRMS_Basic_Code, HRMS_Basic_Code_Source } from '@models/basic-maintenance/2_1_4_code-maintenance';
import { S_2_1_4_CodeMaintenanceService } from '@services/basic-maintenance/s_2_1_4-code-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnDestroy {

  //#region Data
  codeMaintenances: HRMS_Basic_Code[] = [];
  typeSeqs: KeyValuePair[] = [];

  //#endregion

  //#region Vaiables
  codeMaintenanceParam: CodeMaintenanceParam = <CodeMaintenanceParam>{}
  selectedData: HRMS_Basic_Code = <HRMS_Basic_Code>{}
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  //#endregion

  //#region Pagination
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  //#endregion

  constructor(
    private codeMaintenanceServices: S_2_1_4_CodeMaintenanceService,
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    // Load danh sách Data trước đó
    effect(() => {
      // 0. Gán params & pagination
      this.codeMaintenanceParam = this.codeMaintenanceServices.basicCodeSource().param;
      this.pagination = this.codeMaintenanceServices.basicCodeSource().pagination;
      this.codeMaintenances = this.codeMaintenanceServices.basicCodeSource().data;
      if (this.codeMaintenances.length > 0) {
        this.functionUtility.checkFunction('Search')
          ? this.getPaginationData()
          : this.clear()
      }
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    });
  }
  ngOnDestroy(): void {
    this.codeMaintenanceServices.setSource(<HRMS_Basic_Code_Source>{
      pagination: this.pagination, param: this.codeMaintenanceParam, data: this.codeMaintenances, model: this.selectedData
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    // load dữ liệu [TypeSeqs]
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (res) => {
        this.typeSeqs = res.resolverTypeSeqs
      });
  }
  //#region Methods
  getTypeSeqs() {
    this.codeMaintenanceServices.getTypeSeqs().subscribe({
      next: result => this.typeSeqs = result
    })
  }

  getPaginationData(isSearch?: boolean) {
    this.spinnerService.show();
    this.codeMaintenanceServices.getDataMainPagination(this.pagination, this.codeMaintenanceParam).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.codeMaintenances = result.result;
        this.pagination = result.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    })
  }

  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getPaginationData(isSearch) : this.pagination.pageNumber = 1;

  deleteProperty = (name: string) => delete this.codeMaintenanceParam[name]

  clear() {
    this.codeMaintenanceParam = <CodeMaintenanceParam>{}
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.codeMaintenances = [];
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPaginationData();
  }
  //#endregion

  //#region Events
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: HRMS_Basic_Code) {
    this.selectedData = item;
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDetail(item: HRMS_Basic_Code) {
    this.selectedData = item;
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  onDelete(typeSeq: string, code: string) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.codeMaintenanceServices.delete(typeSeq, code).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? `System.Message.DeleteOKMsg` : result.error, result.isSuccess)
          if (result.isSuccess) this.search(false);
        }
      })
    });
  }

  onExport() {
    this.spinnerService.show();
    this.codeMaintenanceServices.download(this.codeMaintenanceParam).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        this.functionUtility.exportExcel(result.data, fileName);
      }
    });
  }
  //#endregion

}
