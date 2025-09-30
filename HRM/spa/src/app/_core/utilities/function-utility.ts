import { CommonService } from '@services/common.service';
import { HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { LocalStorageConstants, SessionStorageConstants } from "@constants/local-storage.constants";
import { CodeInformation, FunctionInfomation } from '@models/common';
import { TranslateService } from "@ngx-translate/core";
import { NgxSpinnerService } from "ngx-spinner";
import { NgSnotifyService } from "../services/ng-snotify.service";
import { Pagination } from "./pagination-utility";
import { KeyValuePair } from "./key-value-pair";
import { take } from "rxjs";

@Injectable({
  providedIn: "root",
})
export class FunctionUtility {
  /**
   * @class FunctionUtility
   * @description Chứa các hàm tiện ích dùng chung trong ứng dụng.
   */

  constructor(
    private snotify: NgSnotifyService,
    private translateService: TranslateService,
    private spinnerService: NgxSpinnerService,
    private commonService: CommonService
  ) { }

  /**
   * @function getToDay
   * @description Trả về ngày hiện tại dưới dạng chuỗi, chỉ lấy năm, tháng, ngày: yyyy/MM/dd.
   * @returns {string} Ngày hiện tại dưới dạng chuỗi yyyy/MM/dd.
   */
  getToDay(): string {
    const toDay =
      new Date().getFullYear().toString() +
      "/" +
      (new Date().getMonth() + 1).toString() +
      "/" +
      new Date().getDate().toString();
    return toDay;
  }

  /**
   * @function getDateFormat
   * @description Trả về chuỗi ngày tháng năm, chỉ lấy năm, tháng, ngày: yyyy/MM/dd.
   * @param {Date} date - Đối tượng Date cần được format.
   * @returns {string} Ngày tháng năm đã được format dưới dạng chuỗi yyyy/MM/dd.
   */
  getDateFormat(date: Date): string {
    return (
      date.getFullYear() +
      "/" +
      (date.getMonth() + 1 < 10
        ? "0" + (date.getMonth() + 1)
        : date.getMonth() + 1) +
      "/" +
      (date.getDate() < 10 ? "0" + date.getDate() : date.getDate())
    );
  }

  /**
   * @function getDateTimeFormat
   * @description Trả về chuỗi ngày giờ: yyyy/MM/dd HH:mm:ss.
   * @param {Date} date - Đối tượng Date cần được format.
   * @returns {string} Ngày giờ đã được format dưới dạng chuỗi yyyy/MM/dd HH:mm:ss.
   */
  getDateTimeFormat(date: Date): string {
    return (
      date.getFullYear() +
      "/" +
      (date.getMonth() + 1 < 10
        ? "0" + (date.getMonth() + 1)
        : date.getMonth() + 1) +
      "/" +
      (date.getDate() < 10 ? "0" + date.getDate() : date.getDate()) +
      " " +
      (date.getHours() < 10 ? "0" + date.getHours() : date.getHours()) +
      ":" +
      (date.getMinutes() < 10 ? "0" + date.getMinutes() : date.getMinutes()) +
      ":" +
      (date.getSeconds() < 10 ? "0" + date.getSeconds() : date.getSeconds())
    );
  }

  /**
   * @function getUTCDate
   * @description Trả về đối tượng Date ở múi giờ UTC.
   * @param {Date} [d] - Đối tượng Date. Nếu không truyền vào, mặc định là ngày giờ hiện tại.
   * @returns {Date} Đối tượng Date ở múi giờ UTC.
   */
  getUTCDate(d?: Date): Date {
    let date = d ? d : new Date();
    return new Date(
      Date.UTC(
        date.getFullYear(),
        date.getMonth(),
        date.getDate(),
        date.getHours(),
        date.getMinutes(),
        date.getSeconds()
      )
    );
  }

  /**
   * @function checkEmpty
   * @description Kiểm tra một chuỗi có phải là empty, null hoặc undefined hay không.
   * @param {any} str - Chuỗi cần kiểm tra.
   * @returns {boolean} `true` nếu chuỗi là empty, null hoặc undefined, `false` nếu không.
   */
  checkEmpty(str: any): boolean {
    return !str || /^\s*$/.test(str);
  }

  /**
   * @function isValidDate
   * @description Kiểm tra một đối tượng có phải là Date hợp lệ hay không.
   * @param {Date} d - Đối tượng cần kiểm tra.
   * @returns {boolean} `true` nếu là Date hợp lệ, `false` nếu không.
   */
  isValidDate(d: Date): boolean {
    return d instanceof Date && !isNaN(d.getTime());
  }

  /**
   * @function calculatePagination
   * @description Kiểm tra số lượng phần tử ở trang hiện tại, nếu bằng 1 và trang hiện tại không phải là trang 1 thì cho pageNumber lùi 1 trang.
   * @param {Pagination} pagination - Đối tượng Pagination chứa thông tin phân trang.
   * @returns {void} Không trả về giá trị.
   */
  calculatePagination(pagination: Pagination): void {
    // Kiểm tra trang hiện tại phải là trang cuối không và trang hiện tại không phải là trang 1
    if (
      pagination.pageNumber === pagination.totalPage &&
      pagination.pageNumber !== 1
    ) {
      // Lấy ra số lượng phần tử hiện tại của trang
      let currentItemQty =
        pagination.totalCount -
        (pagination.pageNumber - 1) * pagination.pageSize;

      // Nếu bằng 1 thì lùi 1 trang
      if (currentItemQty === 1) {
        pagination.pageNumber--;
      }
    }
  }

  /**
   * @function changeDomClassList
   * @description Thêm hoặc xóa class tác động vào id element trên DOM.
   * @param {string} id - ID của phần tử DOM.
   * @param {string} className - Tên class cần thêm hoặc xóa.
   * @param {boolean} type - `true` để thêm class, `false` để xóa class.
   * @returns {void} Không trả về giá trị.
   */
  changeDomClassList(id: string, className: string, type: boolean): void {
    type
      ? document.getElementById(id).classList.add(className)
      : document.getElementById(id).classList.remove(className);
  }

  /**
   * @function toFormData
   * @description Chuyển đổi một đối tượng thành FormData để gửi lên server. Hỗ trợ nested object và Date.
   * @param {any} obj - Đối tượng cần chuyển đổi.
   * @param {FormData} [form] - Đối tượng FormData (sử dụng khi gọi đệ quy).
   * @param {string} [namespace] - Namespace của property (sử dụng khi gọi đệ quy).
   * @returns {FormData} Đối tượng FormData đã được tạo.
   */
  toFormData(obj: any, form?: FormData, namespace?: string): FormData {
    let fd = form || new FormData();
    let formKey: string;
    for (var property in obj) {
      if (obj.hasOwnProperty(property)) {
        // namespaced key property
        if (!isNaN(property as any)) {
          // obj is an array
          formKey = namespace ? `${namespace}[${property}]` : property;
        } else {
          // obj is an object
          formKey = namespace ? `${namespace}.${property}` : property;
        }
        if (obj[property] instanceof Date) {
          // the property is a date, so convert it to a string
          fd.append(formKey, obj[property].toISOString());
        } else if (typeof obj[property] === 'object' && !(obj[property] instanceof File)) {
          // the property is an object or an array, but not a File, use recursivity
          this.toFormData(obj[property], fd, formKey);
        } else {
          // the property is a string, number or a File object
          fd.append(formKey, obj[property]);
        }
      }
    }
    return fd;
  }

  /**
   * @function toParams
   * @description Chuyển đổi một đối tượng thành HttpParams để gửi lên server.
   * @param {any} formValue - Đối tượng cần chuyển đổi.
   * @returns {HttpParams} Đối tượng HttpParams đã được tạo.
   */
  toParams(formValue: any): HttpParams {
    let params = new HttpParams();
    for (const key of Object.keys(formValue)) {
      const value = formValue[key];
      params = params.append(key, value);
    }
    return params;
  }

  /**
   * @function exportExcel
   * @description Xuất dữ liệu ra file Excel.
   * @param {Blob | string} result - Dữ liệu Blob hoặc chuỗi base64.
   * @param {string} fileName - Tên file khi xuất.
   * @param {string} [type='xlsx'] - Loại file xuất, mặc định là 'xlsx'.
   * @returns {void} Không trả về giá trị.
   * @description Nếu truyền vào là string thì chuyển về blob
   */
  exportExcel(result: Blob | string, fileName: string, type?: string): void {
    if (!type) type = 'xlsx';
    if (typeof result === "string") {
      let byteCharacters = atob(result);
      let byteArrays = [];
      for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        let slice = byteCharacters.slice(offset, offset + 512);
        let byteNumbers = new Array(slice.length);
        for (var i = 0; i < slice.length; i++) {
          byteNumbers[i] = slice.charCodeAt(i);
        }
        let byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
      }
      result = new Blob(byteArrays, { type: `application/${type}` });
    }
    if (result.size == 0) {
      this.spinnerService.hide();
      return this.snotify.warning('No Data', 'Warning');
    }
    if (result.type !== `application/${type}`) {
      this.spinnerService.hide();
      return this.snotify.error(result.type.toString(), 'Error');
    }
    const blob = new Blob([result]);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `${fileName}.${type}`);
    document.body.appendChild(link);
    link.click();
  }
  // exportExcel(result: Blob | string, fileName: string, type?: string): void {
  //   if (typeof result === "string") {
  //     let byteCharacters = atob(result);
  //     let byteArrays = [];
  //     for (let offset = 0; offset < byteCharacters.length; offset += 512) {
  //       let slice = byteCharacters.slice(offset, offset + 512);
  //       let byteNumbers = new Array(slice.length);
  //       for (var i = 0; i < slice.length; i++) {
  //         byteNumbers[i] = slice.charCodeAt(i);
  //       }
  //       let byteArray = new Uint8Array(byteNumbers);
  //       byteArrays.push(byteArray);
  //     }
  //     result = new Blob(byteArrays, { type: 'application/xlsx' });
  //   }
  //   if (!type) type = 'xlsx';
  //   if (result.size == 0) {
  //     this.spinnerService.hide();
  //     return this.snotify.warning('No Data', 'Warning');
  //   }
  //   if (result.type !== `application/${type}`) {
  //     this.spinnerService.hide();
  //     return this.snotify.error(result.type.toString(), 'Error');
  //   }
  //   const blob = new Blob([result]);
  //   const url = window.URL.createObjectURL(blob);
  //   const link = document.createElement('a');
  //   link.href = url;
  //   link.setAttribute('download', `${fileName}.${type}`);
  //   document.body.appendChild(link);
  //   link.click();
  // }

  /**
   * @function print
   * @description In dữ liệu ra file PDF.
   * @param {Blob} result - Dữ liệu Blob của file PDF.
   * @returns {void} Không trả về giá trị.
   */
  print(result: Blob): void {
    if (result.size == 0) {
      this.spinnerService.hide();
      return this.snotify.warning('No Data', "Warning")
    }
    const blob = new Blob([result], { type: 'application/pdf' });
    const blobUrl = URL.createObjectURL(blob);
    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    iframe.src = blobUrl;
    document.body.appendChild(iframe);
    iframe.contentWindow.print();
  }

  /**
   * @function getFileName
   * @description Tạo tên file tự động dựa theo thời gian hiện tại.
   * @param {string} name - Tên gốc của file.
   * @returns {string} Tên file đã được tạo.
   */
  getFileName(name: string): string {
    return name + '_' +
      new Date().getFullYear().toString().substring(2) +
      (new Date().getMonth() + 1).toString().padStart(2, "0") +
      new Date().getDate().toString().padStart(2, "0") +
      new Date().getHours().toString().padStart(2, "0") +
      new Date().getMinutes().toString().padStart(2, "0") +
      new Date().getSeconds().toString().padStart(2, "0");
  }

  /**
   * @function getFileNameExport
   * @description Tạo tên file tự động dựa theo thời gian hiện tại.
   * @param {string} programCode - Mã của program xuất file.
   * @param {string} typeExport - Loại xuất của file.
   * @returns {string} Tên file đã được tạo.
   */
  getFileNameExport(programCode: string, typeExport: string): string {
    const codes: CodeInformation[] = this.commonService.systemInfo.code_Information || [];
    const program = codes.find(x => x.code == programCode && x.kind == 'P');
    const program_Lang = program ? program.name.replace(/[^a-zA-Z]/g, '') : ''
    const time_Export = new Date().getFullYear().toString().substring(2) +
      (new Date().getMonth() + 1).toString().padStart(2, "0") +
      new Date().getDate().toString().padStart(2, "0") +
      new Date().getHours().toString().padStart(2, "0") +
      new Date().getMinutes().toString().padStart(2, "0") +
      new Date().getSeconds().toString().padStart(2, "0");
    return `${programCode}_${program_Lang}_${typeExport}_${time_Export}`
  }

  /**
   * @function setFunction
   * @description Lưu thông tin function của program_Code đã chọn vào session storage.
   * @param {string} program_Code - Mã program cần set.
   * @returns {void} Không trả về giá trị.
   */
  setFunction(program_Code: string): void {
    const selected_Functions = this.commonService.systemInfo.functions.filter(val => val.program_Code == program_Code);
    sessionStorage.setItem(SessionStorageConstants.SELECTED_FUNCTIONS, JSON.stringify(selected_Functions))
  }

  /**
   * @function checkFunction
   * @description Kiểm tra xem function_Code có nằm trong danh sách các function đã set hay không.
   * @param {string} function_Code - Mã function cần kiểm tra.
   * @returns {boolean} `true` nếu có, `false` nếu không.
   */
  checkFunction(function_Code: string): boolean {
    const functions: FunctionInfomation[] = JSON.parse(
      sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)
    );
    return functions?.some(val => val.function_Code == function_Code);
  }

  /**
   * @function isEmptyObject
   * @description Kiểm tra một object có rỗng hay không.
   * @param {any} object - Object cần kiểm tra.
   * @returns {boolean} true nếu rỗng, false nếu không.
   * @description rỗng nếu tất cả value là false hoặc = '0001-01-01T00:00:00'
   */
  isEmptyObject(object: any): boolean {
    return Object.keys(object).every(function (x) {
      return (!Boolean(object[x]) || object[x] === '0001-01-01T00:00:00');
    });
  }

  /**
   * @function snotifySuccessError
   * @description Hiển thị thông báo thành công hoặc lỗi.
   * @param {boolean} isSuccess - `true` để hiển thị thông báo thành công, `false` để hiển thị thông báo lỗi.
   * @param {string} message - Thông điệp cần hiển thị.
   * @param {boolean} [isTranslate=true] - `true` nếu muốn dịch thông điệp, `false` nếu không. Mặc định là `true`.
   * @returns {void} Không trả về giá trị.
   */
  snotifySuccessError(isSuccess: boolean, message: string, isTranslate: boolean = true): void {
    this.snotify[isSuccess ? 'success' : 'error'](
      isTranslate
        ? this.hasTranslation(message)
          ? this.translateService.instant(message)
          : `${this.translateService.instant('System.Message.InvalidErrorCode')} : ${message}`
        : message,
      isTranslate
        ? this.translateService.instant(`System.Caption.${isSuccess ? 'Success' : 'Error'}`)
        : isSuccess ? 'Success!' : 'Error!'
    );
  }
  /**
   * * @function snotifyConfirm
   * @description Hiển thị thông báo xác nhận cho người dùng.
   * @param {() => any} body - Tiêu đề thông báo.
   * @param {() => any} title - Nội dung thông báo.
   * @memberof FunctionUtility
   */
  snotifyConfirm(body: string, title: string, isTranslate: boolean = true, okCallback: () => any, cancelCallBack?: () => any) {
    this.snotify.confirm(isTranslate ? this.translateService.instant(body) : body,
      isTranslate ? this.translateService.instant(title) : title, (isSuccess: boolean) => {
        isSuccess ? okCallback() : cancelCallBack()
      })
  }

  /**
   * @function snotifyConfirmDefault
   * @description Hiển thị thông báo xác nhận xóa cho người dùng.
   * @param {() => any} okCallback - Hàm callback sẽ được gọi khi người dùng nhấn OK.
   * @param {() => any} [cancelCallBack] - Hàm callback sẽ được gọi khi người dùng nhấn Cancel. (optional).
   * @returns {void} Không trả về giá trị.
   * @description Sử dụng các message và caption mặc định từ translate: 'System.Message.ConfirmDelete', 'System.Action.Delete'.
   */
  snotifyConfirmDefault(okCallback: () => any, cancelCallBack?: () => any): void {
    this.snotify.confirm(this.translateService.instant('System.Message.ConfirmDelete'),
      this.translateService.instant('System.Action.Delete'), (isSuccess: boolean) => {
        isSuccess ? okCallback() : cancelCallBack()
      })
  }

  /**
   * @function setTextSelector
   * @description Bật/tắt chế độ chọn văn bản trên phần tử có id "dragscroll".
   * @param {boolean} state - `true` để tắt chế độ chọn văn bản, `false` để bật.
   * @returns {void} Không trả về giá trị.
   * @description Sử dụng class 'disable-selector' để thêm hoặc xóa chức năng chọn text.
   */
  setTextSelector(state: boolean): void {
    var element = document.getElementById("dragscroll");
    state ? element.classList.remove("disable-selector") : element.classList.add("disable-selector")
  }

  /**
   * @function generateGUID
   * @description Tạo một chuỗi GUID (Globally Unique Identifier) ngẫu nhiên.
   * @returns {string} Chuỗi GUID được tạo (ví dụ: "F47AC10B-58CC-4372-A567-0E02B2C3D479").
   * @description trả về Guid viết hoa
   */
  generateGUID(): string {
    return "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx".replace(/[x]/g, (c: any) =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    ).toUpperCase();
  }

  /**
   * @function getColspan
   * @description Tính toán giá trị colspan cho các cột trong bảng dựa trên danh sách functionCode được chọn.
   * @param {string[]} functionCodes - Mảng chứa các mã function cần tính colspan.
   * @returns {number} Giá trị colspan tính được.
   * @description Lấy các function đã chọn từ session storage và kiểm tra xem có bao nhiêu function_Code trùng với các functionCode được truyền vào.
   */
  getColspan(functionCodes: string[]): number {
    const functions: FunctionInfomation[] = JSON.parse(
      sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)
    );
    return functions?.filter(val => functionCodes.includes(val.function_Code)).length;
  }

  /**
   * @function getNgSelectAllCheckbox
   * @description Gán thuộc tính 'allGroup' cho tất cả các phần tử trong mảng KeyValuePair để hỗ trợ select all cho ng-select.
   * @param {KeyValuePair[]} items - Mảng các đối tượng KeyValuePair.
   * @returns {void} Không trả về giá trị.
   * @description Thêm thuộc tính `allGroup` vào element để đánh dấu select all
   */
  getNgSelectAllCheckbox(items: KeyValuePair[]): void {
    let allSelect = (items: KeyValuePair[]) => items.forEach(element => element['allGroup'] = 'allGroup');
    allSelect(items);
  }

  /**
   * @function getTitle
   * @description Lấy tiêu đề hiển thị cho một program dựa vào mã program và ngôn ngữ hiện tại.
   * @param {string} code - Mã program.
   * @returns {string} Tiêu đề của program.
   * @description Kiểm tra ngôn ngữ hiện tại, lấy thông tin program và program_Lang tương ứng. Trả về tiêu đề theo format: `program_Code` `program_Lang.name`
   */
  getTitle(code: string): string {
    let lang = localStorage.getItem(LocalStorageConstants.LANG)
    const codes: CodeInformation[] = this.commonService.systemInfo.code_Information || [];
    const program = codes.find(x => x.code == code && x.kind == 'P');
    const program_Lang = program ? program.translations.find(x => x.lang == lang)?.name ?? program.name : ''
    return `${code} ${program_Lang}`
  }

  /**
   * @function getRootUrl
   * @description Lấy URL gốc từ URL hiện tại.
   * @param {string} currentUrl - URL hiện tại.
   * @param {number} [repeatTime=1] - Số lần lặp lại để lấy cấp cha. Mặc định là 1 (lấy cấp cha trực tiếp).
   * @returns {string} URL gốc (ví dụ: "/dashboard" hoặc một phần của URL).
   * @description Sử dụng regex để tách url hiện tại, nếu không tìm thấy thì trả về `/dashboard`
   */
  getRootUrl(currentUrl: string, repeatTime: number = 1): string {
    const regexUrl = new RegExp(`${'.+(?=\/'.repeat(repeatTime)}${')'.repeat(repeatTime)}`)
    const validPaths: string[] = JSON.parse(localStorage.getItem(LocalStorageConstants.ROUTING))
    let result = currentUrl.match(regexUrl)
    return result == null || (result != null && !validPaths.includes(result[0])) ? '/dashboard' : result[0]
  }

  /**
   * @function hasTranslation
   * @description Kiểm tra xem một key có bản dịch hay không.
   * @param {string} key - Key cần kiểm tra.
   * @returns {boolean} `true` nếu có bản dịch, `false` nếu không.
   * @description So sánh bản dịch trả về với key, và kiểm tra có rỗng hay không
   */
  hasTranslation(key: string): boolean {
    const translation = this.translateService.instant(key);
    return translation !== key && translation !== '';
  }

  /**
   * @function hardReload
   * @description Thực hiện hard reload trang hiện tại bằng cách thêm chuỗi query "?reload=timestamp" vào URL.
   * @returns {void} Không trả về giá trị.
   * @description thêm chuỗi query `?reload=timestamp` vào url để bỏ qua cache
   */
  hardReload(): void {
    window.location.href = window.location.origin + window.location.pathname + '?reload=' + new Date().getTime();
  }

  /**
   * @function isExistedTranslation
   * @description Kiểm tra xem file dịch .json của lang tương ứng có tồn tại hay không.
   * @returns {Promise<boolean>} `true` nếu có file dịch, `false` nếu không.
   * @description Sử dụng hàm bất đồng bộ để kiểm tra sự tồn tại của file dịch .json
   */
  isExistedTranslation(lang: string): Promise<boolean> {
    return new Promise((resolve) => {
      this.translateService.currentLoader.getTranslation(lang).pipe(take(1))
        .subscribe({ next: () => resolve(true), error: () => resolve(false) });
    })
  }
}
