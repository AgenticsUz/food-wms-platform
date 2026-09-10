import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import type { ProductionRecipe } from '../production.model';
import { ProductionService } from '../production.service';

@Component({
  selector: 'app-recipe-list',
  imports: [DecimalPipe, FormsModule, TableModule, Button, InputText, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
})
export default class RecipeListComponent implements OnInit {
  private readonly productionService = inject(ProductionService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  readonly recipes = signal<ProductionRecipe[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase();
    const list = this.recipes();
    return q
      ? list.filter((r) => r.name.toLowerCase().includes(q) || r.outputProductName.toLowerCase().includes(q))
      : list;
  });

  ngOnInit(): void {
    this.loadRecipes();
  }

  loadRecipes(): void {
    this.loading.set(true);
    this.productionService.getRecipes().subscribe({
      next: (res) => {
        this.recipes.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  createNew(): void {
    void this.router.navigate(['/production/recipes/new']);
  }

  viewRecipe(recipe: ProductionRecipe): void {
    void this.router.navigate(['/production/recipes', recipe.id]);
  }

  deleteRecipe(recipe: ProductionRecipe): void {
    this.notify.confirmDelete(`Delete recipe "${recipe.name}"?`, () => {
      this.productionService.deleteRecipe(recipe.id).subscribe({
        next: () => {
          this.notify.success('Recipe deleted');
          this.loadRecipes();
        },
        // Xato toastini qobiq chiqaradi.
        error: () => undefined,
      });
    });
  }
}
