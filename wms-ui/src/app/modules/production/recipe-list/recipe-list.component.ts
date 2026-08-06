import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ProductionService } from '../../../core/services/production.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ProductionRecipe } from '../../../core/models/production.model';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [DecimalPipe, FormsModule, TableModule, Button, InputText, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss'
})
export default class RecipeListComponent implements OnInit {
  private productionService = inject(ProductionService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  recipes = signal<ProductionRecipe[]>([]);
  loading = signal(true);
  search = signal('');
  filtered = signal<ProductionRecipe[]>([]);

  ngOnInit() {
    this.loadRecipes();
  }

  loadRecipes() {
    this.loading.set(true);
    this.productionService.getRecipes().subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.recipes.set(list);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  applyFilter() {
    let list = this.recipes();
    const q = this.search().toLowerCase();
    if (q) {
      list = list.filter(r =>
        r.name.toLowerCase().includes(q) ||
        (r.outputProductName && r.outputProductName.toLowerCase().includes(q))
      );
    }
    this.filtered.set(list);
  }

  onSearchChange(value: string) {
    this.search.set(value);
    this.applyFilter();
  }

  createNew() {
    this.router.navigate(['/production/recipes/new']);
  }

  viewRecipe(recipe: ProductionRecipe) {
    this.router.navigate(['/production/recipes', recipe.id]);
  }

  deleteRecipe(recipe: ProductionRecipe) {
    this.notify.confirmDelete(`Delete recipe "${recipe.name}"?`, () => {
      this.productionService.deleteRecipe(recipe.id).subscribe({
        next: () => { this.notify.success('Recipe deleted'); this.loadRecipes(); },
        error: () => this.notify.error('Failed to delete recipe')
      });
    });
  }
}
