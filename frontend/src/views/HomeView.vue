<script setup lang="ts">
import { ref, onBeforeMount, h } from 'vue';
import { NDataTable, NTag, NIcon, useDialog, type DataTableColumns } from 'naive-ui';
import type { Expense } from '@/types';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import { formatCurrency } from '../util'
import { DeleteOutlineFilled } from '@vicons/material';
import { getCategoriesByUserId } from '@/api/category';
import { deleteExpense, getExpensesByUserIdAndRange } from '@/api/expense';

const dialog = useDialog();
const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();
const options = ref<{ label: string, value: number }[]>([]);
const expenses = ref<Expense[]>([]);

sMonth.$onAction(({ after }) => {
    after(() => {
        syncExpenses();
        loadData();
    });
});

async function syncExpenses() {
    if (sUser.user?.id) {
        const startDate = `${sYear.year}-${String(sMonth.month).padStart(2, '0')}-01`;
        const endDate = new Date(sYear.year, sMonth.month, 0).toISOString().split('T')[0];
        const result = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
        if (result) expenses.value = result;
    }
}

const loadData = async () => {
    if (sUser.user?.id) {
        const cat = await getCategoriesByUserId(sUser.user.id);
        if (cat) options.value = cat.map(x => ({ label: x.name!, value: x.id! }));
    }
}

onBeforeMount(async () => {
    await syncExpenses();
    await loadData();
});

const handleDelete = (index: number) => {
    dialog.warning({
        title: 'Deletar despesa selecionada?',
        content: 'Tem Certeza?',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            const element = expenses.value[index];
            await deleteExpense(element.id!);
            await syncExpenses();
        },
    });
}

function createColumns(): DataTableColumns<Expense> {
    return [
        {
            title: 'Data',
            key: 'date',
            render(row) {
                return new Date(row.date).toLocaleDateString();
            }
        },
        {
            title: 'Descrição',
            key: 'description',
        },
        {
            title: 'Valor',
            key: 'value',
            render(row) {
                return formatCurrency(row.value);
            }
        },
        {
            title: 'Categoria',
            key: 'categoryId',
            render(row) {
                return h(NTag, {}, () => options.value.find(x => x.value == row.categoryId)?.label);
            }
        },
        {
            title: 'Deletar',
            key: 'actions',
            render(row, index) {
                return h(
                    NIcon,
                    {
                        color: "red",
                        style: { cursor: "pointer" },
                        onClick: () => handleDelete(index)
                    },
                    () => h(DeleteOutlineFilled)
                );
            }
        }
    ];
}

const columns = createColumns();
</script>

<template>
    <n-data-table :columns="columns" :data="expenses" :bordered="false" />
</template>

<style scoped></style>
