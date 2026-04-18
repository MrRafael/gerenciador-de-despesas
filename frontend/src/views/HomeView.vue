<script setup lang="ts">
import { ref, onBeforeMount, h } from 'vue';
import { NDataTable, NTag, NIcon, NSelect, useDialog, useMessage, type DataTableColumns } from 'naive-ui';
import type { Expense, MemberGroup } from '@/types';
import { SplitType } from '@/types';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import { formatCurrency } from '../util'
import { DeleteOutlineFilled } from '@vicons/material';
import { getCategoriesByUserId } from '@/api/category';
import { deleteExpense, getExpensesByUserIdAndRange, updateExpenseGroup } from '@/api/expense';
import { getMyGroups } from '@/api/groups';

const dialog = useDialog();
const message = useMessage();
const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();
const options = ref<{ label: string, value: number }[]>([]);
const groups = ref<MemberGroup[]>([]);
const expenses = ref<Expense[]>([]);

const groupOptions = () => [
    { label: 'Pessoal (sem grupo)', value: null as number | null },
    ...groups.value.map(g => ({ label: g.name, value: g.id as number | null }))
];

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
        const [cat, grp] = await Promise.all([
            getCategoriesByUserId(sUser.user.id),
            getMyGroups(sUser.user.id),
        ]);
        if (cat) options.value = cat.map(x => ({ label: x.name!, value: x.id! }));
        if (grp) groups.value = grp;
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

const handleGroupChange = async (expense: Expense, index: number, groupId: number | null) => {
    try {
        const splitType = groupId != null ? SplitType.Equal : undefined;
        const updated = await updateExpenseGroup(expense.id!, groupId, splitType);
        expenses.value[index] = { ...expenses.value[index], groupId: updated.groupId, groupName: updated.groupName };
    } catch {
        message.error('Erro ao atualizar grupo da despesa');
    }
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
            title: 'Grupo',
            key: 'groupId',
            render(row, index) {
                return h(NSelect, {
                    value: row.groupId ?? null,
                    options: groupOptions(),
                    style: { minWidth: '160px' },
                    onUpdateValue: (v: number | null) => handleGroupChange(row, index, v)
                });
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
