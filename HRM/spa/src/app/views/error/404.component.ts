import { Component } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-p404',
  templateUrl: '404.component.html'
})
export class P404Component extends InjectBase {
  tempUrl: string = ''
  iconButton = IconButton;
  constructor() {
    super()
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url))
  }
  back = () => this.router.navigate([this.tempUrl]);
}
