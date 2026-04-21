<script setup lang="ts">
import {
  NUpload, NUploadDragger, NText, NP, NIcon, NDataTable, NInput, NSelect,
  NButton, NFormItem, NTag, NSteps, NStep, NCheckbox, NCheckboxGroup, NCard,
  NSpace, NStatistic, NDivider, NResult, NAlert, NTooltip,
  type DataTableColumns, useMessage
} from 'naive-ui';
import Papa from 'papaparse';
import {
  ArchiveRound as ArchiveIcon,
  WarningAmberFilled as WarningIcon,
  DoneAllFilled as DoneIcon,
  ArrowForwardRound as ArrowForwardIcon,
  ArrowBackRound as ArrowBackIcon,
} from '@vicons/material';
import { ref, h, computed } from 'vue';
import type { Expense } from '@/types';
import { useUserStore } from '@/stores/user';
import { getCategoriesByUserId } from '@/api/category';
import { createExpenses, getExpensesByUserIdAndRange } from '@/api/expense';
import { extractTextFromPdf } from '@/services/pdfTextExtractor';
import { detectParser, type CardGroup } from '@/services/invoiceParsers';

type WizardStep = 'upload' | 'cards' | 'configure' | 'review' | 'done';

const message = useMessage();
const sUser = useUserStore();

const categories = ref<{ label: string; value: number }[]>([]);
const savedData = ref<Expense[]>([]);

const currentStep = ref<WizardStep>('upload');
const stepNumber = computed(() => {
  const map: Record<WizardStep, number> = { upload: 1, cards: 2, configure: 3, review: 4, done: 5 };
  return map[currentStep.value];
});

const detectedBank = ref('');
const cardGroups = ref<CardGroup[]>([]);
const selectedCardLabels = ref<string[]>([]);
const selectedCards = computed(() =>
  cardGroups.value.filter(c => selectedCardLabels.value.includes(c.cardLabel))
);

const currentCardIndex = ref(0);
const currentCard = computed(() => selectedCards.value[currentCardIndex.value]);

const bulkCategoryId = ref<number | null>(null);
const skipBulkCategory = ref(false);

const cardExpenses = ref<Expense[]>([]);
const totalImported = ref(0);
const totalCards = ref(0);

const isCsvMode = ref(false);

const loadData = async () => {
  if (sUser.user?.id) {
    const cat = await getCategoriesByUserId(sUser.user.id);
    if (cat) categories.value = cat.map(x => ({ label: x.name!, value: x.id! }));
  }
};

const loadSavedData = async (expenses: { date: string }[]) => {
  if (!expenses.length || !sUser.user?.id) return;
  const dates = expenses.map(e => e.date).sort();
  const startDate = dates[0];
  const endDate = dates[dates.length - 1];
  const result = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
  if (result) savedData.value = result;
};

const handleDelete = (index: number) => {
  cardExpenses.value.splice(index, 1);
};

const onRemoveCategory = (indexToRemove: number) => {
  const element = cardExpenses.value[indexToRemove];
  cardExpenses.value[indexToRemove] = {
    date: element.date,
    description: element.description,
    value: element.value,
  };
};

function createColumns(): DataTableColumns<Expense> {
  return [
    {
      title: 'Data',
      key: 'date',
      width: 120,
      render(row, index) {
        return h(NInput, {
          value: row.date,
          size: 'small',
          onUpdateValue(v: string) { cardExpenses.value[index].date = v; },
        });
      },
    },
    {
      title: 'Descrição',
      key: 'description',
      render(row, index) {
        return h(NInput, {
          value: row.description,
          size: 'small',
          onUpdateValue(v: string) { cardExpenses.value[index].description = v; },
        });
      },
    },
    {
      title: 'Valor',
      key: 'value',
      width: 100,
      render(row, index) {
        return h(NInput, {
          value: row.value?.toString() ?? '',
          size: 'small',
          onUpdateValue(v: string) { cardExpenses.value[index].value = parseFloat(v) || null; },
        });
      },
    },
    {
      title: 'Categoria',
      key: 'categoryId',
      width: 150,
      render(row, index) {
        if (row.categoryId) {
          return h(NTag, { closable: true, size: 'small', onClose: () => onRemoveCategory(index) },
            () => categories.value.find(x => x.value == row.categoryId)?.label);
        }
        return h(NSelect, {
          value: row.categoryId,
          options: categories.value,
          size: 'small',
          onUpdateValue(v: number) { cardExpenses.value[index].categoryId = v; },
        });
      },
    },

    {
      title: '',
      key: 'conflicts',
      width: 40,
      render(row) {
        const isDuplicate = savedData.value.some(x =>
          x.date?.substring(0, 10) === row.date?.substring(0, 10)
          && Number(x.value) === Number(row.value)
        );
        if (isDuplicate) {
          return h(NTooltip, null, {
            default: () => 'Essa despesa pode já estar cadastrada',
            trigger: () => h(NIcon, { color: '#e88080', size: 18 }, () => h(WarningIcon)),
          });
        }
        return h(NTooltip, null, {
          default: () => 'Despesa única',
          trigger: () => h(NIcon, { color: '#63e2b7', size: 18 }, () => h(DoneIcon)),
        });
      },
    },
    {
      title: '',
      key: 'actions',
      width: 70,
      render(_row, index) {
        return h(NButton, { size: 'tiny', tertiary: true, type: 'error', onClick: () => handleDelete(index) }, () => 'Remover');
      },
    },
  ];
}

