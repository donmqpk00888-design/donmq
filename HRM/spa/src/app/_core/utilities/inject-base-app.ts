import { TranslateService } from '@ngx-translate/core';
import { DestroyRef, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { NgSnotifyService } from '@services/ng-snotify.service';
import { FunctionUtility } from "@utilities/function-utility";
import { NgxSpinnerService } from "ngx-spinner";
import { CommonService } from '@services/common.service';

export abstract class InjectBase {
  protected router = inject(Router);
  protected route = inject(ActivatedRoute);
  protected commonService = inject(CommonService);
  protected translateService = inject(TranslateService);
  protected spinnerService = inject(NgxSpinnerService);
  protected snotifyService = inject(NgSnotifyService);
  protected functionUtility = inject(FunctionUtility);
  protected destroyRef = inject(DestroyRef);
}
