import {
  AfterViewInit,
  Component,
  input,
  ViewChild,
} from '@angular/core';
import { TreeviewConfig, TreeviewItem } from '@ash-mezdo/ngx-treeview';
import { ClassButton, IconButton } from '@constants/common.constants';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  data: TreeviewItem[] = [];

  IconButton = IconButton;
  classButton = ClassButton;
  config = TreeviewConfig.create({
    hasAllCheckBox: false,
    hasFilter: false,
    hasCollapseExpand: true,
    decoupleChildFromParent: false
  });

  constructor(private modalService: ModalService) { super() }

  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit( this.data )
  open(data: TreeviewItem[]): void {
    this.data = data.map(x => new TreeviewItem(x));
    this.directive.show()
  }
  close = () => this.directive.hide();
}
