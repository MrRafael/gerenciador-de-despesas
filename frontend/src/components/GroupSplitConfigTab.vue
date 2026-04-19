<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { NButton, NTag, NModal, NForm, NFormItem, NSelect, NCheckbox, NInputNumber, useMessage, NSpace, NDivider } from 'naive-ui';
import type { GroupSplitConfig, GroupSplitConfigShare, MemberGroup, SplitType } from '@/types';
import { getSplitConfigs, createSplitConfig, updateSplitConfig, deleteSplitConfig } from '@/api/splitConfig';
import { getMembers } from '@/api/groups';
import { useUserStore } from '@/stores/user';

const props = defineProps<{
    groupId: number;
    ownerId: string;
}>();

const emit = defineEmits<{
    configsChanged: [];
}>();

const message = useMessage();
const sUser = useUserStore();

const configs = ref<GroupSplitConfig[]>([]);
const members = ref<MemberGroup[]>([]);
const showModal = ref(false);
const editingConfigId = ref<number | null>(null);
const isSaving = ref(false);

const SPLIT_TYPE_LABELS: Record<number, string> = {
    0: 'Divisão igual',
    1: 'Proporcional ao salário',
    2: 'Manual',
};

const isOwner = computed(() => sUser.user?.id === props.ownerId);

const form = ref<{
    splitType: number;
    isDefault: boolean;
    shares: { userId: string; name: string; percentage: number }[];
}>({
    splitType: 0,
    isDefault: false,
    shares: [],
});

const splitTypeOptions = computed(() => {
    const usedTypes = new Set(
        configs.value
            .filter(c => c.id !== editingConfigId.value)
            .map(c => c.splitType)
    );
    return [
        { label: 'Divisão igual', value: 0 },
        { label: 'Proporcional ao salário', value: 1 },
        { label: 'Manual', value: 2 },
    ].filter(o => !usedTypes.has(o.value as SplitType));
});

const allMembers = computed(() => {
    return members.value.filter(m => m.isActive || m.ownerId === m.userId);
});

const sharesTotal = computed(() => form.value.shares.reduce((sum, s) => sum + (s.percentage || 0), 0));
const sharesValid = computed(() => Math.abs(sharesTotal.value - 100) < 0.01);

onMounted(loadData);
watch(() => props.groupId, loadData);

async function loadData() {
    const [cfgs, mbrs] = await Promise.all([
        getSplitConfigs(props.groupId),
        getMembers(props.groupId),
    ]);
    configs.value = cfgs;
    members.value = mbrs;
}

function openAddModal() {
    editingConfigId.value = null;
    form.value = {
        splitType: splitTypeOptions.value[0]?.value ?? 0,
        isDefault: configs.value.length === 0,
        shares: allMembers.value.map(m => ({
            userId: m.userId!,
            name: m.memberName ?? m.userId!,
            percentage: 0,
        })),
    };
    showModal.value = true;
}

function openEditModal(config: GroupSplitConfig) {
    editingConfigId.value = config.id;
    form.value = {
        splitType: config.splitType,
        isDefault: config.isDefault,
        shares: allMembers.value.map(m => {
            const existing = config.shares.find(s => s.userId === m.userId);
            return {
                userId: m.userId!,
                name: m.memberName ?? m.userId!,
                percentage: existing?.percentage ?? 0,
            };
        }),
    };
    showModal.value = true;
}

async function save() {
    if (form.value.splitType === 2 && !sharesValid.value) {
        message.error('Os percentuais devem somar 100%');
        return;
    }

    isSaving.value = true;
    try {
        const shares: GroupSplitConfigShare[] = form.value.shares.map(s => ({
            userId: s.userId,
            percentage: s.percentage,
        }));

        if (editingConfigId.value !== null) {
            await updateSplitConfig(props.groupId, editingConfigId.value, {
                isDefault: form.value.isDefault,
                shares: form.value.splitType === 2 ? shares : [],
            });
            message.success('Configuração atualizada');
        } else {
            await createSplitConfig(props.groupId, {
                splitType: form.value.splitType as SplitType,
                isDefault: form.value.isDefault,
                shares: form.value.splitType === 2 ? shares : [],
            });
            message.success('Configuração criada');
        }

        showModal.value = false;
        await loadData();
        emit('configsChanged');
    } catch {
        message.error('Erro ao salvar configuração');
    } finally {
        isSaving.value = false;
    }
}

async function removeConfig(configId: number) {
    try {
        await deleteSplitConfig(props.groupId, configId);
        await loadData();
        emit('configsChanged');
        message.success('Configuração removida');
    } catch {
        message.error('Não é possível remover a única configuração');
    }
}

async function setDefault(configId: number) {
    const config = configs.value.find(c => c.id === configId);
    if (!config) return;
    await updateSplitConfig(props.groupId, configId, {
        isDefault: true,
        shares: config.shares,
    });
    await loadData();
}

function memberName(userId: string): string {
    return members.value.find(m => m.userId === userId)?.memberName ?? userId;
}

