<script setup lang="ts">
import type { MemberGroup } from '@/types';
import GroupMember from '@/components/GroupMember.vue';
import { NFormItem, useDialog, useMessage, NButton, NSelect, NCheckbox } from 'naive-ui'
import { ref, computed, onMounted } from 'vue';
import InviteList from './InviteList.vue'
import NewInvite from './NewInvite.vue'
import { deleteGroup, deleteMember, getMembers } from '@/api/groups';
import { useUserStore } from '@/stores/user';
import { useSelectedGroupStore } from '@/stores/selectedGroup';


const message = useMessage();
const dialog = useDialog();
const sUser = useUserStore();
const sSelectedGroup = useSelectedGroupStore();
const props = defineProps<{
    myGroups: Array<MemberGroup>,
}>();

const emit = defineEmits(['groupDeleted', 'invitesChange']);

const selectedGroupMembers = ref<MemberGroup[]>([]);
const loadSelectedGroupMembers = async () => {
    if (currentGroup.value) {
        selectedGroupMembers.value = await getMembers(currentGroup.value);
    }
}

const currentGroup = ref<number | null>(sSelectedGroup.groupId ?? null);

onMounted(() => {
    if (currentGroup.value) loadSelectedGroupMembers();
});

const myGroupsOptions = computed(() => {
    return props.myGroups.map(group => ({ value: group.id, label: group.name }))
});

const selectedGroup = computed(() => {
    const selectedGroup = props.myGroups.filter(group => group.id == currentGroup.value);

    return selectedGroup[0];
});

function canIDeleteThisGroup(): boolean {
    if (!selectedGroup.value) return false;
    return sUser.user.id === selectedGroup.value.ownerId || sUser.user.id === selectedGroup.value.userId;
}

function handleDeleteGroup() {
    dialog.warning({
        title: 'Deletar grupo selecionado?',
        content: 'Tem Certeza? todas as despesas adicionadas serão perdidas',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            await deleteGroup(selectedGroup.value.id!)
            currentGroup.value = null;
            emit('groupDeleted')
            message.success('Grupo deletado com sucesso!')
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
            await deleteMember(selectedGroup.value.id!, userId);
            if(selectedGroup.value.userId === userId){
                currentGroup.value = null
            }
            emit('groupDeleted')
            message.success('Membro deletado com sucesso!')
        },
    });
}

</script>
<template>
    <invite-list @invites-change="$emit('invitesChange')" />
    <n-form-item v-if="myGroups.length > 0" label="Selecione um grupo" path="remuneration">
        <n-select v-model:value="currentGroup" :options="myGroupsOptions"
            @update:value="() => loadSelectedGroupMembers()" />
    </n-form-item>
    <slot>
    </slot>
    <div v-if="currentGroup" class="display-flex direction-column members">
        <n-checkbox
            :checked="sSelectedGroup.groupId === currentGroup"
            @update:checked="(v: boolean) => sSelectedGroup.setGroup(v ? currentGroup : null, v ? props.myGroups.find(g => g.id === currentGroup)?.name ?? null : null)"
        >
            Visualização padrão na tela inicial
        </n-checkbox>
        <h3>Membros do grupo</h3>
        <group-member v-for="groupMember in selectedGroupMembers" v-bind:key="groupMember.userId"
            :userName="groupMember.memberName" :user-email="groupMember.memberEmail" :user-id="groupMember.userId"
            :show-delete="canIDeleteThisGroup() && groupMember.userId !== groupMember.ownerId"
            @delete-member="handleDeleteMember" />

        <!-- <div v-if="selectedGroup && [...selectedGroup.invitedPeople, ...emailsAdded].length > 0">
            <h4>Convidados</h4>
            <p v-for="(email, index) in [...selectedGroup.invitedPeople, ...emailsAdded]" v-bind:key="index">{{ email }}</p>
        </div> -->

        <n-button v-if="toggleNewInvite && canIDeleteThisGroup()" @click="toggleNewInvite = false">Convide
            membros</n-button>
        <new-invite v-if="!toggleNewInvite" :selectedGroup="selectedGroup" @inveted="updateInvites" />
        <n-button v-if="canIDeleteThisGroup()" @click="handleDeleteGroup" style="margin-top: 2rem;">Delete grupo
            selecionado</n-button>
    </div>
</template>

<style scoped>
.members * {
    margin-bottom: 1rem;
}
</style>