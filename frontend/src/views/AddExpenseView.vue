<script setup lang="ts">
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import NewCategory from '@/components/NewCategory.vue';
import type { ExpenseCategory, ExpenseSaveDto, UserInfo } from '@/types';
import { NButton, NDatePicker, NForm, NFormItem, NModal, NInput, NInputNumber, NTooltip, useMessage, NSelect, NIcon, type FormInst } from 'naive-ui';
import {
    AddCircleOutlineTwotone as AddIcon,
} from '@vicons/material'
import { computed, onBeforeMount, ref } from 'vue';
import { formatCurrency, parseCurrency } from '../util';
import { getCategoriesByUserId } from '@/api/category';
import { createExpense } from '@/api/expense';

const message = useMessage();

const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();

const formRef = ref<FormInst | null>(null);
const expense = ref<ExpenseSaveDto>(startExpense());

const user = ref<UserInfo>();
const categories = ref<ExpenseCategory[]>([]);
const isSaving = ref(false);
const showModal = ref(false);

const options = computed(() => categories.value.map(x => ({ label: x.name, value: x.id })))

onBeforeMount(async () => {
    loadData();
});

const loadData = async () => {
    user.value = sUser.user;
    if (user.value?.id) {
        const cat = await getCategoriesByUserId(user.value.id);
        if (cat) {
            categories.value = cat;
        }
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

sMonth.$onAction(({
    name, // name of the action
    store, // store instance, same as `someStore`
    args, // array of parameters passed to the action
    after, // hook after the action returns or resolves
    onError, // hook if the action throws or rejects
}) => {
    after(() => {
        expense.value = startExpense();
    })
});

sUser.$onAction(({
    name, // name of the action
    store, // store instance, same as `someStore`
    args, // array of parameters passed to the action
    after, // hook after the action returns or resolves
    onError, // hook if the action throws or rejects
}) => {
    after(() => {
        loadData()
    })
});

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
                    userId: user.value?.id
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
            <n-select v-model:value="expense.categoryId" filterable :options="options" />
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