import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type {
  ProductOption,
  ProductionStage,
  RecipeCreateDto,
  RecipeStageCreateDto,
  RecipeStageItemCreateDto,
  UnitOption,
  WarehouseOption,
} from '../production.model';
import { ProductionService } from '../production.service';

interface StageForm {
  stageId: string | null;
  orderNumber: number;
  outputProductId: string | null;
  expectedOutputQty: number | null;
  allowWarehouseOutput: boolean;
  outputWarehouseId: string | null;
  inputs: InputForm[];
}

interface InputForm {
  productId: string | null;
  quantity: number;
  unitId: string | null;
}

/** To'liq to'ldirilgan ingredient qatori (tur toraytiruvchi — `!` siz). */
function isCompleteInput(inp: InputForm): inp is InputForm & { productId: string; unitId: string } {
  return inp.productId !== null && inp.unitId !== null;
}

/**
 * ⚠️ «Faol» o'chirgichi ESKI formada bor edi, bu yerda YO'Q: F6 `CreateRecipeDto`
 * `isActive` ni qabul qilmaydi — o'chirgich jimgina e'tiborsiz qolardi.
 */
@Component({
  selector: 'app-recipe-create',
  imports: [FormsModule, Button, InputText, InputNumber, Select, ToggleSwitch, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-create.component.html',
  styleUrl: './recipe-create.component.scss',
})
export default class RecipeCreateComponent implements OnInit {
  private readonly productionService = inject(ProductionService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly recipeName = signal('');
  readonly outputProductId = signal<string | null>(null);
  readonly outputQuantity = signal<number>(0);
  readonly outputUnitId = signal<string | null>(null);

  readonly stages = signal<StageForm[]>([]);

  readonly availableStages = signal<ProductionStage[]>([]);
  readonly products = signal<ProductOption[]>([]);
  readonly units = signal<UnitOption[]>([]);
  readonly warehouses = signal<WarehouseOption[]>([]);

  ngOnInit(): void {
    this.productionService.getStages().subscribe({
      next: (res) => this.availableStages.set(res.data ?? []),
    });
    this.productionService.getProducts().subscribe({
      next: (res) => this.products.set(res.data ?? []),
    });
    this.productionService.getUnits().subscribe({
      next: (res) => this.units.set(res.data ?? []),
    });
    this.productionService.getWarehouses().subscribe({
      next: (res) => this.warehouses.set(res.data ?? []),
      error: () => undefined,
    });
  }

  addStage(): void {
    this.stages.update((list) => [
      ...list,
      {
        stageId: null,
        orderNumber: list.length + 1,
        outputProductId: null,
        expectedOutputQty: null,
        allowWarehouseOutput: false,
        outputWarehouseId: null,
        inputs: [],
      },
    ]);
  }

  removeStage(index: number): void {
    this.stages.update((list) => list.filter((_, i) => i !== index));
  }

  addInput(stageIndex: number): void {
    this.stages.update((list) =>
      list.map((stage, i) =>
        i !== stageIndex ? stage : { ...stage, inputs: [...stage.inputs, { productId: null, quantity: 0, unitId: null }] }
      )
    );
  }

  removeInput(stageIndex: number, inputIndex: number): void {
    this.stages.update((list) =>
      list.map((stage, i) =>
        i !== stageIndex ? stage : { ...stage, inputs: stage.inputs.filter((_, j) => j !== inputIndex) }
      )
    );
  }

  submit(): void {
    const outputProductId = this.outputProductId();
    const outputUnitId = this.outputUnitId();
    if (!this.recipeName().trim()) { this.notify.warn('Recipe name is required'); return; }
    if (!outputProductId) { this.notify.warn('Select an output product'); return; }
    if (this.outputQuantity() <= 0) { this.notify.warn('Output quantity must be greater than 0'); return; }
    if (!outputUnitId) { this.notify.warn('Select an output unit'); return; }
    if (this.stages().length === 0) { this.notify.warn('Add at least one stage'); return; }

    const stages: RecipeStageCreateDto[] = [];
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

      stages.push({
        stageId: stage.stageId,
        orderNumber: stage.orderNumber,
        outputProductId: stage.outputProductId,
        expectedOutputQty: stage.expectedOutputQty,
        allowWarehouseOutput: stage.allowWarehouseOutput,
        outputWarehouseId: stage.allowWarehouseOutput ? stage.outputWarehouseId : null,
        inputs: stage.inputs.filter(isCompleteInput).map(
          (inp): RecipeStageItemCreateDto => ({ productId: inp.productId, quantity: inp.quantity, unitId: inp.unitId })
        ),
      });
    }

    const dto: RecipeCreateDto = {
      name: this.recipeName().trim(),
      outputProductId,
      outputQuantity: this.outputQuantity(),
      outputUnitId,
      stages,
    };

    this.saving.set(true);
    this.productionService.createRecipe(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success('Recipe created successfully');
        void this.router.navigate(['/production/recipes']);
      },
      error: () => this.saving.set(false),
    });
  }

  goBack(): void {
    void this.router.navigate(['/production/recipes']);
  }
}
