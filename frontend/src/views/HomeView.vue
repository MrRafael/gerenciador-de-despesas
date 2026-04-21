<script setup lang="ts">
import { ref, h, computed, watch, onMounted } from 'vue';
import { NDataTable, NTag, NIcon, NSelect, NSwitch, NCheckbox, NModal, useDialog, useMessage, type DataTableColumns } from 'naive-ui';
import type { Expense, GroupExpense, MemberGroup, SplitMemberResult } from '@/types';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import { useSelectedGroupStore } from '@/stores/selectedGroup';
import { formatCurrency } from '../util'
import { DeleteOutlineFilled } from '@vicons/material';
import { getCategoriesByUserId } from '@/api/category';
import { deleteExpense, getExpensesByUserIdAndRange, updateExpenseGroup } from '@/api/expense';
import { getGroupExpenses, getMyGroups } from '@/api/groups';
import { getSplitConfigs } from '@/api/splitConfig';
import { getSplitSummary, getMonthCloseStatus, confirmMonthClose, unconfirmMonthClose } from '@/api/monthClose';
import type { MonthCloseStatus } from '@/types';

const SPLIT_TYPE_LABELS: Record<number, string> = { 0: 'Divisão igual', 1: 'Proporcional', 2: 'Manual' };

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
const activeGroupSplitConfigs = ref<{ label: string; value: number }[]>([]);
const isDataLoaded = ref(false);

const splitSummary = ref<{ groupId: number; groupName: string; members: SplitMemberResult[] } | null>(null);

const selectedMonthStatus = ref<MonthCloseStatus | null>(null);
const showLastConfirmModal = ref(false);

const activeGroupId = ref<number | null>(sSelectedGroup.groupId);

const isGroupMode = computed(() => activeGroupId.value !== null);

const isSelectedMonthPast = computed(() => {
    const now = new Date();
    return new Date(sYear.year, sMonth.month - 1) < new Date(now.getFullYear(), now.getMonth());
});

watch(() => sSelectedGroup.groupId, (groupId) => {
    activeGroupId.value = groupId;
    syncExpenses();
    loadSplitSummary();
    loadSelectedMonthStatus();
});

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
    after(() => {
        syncExpenses();
        loadSplitSummary();
        loadSelectedMonthStatus();
    });
});

watch(() => sUser.user?.id, async (id) => {
    if (!id) return;
    await loadData();
    await syncExpenses();
    await loadSplitSummary();
    await loadSelectedMonthStatus();
}, { immediate: true });

onMounted(async () => {
    if (!sUser.user?.id || isDataLoaded.value) return;
    await loadData();
    await syncExpenses();
    await loadSplitSummary();
    await loadSelectedMonthStatus();
});