const columns = createColumns();

const handleUpload = async (fileEvent: any) => {
  const file = fileEvent.file.file as File;
  const extension = file.name.split('.').pop()?.toLowerCase();

  await loadData();

  if (extension === 'csv') {
    isCsvMode.value = true;
    Papa.parse(file, { complete: onCompleteCsvParse, error: onErrorCsvParse });
    return;
  }

  if (extension === 'pdf') {
    isCsvMode.value = false;
    try {
      const pages = await extractTextFromPdf(file);
      const parser = detectParser(pages);

      if (!parser) {
        message.error('Formato de fatura não reconhecido. Bancos suportados: Nubank, Santander.');
        return;
      }

      detectedBank.value = parser.bankName;
      cardGroups.value = parser.parse(pages);

      if (cardGroups.value.length === 0) {
        message.error('Nenhuma transação encontrada no PDF.');
        return;
      }

      selectedCardLabels.value = cardGroups.value.map(c => c.cardLabel);
      currentStep.value = 'cards';
    } catch {
      message.error('Erro ao processar o PDF.');
    }
    return;
  }

  message.error('Formato não suportado. Use CSV ou PDF.');
};

const onCompleteCsvParse = async (result: any) => {
  const resultFiltered: Expense[] = [];
  let startDate: string | null = null;
  let endDate: string | null = null;

  for (let index = 1; index < result.data.length; index++) {
    const element = result.data[index];
    if (element[2] > 0) {
      startDate = !startDate ? element[0] : new Date(startDate) > new Date(element[0]) ? element[0] : startDate;
      endDate = !endDate ? element[0] : new Date(endDate) < new Date(element[0]) ? element[0] : endDate;
      resultFiltered.push({ date: element[0], description: element[1], value: element[2] });
    }
  }

  if (startDate && endDate) {
    const savedResult = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
    if (savedResult) savedData.value = savedResult;
  }

  cardExpenses.value = resultFiltered;
  currentStep.value = 'review';
};

const onErrorCsvParse = () => {
  message.error('Erro ao ler csv.');
};

const goToConfigureCard = () => {
  if (selectedCards.value.length === 0) {
    message.warning('Selecione ao menos um cartão.');
    return;
  }
  currentCardIndex.value = 0;
  resetBulkOptions();
  currentStep.value = 'configure';
};

const resetBulkOptions = () => {
  bulkCategoryId.value = null;
  skipBulkCategory.value = false;
};

const goToReview = async () => {
  const card = currentCard.value;
  if (!card) return;

  const expenses: Expense[] = card.expenses.map(e => ({
    date: e.date,
    description: e.description,
    value: e.value,
    categoryId: (!skipBulkCategory.value && bulkCategoryId.value) ? bulkCategoryId.value : undefined,
  }));

  cardExpenses.value = expenses;
  savedData.value = [];
  await loadSavedData(expenses);
  currentStep.value = 'review';
};

const isDataValid = () => {
  return !cardExpenses.value.some(x => !x.date || !x.description || !x.value || !x.categoryId);
};

const saveCurrentCard = async (): Promise<boolean> => {
  if (!isDataValid()) {
    message.error('Todos campos devem estar preenchidos (data, descrição, valor e categoria).');
    return false;
  }
  try {
    cardExpenses.value.forEach(x => x.userId = sUser.user.id);
    await createExpenses(cardExpenses.value);
    totalImported.value += cardExpenses.value.length;
    return true;
  } catch {
    message.error('Erro ao salvar. Verifique todos os campos.');
    return false;
  }
};

