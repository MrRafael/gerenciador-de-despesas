<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { NButton, NCheckbox, NTag, NModal, useMessage } from 'naive-ui';
import type { SplitMemberResult, MonthCloseStatus } from '@/types';
import { getSplitSummary, getMonthCloseStatus, confirmMonthClose, unconfirmMonthClose } from '@/api/monthClose';
import { useUserStore } from '@/stores/user';
import { formatCurrency } from '@/util';

const props = defineProps<{
    groupId: number;
    groupMembers: { userId: string; name: string }[];
}>();

const message = useMessage();
const sUser = useUserStore();

const MONTH_NAMES = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro'];

const now = new Date();
const viewMonth = ref(now.getMonth() + 1);
const viewYear = ref(now.getFullYear());

const splitResults = ref<SplitMemberResult[]>([]);
const viewedMonthStatus = ref<MonthCloseStatus | null>(null);

const showLastConfirmModal = ref(false);
const isConfirming = ref(false);

const isPastMonth = computed(() => {
    const viewed = new Date(viewYear.value, viewMonth.value - 1);
    const current = new Date(now.getFullYear(), now.getMonth());
    return viewed < current;
});

const isClosed = computed(() => viewedMonthStatus.value?.isClosed ?? false);

const myConfirmation = computed(() =>
    viewedMonthStatus.value?.confirmations.find(c => c.userId === sUser.user?.id)?.confirmed ?? false
);

const pendingCount = computed(() =>
    viewedMonthStatus.value?.confirmations.filter(c => !c.confirmed).length ?? 0
);

onMounted(loadAll);
watch(() => props.groupId, loadAll);

async function loadAll() {
    await Promise.all([loadSplitSummary(), loadViewedMonthStatus()]);
}

async function loadSplitSummary() {
    try {
        splitResults.value = await getSplitSummary(props.groupId, viewMonth.value, viewYear.value);
    } catch {
        splitResults.value = [];
    }
}

async function loadViewedMonthStatus() {
    if (!isPastMonth.value) { viewedMonthStatus.value = null; return; }
    try {
        viewedMonthStatus.value = await getMonthCloseStatus(props.groupId, viewMonth.value, viewYear.value);
    } catch {
        viewedMonthStatus.value = null;
    }
}

async function navigateMonth(delta: number) {
    let m = viewMonth.value + delta;
    let y = viewYear.value;
    if (m > 12) { m = 1; y++; }
    if (m < 1) { m = 12; y--; }
    viewMonth.value = m;
    viewYear.value = y;
    await Promise.all([loadSplitSummary(), loadViewedMonthStatus()]);
}

const isCurrentOrFuture = computed(() => !isPastMonth.value);

async function handleCheckboxChange(checked: boolean) {
    if (!checked) {
        await doUnconfirm();
        return;
    }
    if (pendingCount.value <= 1) {
        showLastConfirmModal.value = true;
    } else {
        await doConfirm();
    }
}

async function doConfirm() {
    isConfirming.value = true;
    try {
        await confirmMonthClose(props.groupId, viewMonth.value, viewYear.value);
        await loadViewedMonthStatus();
        message.success(isClosed.value ? 'Mês fechado!' : 'Confirmação registrada');
    } catch {
        message.error('Erro ao confirmar mês');
    } finally {
        isConfirming.value = false;
    }
}

async function doUnconfirm() {
    try {
        await unconfirmMonthClose(props.groupId, viewMonth.value, viewYear.value);
        await loadViewedMonthStatus();
    } catch {
        message.error('Erro ao remover confirmação');
    }
}

async function confirmLastAndClose() {
    showLastConfirmModal.value = false;
    await doConfirm();
}
</script>

