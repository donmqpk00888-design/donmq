import { Component, OnInit } from '@angular/core';
import { SeaDeleteEmployeeService } from '@services/seahr/seaDeleteEmployee.service';
import { catchError, firstValueFrom, of, tap } from 'rxjs';
import { InjectBase } from '@utilities/inject-base-app';

@Component({
  selector: 'app-seahr-delete',
  templateUrl: './seahr-delete.component.html',
  styleUrls: ['./seahr-delete.component.scss'],
})
export class SeahrDeleteComponent extends InjectBase implements OnInit {
  fileExcel: File | any;
  file: File | any;
  accept: string = '.xls, .xlsm, .xlsx';
  constructor(
    private service: SeaDeleteEmployeeService,
  ) {
    super();
  }

  ngOnInit() { }
  back() {
    this.router.navigate(['seahr']);
  }

  onSelectFile(event: any) {
    if (event.target.files && event.target.files[0]) {
      const reader = new FileReader();

      reader.readAsDataURL(event.target.files[0]); // read file as data url
      const file = event.target.files[0];
      // check file name extension
      const fileNameExtension = event.target.files[0].name.split('.').pop();
      if (!this.accept.includes(fileNameExtension)) {
        return this.snotifyService.error(
          this.translateService.instant('System.Message.AllowExcelFile'),
          this.translateService.instant('System.Caption.Error')
        );
      }
      this.fileExcel = file;
    }
  }
  uploadFile() {
    if (this.fileExcel == null) {
      return this.snotifyService.warning(
        this.translateService.instant('System.Message.InvalidFile'),
        this.translateService.instant('System.Caption.Warning')
      );
    }
    this.spinnerService.show();
    this.service.uploadExcel(this.fileExcel).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (res.data) {
            this.functionUtility.exportExcel(res.data.file, res.data.name);
            this.snotifyService.warning(
              this.translateService.instant('SeaHr.SearHrAddEmployee.' + res.error),
              this.translateService.instant('System.Caption.Warning')
            );
          } else {
            this.snotifyService.success(
              this.translateService.instant('SeaHr.SearHrAddEmployee.' + res.error),
              this.translateService.instant('System.Caption.Success')
            );
          }
          this.back();
        } else {
          if (res.data)
            this.functionUtility.exportExcel(res.data.file, res.data.name);
          this.snotifyService.error(
            this.translateService.instant('SeaHr.SearHrAddEmployee.' + res.error),
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
      error: () => {
        this.spinnerService.hide();
        this.snotifyService.error(
          this.translateService.instant('System.Message.UnknowError'),
          this.translateService.instant('System.Caption.Error'));
      }
    });
  }
  async dowloadFile() {
    this.spinnerService.show();
    await firstValueFrom(
      this.service.downloadExcel().pipe(
        tap((res) => this.spinnerService.hide()),
        catchError((err) => {
          this.spinnerService.hide();
          this.snotifyService.error(this.translateService.instant('System.Message.UnknowError'), this.translateService.instant('System.Caption.Error'));
          return of(null);
        })
      )
    );
  }
}
