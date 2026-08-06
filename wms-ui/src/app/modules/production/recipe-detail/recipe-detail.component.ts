import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ProductionService } from '../../../core/services/production.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ProductionRecipe } from '../../../core/models/production.model';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [DecimalPipe, TableModule, Button, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss'
})
export default class RecipeDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productionService = inject(ProductionService);
  private notify = inject(NotificationService);

  recipe = signal<ProductionRecipe | null>(null);
  loading = signal(true);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/production/recipes']); return; }
    this.loadRecipe(id);
  }

  private loadRecipe(id: number) {
    this.productionService.getRecipe(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const r = res.data;
          if (r.recipeStages) {
            r.recipeStages = r.recipeStages.sort((a, b) => a.orderNumber - b.orderNumber);
          }
          this.recipe.set(r);
        }
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  goBack() {
    this.router.navigate(['/production/recipes']);
  }
}
