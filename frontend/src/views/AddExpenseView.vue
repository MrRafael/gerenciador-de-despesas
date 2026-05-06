<script setup lang="ts">
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import NewCategory from '@/components/NewCategory.vue';
import type { ExpenseCategory, ExpenseSaveDto, GroupSplitConfig, MemberGroup, UserInfo } from '@/types';
import { NButton, NDatePicker, NForm, NFormItem, NModal, NInput, NInputNumber, NTooltip, useMessage, NSelect, NIcon, type FormInst } from 'naive-ui';
import {
    AddCircleOutlineTwotone as AddIcon,
} from '@vicons/material'
import { computed, onBeforeMount, ref } from 'vue';
import { formatCurrency, parseCurrency } from '../util';
import { getCategoriesByUserId } from '@/api/category';
import { createExpense } from '@/api/expense';
import { getMyGroups } from '@/api/groups';
import { getSplitConfigs } from '@/api/splitConfig';

const message = useMessage();

const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();

const formRef = ref<FormInst | null>(null);
const expense = ref<ExpenseSaveDto>(startExpense());

const user = ref<UserInfo>();
const categories = ref<ExpenseCategory[]>([]);
const groups = ref<MemberGroup[]>([]);
const groupSplitConfigs = ref<GroupSplitConfig[]>([]);
const isSaving = ref(false);
const showModal = ref(false);

const categoryOptions = computed(() => categories.value.map(x => ({ label: x.name, value: x.id })));

const groupOptions = computed(() => [
    { label: 'Nenhum (despesa pessoal)', value: null },
    ...groups.value.map(g => ({ label: g.name, value: g.id }))
]);

const splitConfigOptions = computed(() =>
    groupSplitConfigs.value.map(c => ({
        label: splitTypeLabel(c),
        value: c.id,
    }))
);

const showSplitSelector = computed(() => expense.value.groupId && groupSplitConfigs.value.length >= 1);

function splitTypeLabel(config: GroupSplitConfig): string {
    const labels: Record<number, string> = { 0: 'Divisão igual', 1: 'Proporcional ao salário', 2: 'Manual' };
    const base = labels[config.splitType] ?? 'Desconhecido';
    return config.isDefault ? `${base} (padrão)` : base;
}

onBeforeMount(async () => {
    loadData();
});

const loadData = async () => {
    user.value = sUser.user;
    if (user.value?.id) {
        const [cat, grp] = await Promise.all([
            getCategoriesByUserId(user.value.id),
            getMyGroups(user.value.id),
        ]);
        if (cat) categories.value = cat;
        if (grp) groups.value = grp;
    }
}

function startExpense(): ExpenseSaveDto {
    const month = sMonth.month;
    const year = sYear.year;

    let date = new Date();

    if (month !== (date.getMonth() + 1) || year !== date.getFullYear()) {
        date = new Date(year, month - 1, 1);
    }
    return {
        date: date.getTime(),
        value: null,
        description: '',
        categoryId: null,
        groupId: null,
        groupSplitConfigId: null,
    }
}

const rules = {
    date: {
        required: true,
        message: 'Campo obrigatório',
    },
    description: {
        required: true,
        message: 'Campo obrigatório',
    },
    value: {
        required: true,
        message: 'Campo obrigatório',
    },
    categoryId: {
        required: true,
        message: 'Campo obrigatório',
    },
}

sMonth.$onAction(({ after }) => {
    after(() => {
        expense.value = startExpense();
    })
});

sUser.$onAction(({ after }) => {
    after(() => {
        loadData()
    })
});

const onGroupChange = async (groupId: number | null) => {
    expense.value.groupSplitConfigId = undefined;
    groupSplitConfigs.value = [];

    if (!groupId) return;

    try {
        const configs = await getSplitConfigs(groupId);
        groupSplitConfigs.value = configs;

        if (configs.length === 1) {
            expense.value.groupSplitConfigId = configs[0].id;
        } else if (configs.length > 1) {
            const defaultConfig = configs.find(c => c.isDefault) ?? configs[0];
            expense.value.groupSplitConfigId = defaultConfig.id;
        }
    } catch { }
}

const onNewCategoryClosed = () => {
    showModal.value = false;
    loadData();
}

const save = async () => {
    isSaving.value = true;
    formRef.value?.validate(async (errors) => {
        if (!errors) {
            try {
                await createExpense({
                    description: expense.value.description,
                    value: expense.value.value,
                    date: new Date(expense.value.date).toISOString().substring(0, 10),
                    categoryId: expense.value.categoryId,
                    userId: user.value?.id,
                    groupId: expense.value.groupId || undefined,
                    groupSplitConfigId: expense.value.groupId ? expense.value.groupSplitConfigId : undefined,
                })

                message.success('Despesa adicionada com sucesso');
                expense.value = startExpense();
                groupSplitConfigs.value = [];
            } catch (err: unknown) {
                const status = (err as { response?: { status?: number } })?.response?.status;
                message.error(status === 409 ? 'Este mês já foi fechado e não pode receber novas despesas' : 'Erro ao adicionar despesa');
            }
        }

    })
    isSaving.value = false;
}
</script>

<template>
    <div class="add-expense-container">
        <h2 class="page-title">Adicionar despesa</h2>
        <n-form ref="formRef" :model="expense" :rules="rules">
            <n-form-item label="Data" path="date">
                <n-date-picker class="form-input" v-model:value="expense.date" format="dd/MM/yyyy" />
            </n-form-item>
            <n-form-item label="Valor" path="value">
                <n-input-number class="form-input" :parse="parseCurrency" :format="formatCurrency"
                    placeholder="Valor da Despesa" v-model:value="expense.value" />
            </n-form-item>
            <n-form-item label="Descrição" path="description">
                <n-input v-model:value="expense.description" placeholder="Descrição" />
            </n-form-item>
            <n-form-item label="Categoria" path="categoryId">
                <div class="category-row">
                    <n-select v-model:value="expense.categoryId" filterable :options="categoryOptions" class="category-select" />
                    <n-tooltip trigger="hover">
                        <template #trigger>
                            <n-button type="tertiary" @click="showModal = true">
                                <n-icon>
                                    <AddIcon />
                                </n-icon>
                            </n-button>
                        </template>
                        Adicionar Categoria
                    </n-tooltip>
                </div>
            </n-form-item>
            <n-form-item v-if="groups.length > 0" label="Grupo" path="groupId">
                <n-select
                    v-model:value="expense.groupId"
                    :options="groupOptions"
                    @update:value="onGroupChange"
                />
            </n-form-item>
            <n-form-item v-if="showSplitSelector" label="Como dividir?" path="groupSplitConfigId">
                <n-select v-model:value="expense.groupSplitConfigId" :options="splitConfigOptions" />
            </n-form-item>
            <n-form-item>
                <n-button class="form-input" type="primary" @click="save" :disabled="isSaving" :loading="isSaving">Salvar</n-button>
            </n-form-item>
        </n-form>
    </div>
    <n-modal v-model:show="showModal">
        <new-category @closed="onNewCategoryClosed" />
    </n-modal>
</template>

<style scoped>
.form-input {
    width: 100%;
}

.add-expense-container {
    max-width: 560px;
}

.page-title {
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: 1.5rem;
}

.category-row {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
}

.category-select {
    flex: 1;
}
</style>
