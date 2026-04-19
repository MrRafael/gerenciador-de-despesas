<script setup lang="ts">
import { ref, h, computed, watch } from 'vue';
import { NDataTable, NTag, NIcon, NSelect, NSwitch, useDialog, useMessage, type DataTableColumns } from 'naive-ui';
import type { Expense, GroupExpense, MemberGroup } from '@/types';
import { SplitType } from '@/types';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import { useSelectedGroupStore } from '@/stores/selectedGroup';
import { formatCurrency } from '../util'
import { DeleteOutlineFilled } from '@vicons/material';
import { getCategoriesByUserId } from '@/api/category';
import { deleteExpense, getExpensesByUserIdAndRange, updateExpenseGroup } from '@/api/expense';
import { getGroupExpenses, getMyGroups } from '@/api/groups';

const dialog = useDialog();
const message = useMessage();
const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();
const sSelectedGroup = useSelectedGroupStore();

const options = ref<{ label: string, value: number }[]>([]);
const groups = ref<MemberGroup[]>([]);
const expenses = ref<Expense[]>([]);
const groupExpenses = ref<GroupExpense[]>([]);
const isDataLoaded = ref(false);

const activeGroupId = ref<number | null>(sSelectedGroup.groupId);

const isGroupMode = computed(() => activeGroupId.value !== null);

const dateRange = computed(() => {
    const startDate = `${sYear.year}-${String(sMonth.month).padStart(2, '0')}-01`;
    const endDate = new Date(sYear.year, sMonth.month, 0).toISOString().split('T')[0];
    return { startDate, endDate };
});

const groupOptions = computed(() => [
    { label: 'Pessoal (sem grupo)', value: null as number | null },
    ...groups.value.map(g => ({ label: g.name, value: g.id as number | null }))
]);

sMonth.$onAction(({ after }) => {
    after(() => syncExpenses());
});

watch(() => sUser.user?.id, async (id) => {
    if (!id) return;
    await loadData();
    await syncExpenses();
}, { immediate: true });

async function syncExpenses() {
    if (!sUser.user?.id) return;
    const { startDate, endDate } = dateRange.value;

    if (isGroupMode.value) {
        const result = await getGroupExpenses(activeGroupId.value!, startDate, endDate);
        if (result) groupExpenses.value = result;
    } else {
        const result = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
        if (result) expenses.value = result;
    }
}

const loadData = async () => {
    if (!sUser.user?.id) return;
    const [cat, grp] = await Promise.all([
        getCategoriesByUserId(sUser.user.id),
        getMyGroups(sUser.user.id),
    ]);
    if (cat) options.value = cat.map(x => ({ label: x.name!, value: x.id! }));
    if (grp) groups.value = grp;
    isDataLoaded.value = true;
}

async function handleViewToggle(showAll: boolean) {
    isDataLoaded.value = false;
    await loadData();
    activeGroupId.value = showAll ? null : sSelectedGroup.groupId;
    await syncExpenses();
}

const handleDelete = (expenseId: number) => {
    dialog.warning({
        title: 'Deletar despesa selecionada?',
        content: 'Tem Certeza?',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            await deleteExpense(expenseId);
            await syncExpenses();
        },
    });
}

const handleGroupChange = async (expense: Expense, index: number, groupId: number | null) => {
    try {
        const splitType = groupId != null ? SplitType.Equal : undefined;
        const updated = await updateExpenseGroup(expense.id!, groupId, splitType);
        expenses.value.splice(index, 1, { ...expenses.value[index], groupId: updated.groupId, groupName: updated.groupName });
    } catch {
        message.error('Erro ao atualizar grupo da despesa');
    }
}

const handleGroupChangeFromGroupView = async (expense: GroupExpense, index: number, groupId: number | null) => {
    try {
        const splitType = groupId != null ? SplitType.Equal : undefined;
        await updateExpenseGroup(expense.id, groupId, splitType);
        groupExpenses.value.splice(index, 1);
    } catch {
        message.error('Erro ao atualizar grupo da despesa');
    }
}

function createAllExpensesColumns(): DataTableColumns<Expense> {
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
                const label = options.value.find(x => x.value == row.categoryId)?.label;
                if (!label) return null;
                return h(NTag, {}, () => label);
            }
        },
        {
            title: 'Grupo',
            key: 'groupId',
            render(row, index) {
                if (row.groupId != null) {
                    return h(NTag, {
                        closable: true,
                        onClose: () => handleGroupChange(row, index, null)
                    }, () => row.groupName ?? String(row.groupId));
                }
                return h(NSelect, {
                    value: null,
                    placeholder: 'Sem grupo',
                    options: groupOptions.value.filter(o => o.value !== null),
                    style: { width: '130px' },
                    onUpdateValue: (v: number | null) => handleGroupChange(row, index, v)
                });
            }
        },
        {
            title: 'Deletar',
            key: 'actions',
            render(row) {
                return h(
                    NIcon,
                    { color: "red", style: { cursor: "pointer" }, onClick: () => handleDelete(row.id!) },
                    () => h(DeleteOutlineFilled)
                );
            }
        }
    ];
}

function createGroupColumns(): DataTableColumns<GroupExpense> {
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
                const label = options.value.find(x => x.value == row.categoryId)?.label;
                if (!label) return null;
                return h(NTag, {}, () => label);
            }
        },
        {
            title: 'Grupo',
            key: 'group',
            render(row, index) {
                const groupName = groups.value.find(g => g.id === activeGroupId.value)?.name ?? String(activeGroupId.value);
                return h(NTag, {
                    closable: true,
                    onClose: () => handleGroupChangeFromGroupView(row, index, null)
                }, () => groupName);
            }
        },
        {
            title: 'Deletar',
            key: 'actions',
            render(row) {
                if (row.userId !== sUser.user?.id) return null;
                return h(
                    NIcon,
                    { color: "red", style: { cursor: "pointer" }, onClick: () => handleDelete(row.id) },
                    () => h(DeleteOutlineFilled)
                );
            }
        }
    ];
}

const _allExpensesColumns = createAllExpensesColumns();
const allExpensesColumns = computed(() =>
    groups.value.length > 0
        ? _allExpensesColumns
        : _allExpensesColumns.filter(c => c.key !== 'groupId')
);
const groupColumns = createGroupColumns();
</script>

<template>
    <div v-if="sSelectedGroup.groupId" style="display: flex; align-items: center; justify-content: flex-end; gap: 8px; margin-bottom: 1rem;">
        <span style="font-size: 13px; color: #555;">Mostrar todas as despesas</span>
        <n-switch :value="!isGroupMode" @update:value="handleViewToggle" />
    </div>
    <template v-if="isDataLoaded">
        <n-data-table v-if="isGroupMode" :columns="groupColumns" :data="groupExpenses" :bordered="false" />
        <n-data-table v-else :columns="allExpensesColumns" :data="expenses" :bordered="false" />
    </template>
</template>

<style scoped></style>
