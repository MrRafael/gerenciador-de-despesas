<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { NCheckbox, NTag, NModal, useMessage } from 'naive-ui';
import type { PendingMonth, MonthCloseStatus } from '@/types';
import { getPendingMonths, getMonthCloseStatus, confirmMonthClose, unconfirmMonthClose } from '@/api/monthClose';
import { useUserStore } from '@/stores/user';

const props = defineProps<{ groupId: number }>();

const message = useMessage();
const sUser = useUserStore();

const MONTH_NAMES = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro'];

const pendingMonths = ref<PendingMonth[]>([]);
const statuses = ref<Record<string, MonthCloseStatus>>({});
const showLastConfirmModal = ref(false);
const pendingConfirm = ref<{ month: number; year: number } | null>(null);

const key = (m: number, y: number) => `${y}-${m}`;

onMounted(load);
watch(() => props.groupId, load);

async function load() {
    try {
        const pending = await getPendingMonths(props.groupId);
        pendingMonths.value = pending;
        const result: Record<string, MonthCloseStatus> = {};
        await Promise.all(pending.map(async (pm) => {
            try {
                result[key(pm.month, pm.year)] = await getMonthCloseStatus(props.groupId, pm.month, pm.year);
            } catch { }
        }));
        statuses.value = result;
    } catch {
        pendingMonths.value = [];
    }
}

function isClosed(m: number, y: number) { return statuses.value[key(m, y)]?.isClosed ?? false; }
function pendingCount(m: number, y: number) { return statuses.value[key(m, y)]?.confirmations.filter(c => !c.confirmed).length ?? 0; }

async function handleCheck(month: number, year: number, checked: boolean) {
    if (!checked) {
        try {
            await unconfirmMonthClose(props.groupId, month, year);
            await load();
        } catch { message.error('Erro ao remover confirmação'); }
        return;
    }
    if (pendingCount(month, year) <= 1) {
        pendingConfirm.value = { month, year };
        showLastConfirmModal.value = true;
    } else {
        await doConfirm(month, year);
    }
}

async function doConfirm(month: number, year: number) {
    try {
        await confirmMonthClose(props.groupId, month, year);
        await load();
        message.success('Confirmação registrada');
    } catch {
        message.error('Erro ao confirmar mês');
    }
}

async function confirmLast() {
    if (!pendingConfirm.value) return;
    showLastConfirmModal.value = false;
    await doConfirm(pendingConfirm.value.month, pendingConfirm.value.year);
    pendingConfirm.value = null;
}
</script>

<template>
    <div v-if="pendingMonths.length > 0" class="month-close-widget">
        <div v-for="pm in pendingMonths" :key="key(pm.month, pm.year)" class="pending-block">
            <div class="pending-header">
                <span class="pending-name">{{ MONTH_NAMES[pm.month - 1] }}/{{ pm.year }}</span>
                <n-tag v-if="isClosed(pm.month, pm.year)" type="success" size="small">Fechado</n-tag>
                <span v-else class="pending-hint">— aguardando fechamento</span>
            </div>
            <div class="conf-list">
                <div
                    v-for="conf in statuses[key(pm.month, pm.year)]?.confirmations ?? []"
                    :key="conf.userId"
                >
                    <n-checkbox
                        :checked="conf.confirmed"
                        :disabled="isClosed(pm.month, pm.year) || conf.userId !== sUser.user?.id"
                        @update:checked="(v: boolean) => handleCheck(pm.month, pm.year, v)"
                    >
                        {{ conf.name }}
                    </n-checkbox>
                </div>
            </div>
        </div>
    </div>

    <n-modal v-model:show="showLastConfirmModal" preset="dialog" type="warning"
        title="Confirmar fechamento do mês"
        :positive-text="'Confirmar e fechar'"
        :negative-text="'Cancelar'"
        @positive-click="confirmLast"
        @negative-click="showLastConfirmModal = false">
        <p>Você é o <strong>último membro</strong> a confirmar.</p>
        <p>O mês será <strong>fechado permanentemente</strong>.</p>
    </n-modal>
</template>

<style scoped>
.month-close-widget {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.pending-block {
    border: 1px solid var(--n-border-color, #e0e0e0);
    border-radius: 8px;
    padding: 0.65rem 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.pending-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.pending-name {
    font-weight: 600;
    font-size: 0.9rem;
}

.pending-hint {
    font-size: 0.8rem;
    color: #d03050;
}

.conf-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    font-size: 0.85rem;
}
</style>
