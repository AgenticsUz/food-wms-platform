import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProductionService } from '../../../core/services/production.service';
import { ProductService } from '../../../core/services/product.service';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import {
  RecipeCreateDto,
  RecipeStageCreateDto,
  RecipeStageItemCreateDto,
  ProductionStage
} from '../../../core/models/production.model';
import { Product, Unit } from '../../../core/models/product.model';
import { Warehouse } from '../../../core/models/warehouse.model';

interface StageForm {
  stageId: number | null;
  orderNumber: number;
  outputProductId: number | null;
  expectedOutputQty: number | null;
  allowWarehouseOutput: boolean;
  outputWarehouseId: number | null;
  inputs: InputForm[];
}

interface InputForm {
  productId: number | null;
  quantity: number;
  unitId: number | null;
}

@Component({
  selector: 'app-recipe-create',
  standalone: true,
  imports: [FormsModule, Button, InputText, InputNumber, Select, ToggleSwitch, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-create.component.html',
  styleUrl: './recipe-create.component.scss'
})
export default class RecipeCreateComponent implements OnInit {
  private productionService = inject(ProductionService);
  private productService = inject(ProductService);
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  saving = signal(false);
  recipeName = signal('');
  outputProductId = signal<number | null>(null);
  outputQuantity = signal<number>(0);
  outputUnitId = signal<number | null>(null);
  isActive = signal(true);

  stages = signal<StageForm[]>([]);

  availableStages = signal<ProductionStage[]>([]);
  products = signal<Product[]>([]);
  units = signal<Unit[]>([]);
  warehouses = signal<Warehouse[]>([]);

  ngOnInit() {
    this.loadStages();
    this.loadProducts();
    this.loadUnits();
    this.loadWarehouses();
  }

  private loadStages() {
    this.productionService.getStages().subscribe({
      next: (res) => { if (res.success && res.data) this.availableStages.set(res.data); }
    });
  }

  private loadProducts() {
    this.productService.getProducts().subscribe({
      next: (res) => { if (res.success && res.data) this.products.set(res.data); }
    });
  }

  private loadUnits() {
    this.productService.getUnits().subscribe({
      next: (res) => { if (res.success && res.data) this.units.set(res.data); }
    });
  }

  private loadWarehouses() {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => { if (res.success && res.data) this.warehouses.set(res.data); }
    });
  }

  addStage() {
    this.stages.update(list => [...list, {
      stageId: null,
      orderNumber: list.length + 1,
      outputProductId: null,
      expectedOutputQty: null,
      allowWarehouseOutput: false,
      outputWarehouseId: null,
      inputs: []
    }]);
  }

  removeStage(index: number) {
    this.stages.update(list => list.filter((_, i) => i !== index));
  }

  addInput(stageIndex: number) {
    this.stages.update(list => list.map((stage, i) => {
      if (i !== stageIndex) return stage;
      return { ...stage, inputs: [...stage.inputs, { productId: null, quantity: 0, unitId: null }] };
    }));
  }

  removeInput(stageIndex: number, inputIndex: number) {
    this.stages.update(list => list.map((stage, i) => {
      if (i !== stageIndex) return stage;
      return { ...stage, inputs: stage.inputs.filter((_, j) => j !== inputIndex) };
    }));
  }

  submit() {
    if (!this.recipeName().trim()) { this.notify.warn('Recipe name is required'); return; }
    if (!this.outputProductId()) { this.notify.warn('Select an output product'); return; }
    if (this.outputQuantity() <= 0) { this.notify.warn('Output quantity must be greater than 0'); return; }
    if (!this.outputUnitId()) { this.notify.warn('Select an output unit'); return; }
    if (this.stages().length === 0) { this.notify.warn('Add at least one stage'); return; }

    for (let i = 0; i < this.stages().length; i++) {
      const stage = this.stages()[i];
      if (!stage.stageId) { this.notify.warn(`Select a production stage for Stage ${i + 1}`); return; }

      // To'liq bo'lmagan ingredient qatorlari jimgina tashlanmasin — foydalanuvchini ogohlantiramiz
      for (let j = 0; j < stage.inputs.length; j++) {
        const inp = stage.inputs[j];
        const anySet = !!inp.productId || !!inp.unitId || inp.quantity > 0;
        if (anySet && (!inp.productId || !inp.unitId || inp.quantity <= 0)) {
          this.notify.warn(`Stage ${i + 1}: complete or remove the ingredient row ${j + 1}`);
          return;
        }
      }
    }

    this.saving.set(true);

    const dto: RecipeCreateDto = {
      name: this.recipeName().trim(),
      outputProductId: this.outputProductId()!,
      outputQuantity: this.outputQuantity(),
      outputUnitId: this.outputUnitId()!,
      isActive: this.isActive(),
      stages: this.stages().map((stage): RecipeStageCreateDto => ({
        stageId: stage.stageId!,
        orderNumber: stage.orderNumber,
        outputProductId: stage.outputProductId,
        expectedOutputQty: stage.expectedOutputQty,
        allowWarehouseOutput: stage.allowWarehouseOutput,
        outputWarehouseId: stage.allowWarehouseOutput ? stage.outputWarehouseId : null,
        inputs: stage.inputs
          .filter(inp => inp.productId && inp.unitId)
          .map((inp): RecipeStageItemCreateDto => ({
            productId: inp.productId!,
            quantity: inp.quantity,
            unitId: inp.unitId!
          }))
      }))
    };

    this.productionService.createRecipe(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success('Recipe created successfully');
        this.router.navigate(['/production/recipes']);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate(['/production/recipes']);
  }
}
