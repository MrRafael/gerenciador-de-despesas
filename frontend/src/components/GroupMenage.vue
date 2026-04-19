<script setup lang="ts">
import type { MemberGroup } from '@/types';
import GroupMember from '@/components/GroupMember.vue';
import GroupSplitConfigTab from '@/components/GroupSplitConfigTab.vue';
import GroupSplitSummarySection from '@/components/GroupSplitSummarySection.vue';
import { NFormItem, useDialog, useMessage, NButton, NSelect, NCheckbox, NTabs, NTabPane, NInputNumber } from 'naive-ui';
import { ref, computed, onMounted, watch } from 'vue';
import InviteList from './InviteList.vue';
import NewInvite from './NewInvite.vue';
import { deleteGroup, deleteMember, getMembers, setMemberSalary } from '@/api/groups';
import { getSplitConfigs } from '@/api/splitConfig';
import { formatCurrency } from '@/util';
import { useUserStore } from '@/stores/user';
import { useSelectedGroupStore } from '@/stores/selectedGroup';

const message = useMessage();
const dialog = useDialog();
const sUser = useUserStore();
const sSelectedGroup = useSelectedGroupStore();

const props = defineProps<{
    myGroups: Array<MemberGroup>;
}>();

const emit = defineEmits(['groupDeleted', 'invitesChange']);

const selectedGroupMembers = ref<MemberGroup[]>([]);
const hasSplitConfigs = ref(false);
const currentGroup = ref<number | null>(sSelectedGroup.groupId ?? null);

const loadSelectedGroupMembers = async () => {
    if (currentGroup.value) {
        selectedGroupMembers.value = await getMembers(currentGroup.value);
    }
};

const loadSplitConfigsFlag = async () => {
    if (!currentGroup.value) { hasSplitConfigs.value = false; return; }
    try {
        const configs = await getSplitConfigs(currentGroup.value);
        hasSplitConfigs.value = configs.length > 0;
    } catch {
        hasSplitConfigs.value = false;
    }
};

onMounted(async () => {
    if (currentGroup.value) {
        await Promise.all([loadSelectedGroupMembers(), loadSplitConfigsFlag()]);
    }
});

const myGroupsOptions = computed(() =>
    props.myGroups.map(group => ({ value: group.id, label: group.name }))
);

const selectedGroup = computed(() =>
    props.myGroups.find(group => group.id === currentGroup.value)
);

const isOwner = computed(() => sUser.user?.id === selectedGroup.value?.ownerId);

const groupMembersForSummary = computed(() =>
    selectedGroupMembers.value.map(m => ({ userId: m.userId!, name: m.memberName ?? m.userId! }))
);

watch(() => props.myGroups, (groups) => {
    if (groups.length > 0 && currentGroup.value !== null) {
        const stillExists = groups.some(g => g.id === currentGroup.value);
        if (!stillExists) {
            currentGroup.value = null;
            sSelectedGroup.setGroup(null, null);
        }
    }
});

async function onGroupChange() {
    await Promise.all([loadSelectedGroupMembers(), loadSplitConfigsFlag()]);
}

function canIDeleteThisGroup(): boolean {
    if (!selectedGroup.value) return false;
    return sUser.user?.id === selectedGroup.value.ownerId;
}

function handleDeleteGroup() {
    dialog.warning({
        title: 'Deletar grupo selecionado?',
        content: 'Tem Certeza? todas as despesas adicionadas serão perdidas',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            await deleteGroup(selectedGroup.value!.id!);
            currentGroup.value = null;
            emit('groupDeleted');
            message.success('Grupo deletado com sucesso!');
        },
    });
}

const toggleNewInvite = ref(true);
const emailsAdded = ref<string[]>([]);

function updateInvites(email: string) {
    emailsAdded.value.push(email);
    toggleNewInvite.value = true;
}

async function handleDeleteMember(userId: string) {
    dialog.warning({
        title: 'Deletar membro?',
        content: 'Tem Certeza? todas as despesas adicionadas serão perdidas',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            await deleteMember(selectedGroup.value!.id!, userId);
            if (selectedGroup.value?.userId === userId) {
                currentGroup.value = null;
            }
            emit('groupDeleted');
            message.success('Membro deletado com sucesso!');
        },
    });
}

async function onSplitConfigsChanged() {
    await loadSplitConfigsFlag();
}

