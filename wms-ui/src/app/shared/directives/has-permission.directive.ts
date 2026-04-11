import { Directive, inject, input, effect, TemplateRef, ViewContainerRef } from '@angular/core';
import { PermissionService } from '../../core/services/permission.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private templateRef = inject(TemplateRef<unknown>);
  private viewContainer = inject(ViewContainerRef);
  private permissionService = inject(PermissionService);

  hasPermission = input.required<string>();

  private hasView = false;

  constructor() {
    effect(() => {
      const code = this.hasPermission();
      const hasPerm = this.permissionService.can(code);

      if (hasPerm && !this.hasView) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.hasView = true;
      } else if (!hasPerm && this.hasView) {
        this.viewContainer.clear();
        this.hasView = false;
      }
    });
  }
}
