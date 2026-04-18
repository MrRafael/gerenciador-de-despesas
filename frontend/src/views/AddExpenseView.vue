<script setup lang="ts">
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import NewCategory from '@/components/NewCategory.vue';
import { SplitType, type ExpenseCategory, type ExpenseSaveDto, type MemberGroup, type UserInfo } from '@/types';
import { NButton, NDatePicker, NForm, NFormItem, NModal, NInput, NInputNumber, NTooltip, useMessage, NSelect, NIcon, type FormInst } from 'naive-ui';
import {
    AddCircleOutlineTwotone as AddIcon,
} from '@vicons/material'
import { computed, onBeforeMount, ref } from 'vue';
import { formatCurrency, parseCurrency } from '../util';
import { getCategoriesByUserId } from '@/api/category';
import { createExpense } from '@/api/expense';
import { getMyGroups } from '@/api/groups';

const message = useMessage();

const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();

const formRef = ref<FormInst | null>(null);
const expense = ref<ExpenseSaveDto>(startExpense());

const user = ref<UserInfo>();
const categories = ref<ExpenseCategory[]>([]);
const groups = ref<MemberGroup[]>([]);
const isSaving = ref(false);
const showModal = ref(false);

const categoryOptions = computed(() => categories.value.map(x => ({ label: x.name, value: x.id })));

const groupOptions = computed(() => [
    { label: 'Nenhum (despesa pessoal)', value: null },
    ...groups.value.map(g => ({ label: g.name, value: g.id }))
]);

const splitTypeOptions = [
    { label: 'Divisão igual', value: SplitType.Equal },
    { label: 'Proporcional ao salário', value: SplitType.Proportional },
    { label: 'Manual', value: SplitType.Manual },
];

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
        groupId: undefined,
        splitType: undefined,
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

const onGroupChange = (groupId: number | null) => {
    if (!groupId) {
        expense.value.splitType = undefined;
    } else if (expense.value.splitType === undefined) {
        expense.value.splitType = SplitType.Equal;
    }
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
                    splitType: expense.value.groupId ? expense.value.splitType : undefined,
                })

                message.success('Despesa adicionada com sucesso');
                expense.value = startExpense();
            } catch {
                message.error('Erro ao adicionar despesa');
            }
        }

    })
    isSaving.value = false;
}
</script>

<template>
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
            <n-select v-model:value="expense.categoryId" filterable :options="categoryOptions" />
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
        </n-form-item>
        <n-form-item v-if="groups.length > 0" label="Grupo" path="groupId">
            <n-select
                v-model:value="expense.groupId"
                :options="groupOptions"
                @update:value="onGroupChange"
            />
        </n-form-item>
        <n-form-item v-if="expense.groupId" label="Tipo de divisão" path="splitType">
            <n-select v-model:value="expense.splitType" :options="splitTypeOptions" />
        </n-form-item>
    </n-form>

    <n-form-item>
        <n-button class="form-input" @click="save" :disabled="isSaving">Salvar</n-button>
    </n-form-item>
    <n-modal v-model:show="showModal">
        <new-category @closed="onNewCategoryClosed" />
    </n-modal>
</template>

<style scoped>
.form-input {
    width: 100%;
}
</style>