function equalBreakdown(): { name: string; percentage: number }[] {
    const n = allMembers.value.length;
    if (n === 0) return [];
    const pct = Math.round((100 / n) * 10) / 10;
    return allMembers.value.map(m => ({ name: m.memberName ?? m.userId!, percentage: pct }));
}

function proportionalBreakdown(): { name: string; percentage: number | null; hasSalary: boolean }[] {
    const withSalary = allMembers.value.map(m => ({
        name: m.memberName ?? m.userId!,
        salary: m.salary ?? null,
    }));
    const total = withSalary.reduce((sum, m) => sum + (m.salary ?? 0), 0);
    return withSalary.map(m => ({
        name: m.name,
        percentage: total > 0 && m.salary != null ? Math.round((m.salary / total) * 1000) / 10 : null,
        hasSalary: m.salary != null,
    }));
}
</script>

<template>
    <div class="split-config-tab">
        <div v-for="config in configs" :key="config.id" class="config-item">
            <div class="config-header">
                <span class="config-label">{{ SPLIT_TYPE_LABELS[config.splitType] }}</span>
                <n-tag v-if="config.isDefault" size="small" type="success">padrão</n-tag>
                <n-space v-if="isOwner">
                    <n-button v-if="!config.isDefault" size="tiny" @click="setDefault(config.id)">Tornar padrão</n-button>
                    <n-button size="tiny" @click="openEditModal(config)">Editar</n-button>
                    <n-button size="tiny" type="error" :disabled="configs.length <= 1" @click="removeConfig(config.id)">Remover</n-button>
                </n-space>
            </div>
            <div class="shares-list">
                <template v-if="config.splitType === 0">
                    <div v-for="row in equalBreakdown()" :key="row.name" class="share-row">
                        <span>{{ row.name }}</span>
                        <span>{{ row.percentage }}%</span>
                    </div>
                </template>
                <template v-else-if="config.splitType === 1">
                    <div v-for="row in proportionalBreakdown()" :key="row.name" class="share-row">
                        <span>{{ row.name }}</span>
                        <span v-if="row.percentage !== null">{{ row.percentage }}%</span>
                        <span v-else class="no-salary">sem salário</span>
                    </div>
                    <p v-if="proportionalBreakdown().some(r => !r.hasSalary)" class="hint">
                        Membros sem salário são excluídos do cálculo proporcional.
                    </p>
                </template>
                <template v-else-if="config.splitType === 2 && config.shares.length > 0">
                    <div v-for="share in config.shares" :key="share.userId" class="share-row">
                        <span>{{ memberName(share.userId) }}</span>
                        <span>{{ share.percentage }}%</span>
                    </div>
                </template>
            </div>
        </div>

        <n-divider v-if="configs.length > 0" />

        <n-button v-if="isOwner && splitTypeOptions.length > 0" @click="openAddModal">
            Adicionar configuração de divisão
        </n-button>
        <p v-else style="color: #888; font-size: 0.85rem;">Todos os tipos de divisão já estão cadastrados.</p>

        <n-modal v-model:show="showModal" preset="card" title="Configuração de divisão" style="max-width: 480px;">
            <n-form>
                <n-form-item v-if="editingConfigId === null" label="Tipo de divisão">
                    <n-select v-model:value="form.splitType" :options="splitTypeOptions" />
                </n-form-item>
                <n-form-item label="Config padrão do grupo">
                    <n-checkbox v-model:checked="form.isDefault">Marcar como padrão</n-checkbox>
                </n-form-item>
                <template v-if="form.splitType === 2">
                    <n-divider>Percentuais por membro</n-divider>
                    <n-form-item v-for="share in form.shares" :key="share.userId" :label="share.name">
                        <n-input-number v-model:value="share.percentage" :min="0" :max="100" :step="1" />
                        <span style="margin-left: 8px;">%</span>
                    </n-form-item>
                    <p :style="{ color: sharesValid ? '#18a058' : '#d03050', fontSize: '0.85rem' }">
                        Total: {{ sharesTotal }}% {{ sharesValid ? '✓' : '(deve ser 100%)' }}
                    </p>
                </template>
            </n-form>
            <template #footer>
                <n-space justify="end">
                    <n-button @click="showModal = false">Cancelar</n-button>
                    <n-button type="primary" :loading="isSaving" @click="save">Salvar</n-button>
                </n-space>
            </template>
        </n-modal>
    </div>
</template>

<style scoped>
.split-config-tab {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}
.config-item {
    border: 1px solid var(--n-border-color, #e0e0e0);
    border-radius: 8px;
    padding: 0.75rem;
}
.config-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
}
.config-label {
    font-weight: 600;
    flex: 1;
}
.shares-list {
    margin-top: 0.5rem;
    font-size: 0.85rem;
}
.share-row {
    display: flex;
    justify-content: space-between;
    padding: 0.15rem 0;
}

.no-salary {
    color: #d03050;
    font-size: 0.8rem;
}

.hint {
    font-size: 0.78rem;
    color: #888;
    margin: 0.25rem 0 0;
}
</style>