const editingSalaryUserId = ref<string | null>(null);
const salaryInputValue = ref<number | null>(null);

function startEditSalary(member: MemberGroup) {
    editingSalaryUserId.value = member.userId;
    salaryInputValue.value = member.salary ?? null;
}

async function saveSalary(member: MemberGroup) {
    try {
        await setMemberSalary(currentGroup.value!, member.userId, salaryInputValue.value);
        member.salary = salaryInputValue.value;
        message.success('Salário atualizado');
    } catch {
        message.error('Erro ao salvar salário');
    } finally {
        editingSalaryUserId.value = null;
    }
}
</script>

<template>
    <invite-list @invites-change="$emit('invitesChange')" />
    <n-form-item v-if="myGroups.length > 0" label="Selecione um grupo" path="remuneration">
        <n-select v-model:value="currentGroup" :options="myGroupsOptions" @update:value="onGroupChange" />
    </n-form-item>
    <slot />

    <div v-if="currentGroup" class="group-detail">
        <n-checkbox
            :checked="sSelectedGroup.groupId === currentGroup"
            @update:checked="(v: boolean) => sSelectedGroup.setGroup(v ? currentGroup : null, v ? props.myGroups.find(g => g.id === currentGroup)?.name ?? null : null)"
        >
            Visualização padrão na tela inicial
        </n-checkbox>

        <n-tabs type="line" animated style="margin-top: 1rem;">
            <n-tab-pane name="members" tab="Membros">
                <div class="tab-content">
                    <div v-for="groupMember in selectedGroupMembers" :key="groupMember.userId" class="member-row">
                        <group-member
                            :userName="groupMember.memberName"
                            :user-email="groupMember.memberEmail"
                            :user-id="groupMember.userId"
                            :show-delete="canIDeleteThisGroup() && groupMember.userId !== groupMember.ownerId"
                            @delete-member="handleDeleteMember"
                        />
                        <div class="salary-row">
                            <span class="salary-label">Salário:</span>
                            <template v-if="editingSalaryUserId === groupMember.userId">
                                <n-input-number
                                    v-model:value="salaryInputValue"
                                    :min="0"
                                    :precision="2"
                                    size="small"
                                    style="width: 140px;"
                                    placeholder="0,00"
                                />
                                <n-button size="tiny" type="primary" @click="saveSalary(groupMember)">Salvar</n-button>
                                <n-button size="tiny" @click="editingSalaryUserId = null">Cancelar</n-button>
                            </template>
                            <template v-else>
                                <span class="salary-value">{{ groupMember.salary != null ? formatCurrency(groupMember.salary) : '—' }}</span>
                                <n-button
                                    v-if="groupMember.userId === sUser.user?.id"
                                    size="tiny"
                                    @click="startEditSalary(groupMember)"
                                >Editar</n-button>
                            </template>
                        </div>
                    </div>
                    <n-button v-if="toggleNewInvite && canIDeleteThisGroup()" @click="toggleNewInvite = false">
                        Convide membros
                    </n-button>
                    <new-invite v-if="!toggleNewInvite && selectedGroup" :selectedGroup="selectedGroup!" @inveted="updateInvites" />
                    <n-button v-if="canIDeleteThisGroup()" @click="handleDeleteGroup" style="margin-top: 1rem;">
                        Delete grupo selecionado
                    </n-button>
                </div>
            </n-tab-pane>

            <n-tab-pane name="split-config" tab="Configurações de divisão">
                <div class="tab-content">
                    <group-split-config-tab
                        :group-id="currentGroup"
                        :owner-id="selectedGroup!.ownerId"
                        @configs-changed="onSplitConfigsChanged"
                    />
                </div>
            </n-tab-pane>

            <n-tab-pane v-if="hasSplitConfigs" name="split-summary" tab="Divisão do mês">
                <div class="tab-content">
                    <group-split-summary-section
                        :group-id="currentGroup"
                        :group-members="groupMembersForSummary"
                    />
                </div>
            </n-tab-pane>
        </n-tabs>
    </div>
</template>

<style scoped>
.group-detail {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.tab-content {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    padding-top: 0.5rem;
}

.member-row {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid var(--n-border-color, #f0f0f0);
}

.member-row:last-child {
    border-bottom: none;
}

.salary-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
    padding-left: 0.25rem;
}

.salary-label {
    color: #888;
    min-width: 50px;
}

.salary-value {
    font-weight: 500;
}
</style>