const onSaveAndNext = async () => {
  const saved = await saveCurrentCard();
  if (!saved) return;

  message.success(`Cartão ${currentCard.value.cardLabel} salvo!`);

  if (currentCardIndex.value < selectedCards.value.length - 1) {
    currentCardIndex.value++;
    resetBulkOptions();
    currentStep.value = 'configure';
  } else {
    totalCards.value = selectedCards.value.length;
    currentStep.value = 'done';
  }
};

const onSaveCsv = async () => {
  if (!isDataValid()) {
    message.error('Todos campos devem estar preenchidos.');
    return;
  }
  try {
    cardExpenses.value.forEach(x => x.userId = sUser.user.id);
    await createExpenses(cardExpenses.value);
    message.success('Salvos');
    totalImported.value = cardExpenses.value.length;
    totalCards.value = 0;
    currentStep.value = 'done';
  } catch {
    message.error('Erro ao salvar. Verifique todos os campos.');
  }
};

const resetWizard = () => {
  currentStep.value = 'upload';
  cardGroups.value = [];
  selectedCardLabels.value = [];
  currentCardIndex.value = 0;
  cardExpenses.value = [];
  savedData.value = [];
  totalImported.value = 0;
  totalCards.value = 0;
  detectedBank.value = '';
  isCsvMode.value = false;
  resetBulkOptions();
};

const formatCurrency = (value: number) => {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
};
</script>