async function syncExpenses() {
    if (!sUser.user?.id) return;
    const { startDate, endDate } = dateRange.value;

    if (isGroupMode.value) {
        const [result, configs] = await Promise.all([
            getGroupExpenses(activeGroupId.value!, startDate, endDate),
            getSplitConfigs(activeGroupId.value!).catch(() => []),
        ]);
        if (result) groupExpenses.value = result;
        activeGroupSplitConfigs.value = configs.map(c => ({
            label: SPLIT_TYPE_LABELS[c.splitType] + (c.isDefault ? ' (padrão)' : ''),
            value: c.id,
        }));
    } else {
        const result = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
        if (result) expenses.value = result;
        activeGroupSplitConfigs.value = [];
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

async function loadSplitSummary() {
    const groupId = sSelectedGroup.groupId;
    if (!groupId) { splitSummary.value = null; return; }

    const group = groups.value.find(g => g.id === groupId);
    if (!group) { splitSummary.value = null; return; }

    try {
        const configs = await getSplitConfigs(groupId);
        if (configs.length === 0) { splitSummary.value = null; return; }
        const members = await getSplitSummary(groupId, sMonth.month, sYear.year);
        splitSummary.value = { groupId, groupName: group.name, members };
    } catch {
        splitSummary.value = null;
    }
}

async function loadSelectedMonthStatus() {
    const groupId = sSelectedGroup.groupId;
    if (!groupId || !isSelectedMonthPast.value) { selectedMonthStatus.value = null; return; }
    try {
        selectedMonthStatus.value = await getMonthCloseStatus(groupId, sMonth.month, sYear.year);
    } catch {
        selectedMonthStatus.value = null;
    }
}

async function handleCloseCheck(checked: boolean) {
    const groupId = sSelectedGroup.groupId;
    if (!groupId) return;
    if (!checked) {
        try {
            await unconfirmMonthClose(groupId, sMonth.month, sYear.year);
            await loadSelectedMonthStatus();
        } catch { message.error('Erro ao remover confirmação'); }
        return;
    }
    await loadSelectedMonthStatus();
    const pendingCount = selectedMonthStatus.value?.confirmations.filter(c => !c.confirmed).length ?? 2;
    if (pendingCount <= 1) {
        showLastConfirmModal.value = true;
    } else {
        await doConfirmClose();
    }
}

async function doConfirmClose() {
    const groupId = sSelectedGroup.groupId;
    if (!groupId) return;
    try {
        await confirmMonthClose(groupId, sMonth.month, sYear.year);
        await loadSelectedMonthStatus();
        message.success('Confirmação registrada');
    } catch { message.error('Erro ao confirmar mês'); }
}

async function confirmLastClose() {
    showLastConfirmModal.value = false;
    await doConfirmClose();
}

async function handleViewToggle(showAll: boolean) {
    isDataLoaded.value = false;
    await loadData();
    activeGroupId.value = showAll ? null : sSelectedGroup.groupId;
    await syncExpenses();
    isDataLoaded.value = true;
}

const closedMonthMsg = 'Este mês já foi fechado';

const handleDelete = (expenseId: number) => {
    dialog.warning({
        title: 'Deletar despesa selecionada?',
        content: 'Tem Certeza?',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            try {
                await deleteExpense(expenseId);
                await syncExpenses();
            } catch (err: unknown) {
                const status = (err as { response?: { status?: number } })?.response?.status;
                if (status === 409) message.error(closedMonthMsg);
            }
        },
    });
}

const handleGroupChange = async (expense: Expense, index: number, groupId: number | null) => {
    try {
        const updated = await updateExpenseGroup(expense.id!, groupId, undefined);
        expenses.value.splice(index, 1, { ...expenses.value[index], groupId: updated.groupId, groupName: updated.groupName });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        message.error(status === 409 ? closedMonthMsg : 'Erro ao atualizar grupo da despesa');
    }
}

const handleGroupChangeFromGroupView = async (expense: GroupExpense, index: number, groupId: number | null) => {
    try {
        await updateExpenseGroup(expense.id, groupId, undefined);
        groupExpenses.value.splice(index, 1);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        message.error(status === 409 ? closedMonthMsg : 'Erro ao atualizar grupo da despesa');
    }
}

const handleSplitConfigChange = async (expense: GroupExpense, index: number, configId: number) => {
    try {
        await updateExpenseGroup(expense.id, activeGroupId.value!, configId);
        groupExpenses.value.splice(index, 1, { ...groupExpenses.value[index], groupSplitConfigId: configId });
        await loadSplitSummary();
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        message.error(status === 409 ? closedMonthMsg : 'Erro ao atualizar divisão da despesa');
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
            title: 'Divisão',
            key: 'splitConfig',
            render(row, index) {
                if (activeGroupSplitConfigs.value.length === 0) return null;
                if (row.userId !== sUser.user?.id) {
                    const label = activeGroupSplitConfigs.value.find(c => c.value === row.groupSplitConfigId)?.label ?? '—';
                    return h('span', { style: 'font-size: 0.85rem; color: #888;' }, label);
                }
                return h(NSelect, {
                    value: row.groupSplitConfigId ?? null,
                    options: activeGroupSplitConfigs.value,
                    placeholder: 'Selecionar',
                    style: 'width: 140px;',
                    size: 'small',
                    onUpdateValue: (v: number) => handleSplitConfigChange(row, index, v),
                });
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
        : _allExpensesColumns.filter(c => (c as unknown as Record<string, unknown>)['key'] !== 'groupId')
);
const groupColumns = createGroupColumns();

const MONTH_NAMES = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro'];
</script>

<template>
    <div v-if="splitSummary" class="split-summary-container">
        <div class="split-summary-block">
            <div class="split-summary-header">
                <span class="split-summary-group-name">{{ splitSummary.groupName }} — {{ MONTH_NAMES[sMonth.month - 1] }}/{{ sYear.year }}</span>
                <span class="split-total">Total: {{ formatCurrency(splitSummary.members.reduce((s, m) => s + m.amountPaid, 0)) }}</span>
            </div>
            <div v-for="member in splitSummary.members" :key="member.userId" class="split-member-block">
                <div class="split-member-name">{{ member.name }} <span class="member-pct">({{ member.percentage }}%)</span></div>
                <div class="split-member-detail">
                    <span class="detail-label">pagou</span>
                    <span class="detail-value">{{ formatCurrency(member.amountPaid) }}</span>
                    <span class="detail-label">deveria pagar</span>
                    <span class="detail-value">{{ formatCurrency(member.amountOwed) }}</span>
                    <span :class="member.direction === 'receiver' ? 'amount-receiver' : 'amount-payer'" class="balance-chip">
                        {{ member.direction === 'receiver' ? '▲ recebe' : '▼ paga' }}
                        {{ formatCurrency(Math.abs(member.balance)) }}
                    </span>
                </div>
            </div>

            <div v-if="selectedMonthStatus && !selectedMonthStatus.isClosed" class="pending-close-section">
                <div class="pending-close-title">Fechar {{ MONTH_NAMES[sMonth.month - 1] }}/{{ sYear.year }}</div>
                <div class="pending-close-checks">
                    <n-checkbox
                        v-for="conf in selectedMonthStatus.confirmations"
                        :key="conf.userId"
                        :checked="conf.confirmed"
                        :disabled="conf.userId !== sUser.user?.id"
                        @update:checked="(v: boolean) => handleCloseCheck(v)"
                    >{{ conf.name }}</n-checkbox>
                </div>
            </div>
        </div>
    </div>

    <n-modal v-model:show="showLastConfirmModal" preset="dialog" type="warning"
        title="Confirmar fechamento do mês"
        positive-text="Confirmar e fechar"
        negative-text="Cancelar"
        @positive-click="confirmLastClose"
        @negative-click="showLastConfirmModal = false">
        <p>Você é o <strong>último membro</strong> a confirmar.</p>
        <p>O mês será <strong>fechado permanentemente</strong>.</p>
    </n-modal>

    <div class="page-header">
        <h2 class="page-title">Despesas de {{ MONTH_NAMES[sMonth.month - 1] }}/{{ sYear.year }}</h2>
    </div>

    <div v-if="sSelectedGroup.groupId" class="view-toggle-bar">
        <span>Mostrar todas as despesas</span>
        <n-switch :value="!isGroupMode" @update:value="handleViewToggle" />
    </div>
    <template v-if="isDataLoaded">
        <n-data-table v-if="isGroupMode" :columns="groupColumns" :data="groupExpenses" :bordered="false" :scroll-x="800" />
        <n-data-table v-else :columns="allExpensesColumns" :data="expenses" :bordered="false" :scroll-x="800" />
    </template>
</template>

<style scoped>
.split-summary-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.split-summary-block {
    background: var(--n-color, #f8f8f8);
    border: 1px solid var(--n-border-color, #e0e0e0);
    border-radius: 8px;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.split-summary-header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
}

.split-summary-group-name {
    font-size: 0.95rem;
    font-weight: 600;
}

.split-total {
    font-size: 0.85rem;
    color: #888;
}

.split-member-block {
    border-top: 1px solid var(--n-border-color, #eee);
    padding-top: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.split-member-name {
    font-size: 0.9rem;
    font-weight: 600;
}

.member-pct {
    font-size: 0.8rem;
    color: #888;
    font-weight: 400;
}

.split-member-detail {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
    font-size: 0.85rem;
}

.detail-label {
    color: #888;
}

.detail-value {
    font-weight: 500;
    margin-right: 0.35rem;
}

.balance-chip {
    font-weight: 600;
    font-size: 0.85rem;
    padding: 0.1rem 0.5rem;
    border-radius: 4px;
}

.amount-receiver {
    color: #18a058;
    background: #f0faf5;
}

.amount-payer {
    color: #d03050;
    background: #fff0f3;
}

.pending-close-section {
    border-top: 1px solid var(--n-border-color, #eee);
    padding-top: 1rem;
    margin-top: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.pending-close-title {
    font-size: 0.82rem;
    font-weight: 600;
    color: #888;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.pending-close-checks {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    font-size: 0.83rem;
}

.page-header {
    margin-bottom: 1.5rem;
}

.page-title {
    font-size: 1.25rem;
    font-weight: 600;
    margin: 0;
}

.view-toggle-bar {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 8px;
    margin-bottom: 1rem;
    padding: 0.5rem 0;
}

.view-toggle-bar span {
    font-size: 13px;
    color: var(--n-text-color-3, #555);
}

@media (max-width: 600px) {
    .split-summary-header {
        flex-direction: column;
        gap: 0.25rem;
    }

    .split-member-detail {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.25rem;
    }
}
</style>