<template>
    <div class="split-summary-section">
        <h4 class="section-title">Divisão do mês</h4>

        <div class="month-nav">
            <n-button size="small" @click="navigateMonth(-1)">‹</n-button>
            <span class="month-label">{{ MONTH_NAMES[viewMonth - 1] }}/{{ viewYear }}</span>
            <n-button size="small" :disabled="isCurrentOrFuture" @click="navigateMonth(1)">›</n-button>
        </div>

        <div v-if="splitResults.length > 0" class="results-list">
            <div class="total-row">
                Total do grupo: <strong>{{ formatCurrency(splitResults.reduce((s, m) => s + m.amountPaid, 0)) }}</strong>
            </div>
            <div v-for="member in splitResults" :key="member.userId" class="member-block">
                <div class="member-block-name">{{ member.name }} <span class="member-pct">({{ member.percentage }}%)</span></div>
                <div class="member-block-detail">
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
        </div>
        <p v-else class="empty-hint">Sem despesas registradas para este período.</p>

        <div v-if="isPastMonth" class="close-section">
            <div class="close-header">
                <span class="close-title">Fechar {{ MONTH_NAMES[viewMonth - 1] }}/{{ viewYear }}</span>
                <n-tag v-if="isClosed" type="success" size="small">Fechado</n-tag>
            </div>
            <div class="confirmations-list">
                <div
                    v-for="conf in viewedMonthStatus?.confirmations ?? []"
                    :key="conf.userId"
                    class="confirmation-row"
                >
                    <n-checkbox
                        :checked="conf.confirmed"
                        :disabled="isClosed || conf.userId !== sUser.user?.id"
                        @update:checked="(v: boolean) => handleCheckboxChange(v)"
                    >
                        {{ conf.name }}
                    </n-checkbox>
                </div>
                <p v-if="!viewedMonthStatus" class="empty-hint">Nenhuma confirmação registrada.</p>
            </div>
        </div>
    </div>

    <n-modal v-model:show="showLastConfirmModal" preset="dialog" type="warning"
        title="Confirmar fechamento do mês"
        :positive-text="'Confirmar e fechar'"
        :negative-text="'Cancelar'"
        @positive-click="confirmLastAndClose"
        @negative-click="showLastConfirmModal = false">
        <p>Você é o <strong>último membro</strong> a confirmar este mês.</p>
        <p>Ao confirmar, o mês será <strong>fechado permanentemente</strong> e não poderá ser revertido.</p>
        <p>Tem certeza?</p>
    </n-modal>
</template>

<style scoped>
.split-summary-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.section-title {
    margin: 0;
    font-size: 0.95rem;
    font-weight: 600;
}

.month-nav {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.month-label {
    font-size: 0.9rem;
    font-weight: 500;
    min-width: 110px;
    text-align: center;
}

.results-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.total-row {
    font-size: 0.85rem;
    color: #555;
    padding-bottom: 0.25rem;
    border-bottom: 1px solid var(--n-border-color, #eee);
}

.member-block {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}

.member-block-name {
    font-size: 0.9rem;
    font-weight: 600;
}

.member-pct {
    font-size: 0.8rem;
    color: #888;
    font-weight: 400;
}

.member-block-detail {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    flex-wrap: wrap;
    font-size: 0.83rem;
}

.detail-label { color: #888; }

.detail-value {
    font-weight: 500;
    margin-right: 0.25rem;
}

.balance-chip {
    font-weight: 600;
    font-size: 0.83rem;
    padding: 0.1rem 0.45rem;
    border-radius: 4px;
}

.amount-receiver { color: #18a058; background: #f0faf5; }
.amount-payer    { color: #d03050; background: #fff0f3; }

.empty-hint {
    font-size: 0.85rem;
    color: #888;
    margin: 0;
}

.close-section {
    border-top: 1px solid var(--n-border-color, #eee);
    padding-top: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.close-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.close-title {
    font-weight: 600;
    font-size: 0.9rem;
}

.confirmations-list {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.confirmation-row {
    display: flex;
    align-items: center;
}
</style>