<template>
  <div class="import-container">
    <h1>Importar fatura</h1>

    <n-steps :current="stepNumber" class="wizard-steps" size="small">
      <n-step title="Upload" />
      <n-step title="Cartões" />
      <n-step title="Configurar" />
      <n-step title="Revisar" />
      <n-step title="Concluído" />
    </n-steps>

    <!-- Step 1: Upload -->
    <div v-if="currentStep === 'upload'" class="step-content">
      <n-upload multiple directory-dnd :max="1" accept=".csv,.pdf" @change="handleUpload">
        <n-upload-dragger>
          <div style="margin-bottom: 12px">
            <n-icon size="48" :depth="3">
              <ArchiveIcon />
            </n-icon>
          </div>
          <n-text style="font-size: 16px">
            Clique ou arraste um arquivo
          </n-text>
          <n-p depth="3" style="margin: 8px 0 0 0">
            Formatos aceitos: CSV ou PDF
            <br />
            PDF: fatura do Nubank ou Santander
            <br />
            CSV: fatura da Nubank com 3 colunas (date, title, amount)
          </n-p>
        </n-upload-dragger>
      </n-upload>
    </div>

    <!-- Step 2: Card overview -->
    <div v-if="currentStep === 'cards'" class="step-content">
      <n-alert type="info" :bordered="false" style="margin-bottom: 16px">
        Fatura <strong>{{ detectedBank }}</strong> — {{ cardGroups.length }} cartões encontrados.
        Selecione quais deseja importar.
      </n-alert>

      <n-checkbox-group v-model:value="selectedCardLabels">
        <n-space vertical :size="12">
          <n-card
            v-for="card in cardGroups"
            :key="card.cardLabel"
            size="small"
            hoverable
            class="card-item"
          >
            <div class="card-row">
              <n-checkbox :value="card.cardLabel" />
              <div class="card-info">
                <div class="card-label">{{ card.cardLabel }}</div>
                <div class="card-holder">{{ card.holderName }}</div>
              </div>
              <div class="card-stats">
                <span class="card-count">{{ card.expenses.length }} transações</span>
                <span class="card-total">{{ formatCurrency(card.totalValue) }}</span>
              </div>
            </div>
          </n-card>
        </n-space>
      </n-checkbox-group>

      <div class="step-actions">
        <n-button @click="resetWizard">
          <template #icon><n-icon><ArrowBackIcon /></n-icon></template>
          Voltar
        </n-button>
        <n-button type="primary" @click="goToConfigureCard">
          Importar selecionados
          <template #icon><n-icon><ArrowForwardIcon /></n-icon></template>
        </n-button>
      </div>
    </div>

    <!-- Step 3: Configure current card -->
    <div v-if="currentStep === 'configure'" class="step-content">
      <n-card class="configure-card">
        <template #header>
          <div class="configure-header">
            <span>Importando cartão <strong>{{ currentCard?.cardLabel }}</strong></span>
            <n-tag type="info" size="small">
              {{ currentCardIndex + 1 }} de {{ selectedCards.length }}
            </n-tag>
          </div>
        </template>

        <div class="configure-holder">
          Titular: <strong>{{ currentCard?.holderName }}</strong>
          — {{ currentCard?.expenses.length }} transações
          — {{ formatCurrency(currentCard?.totalValue ?? 0) }}
        </div>

        <n-divider />

        <n-form-item label="Categoria padrão para todas as despesas deste cartão">
          <n-select
            v-model:value="bulkCategoryId"
            :options="categories"
            :disabled="skipBulkCategory"
            placeholder="Selecionar categoria..."
            clearable
            style="max-width: 300px"
          />
        </n-form-item>

        <n-checkbox v-model:checked="skipBulkCategory" style="margin-bottom: 16px">
          Não aplicar — definir por despesa
        </n-checkbox>


      </n-card>

      <div class="step-actions">
        <n-button @click="currentStep = 'cards'">
          <template #icon><n-icon><ArrowBackIcon /></n-icon></template>
          Voltar
        </n-button>
        <n-button type="primary" @click="goToReview">
          Continuar para revisão
          <template #icon><n-icon><ArrowForwardIcon /></n-icon></template>
        </n-button>
      </div>
    </div>

    <!-- Step 4: Review expenses -->
    <div v-if="currentStep === 'review'" class="step-content">
      <div v-if="!isCsvMode" class="review-header">
        <n-alert type="info" :bordered="false">
          Revisando <strong>{{ currentCard?.cardLabel }}</strong>
          ({{ currentCard?.holderName }})
          — {{ cardExpenses.length }} despesas
        </n-alert>
      </div>

      <n-data-table
        :columns="columns"
        :data="cardExpenses"
        :bordered="false"
        size="small"
        class="expenses-table"
      />

      <div class="step-actions">
        <n-button @click="isCsvMode ? resetWizard() : (currentStep = 'configure')">
          <template #icon><n-icon><ArrowBackIcon /></n-icon></template>
          Voltar
        </n-button>
        <n-button
          v-if="isCsvMode"
          type="primary"
          @click="onSaveCsv"
        >
          Salvar
        </n-button>
        <n-button
          v-else
          type="primary"
          @click="onSaveAndNext"
        >
          {{
            currentCardIndex < selectedCards.length - 1
              ? 'Salvar e próximo cartão'
              : 'Salvar e concluir'
          }}
          <template #icon><n-icon><ArrowForwardIcon /></n-icon></template>
        </n-button>
      </div>
    </div>

    <!-- Step 5: Done -->
    <div v-if="currentStep === 'done'" class="step-content">
      <n-result status="success" title="Importação concluída!" class="done-result">
        <template #footer>
          <n-space justify="center" :size="24">
            <n-statistic label="Despesas importadas" :value="totalImported" />
            <n-statistic v-if="totalCards > 0" label="Cartões processados" :value="totalCards" />
          </n-space>
          <div style="margin-top: 24px">
            <n-button type="primary" @click="resetWizard">Importar outra fatura</n-button>
          </div>
        </template>
      </n-result>
    </div>
  </div>
</template>

<style scoped>
.import-container {
  padding: 0 16px;
}

.wizard-steps {
  margin: 24px 0;
}

.step-content {
  margin-top: 16px;
}

.step-actions {
  display: flex;
  justify-content: space-between;
  margin-top: 24px;
  padding-bottom: 24px;
}

.card-item {
  cursor: pointer;
}

.card-row {
  display: flex;
  align-items: center;
  gap: 16px;
}

.card-info {
  flex: 1;
}

.card-label {
  font-weight: 600;
  font-size: 15px;
  font-family: monospace;
}

.card-holder {
  color: var(--n-text-color-3, #999);
  font-size: 13px;
  margin-top: 2px;
}

.card-stats {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
}

.card-count {
  font-size: 13px;
  color: var(--n-text-color-3, #999);
}

.card-total {
  font-weight: 600;
  font-size: 15px;
  color: var(--n-text-color, #fff);
}

.configure-card {
  margin-bottom: 8px;
}

.configure-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.configure-holder {
  color: var(--n-text-color-3, #999);
  font-size: 14px;
}

.review-header {
  margin-bottom: 16px;
}

.expenses-table {
  margin-top: 8px;
}

.done-result {
  margin-top: 32px;
}
</style>
