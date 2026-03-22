<script setup lang="ts">
import type { ExpenseGroup, MemberGroup } from '@/types';
import { onBeforeMount, ref } from 'vue';
import { NButton } from 'naive-ui'
import { acceptInvite, getInvites, refuseInvite } from '@/api/groups';
import { useUserStore } from '@/stores/user';

const sUser = useUserStore();
const emit = defineEmits(['invitesChange']);

const invitedGroups = ref<MemberGroup[]>([]);
async function syncInvites() {
    if(sUser.user)
        invitedGroups.value = await getInvites(sUser.user?.id);
}

onBeforeMount(async () => {
    await syncInvites();
});

sUser.$onAction(({
    name, // name of the action
    store, // store instance, same as `someStore`
    args, // array of parameters passed to the action
    after, // hook after the action returns or resolves
    onError, // hook if the action throws or rejects
}) => {
    after(() => {
        syncInvites();
    })
});

async function accepts(invite: MemberGroup) {
    await acceptInvite(invite.userId, invite.id!);
    emit('invitesChange');
    syncInvites();
}

async function reject(invite: ExpenseGroup) {
    await refuseInvite(invite.userId, invite.id!)
    syncInvites();
    emit('invitesChange');
}

</script>

<template>
    <div v-if="invitedGroups.length > 0">
        <h3>Convites</h3>
        <div v-for="(invite, index) in invitedGroups" v-bind:key="index" class="invite display-flex direction-column">
            <span>Grupo: {{ invite.name }}</span>
            <span>Dono(a): {{ invite.ownerName }}</span>
            <div class="display-flex justify-evenly" style="margin-top: 1rem;">
                <n-button round type="error" @click="reject(invite)">
                    Recusar
                </n-button>
                <n-button round type="success" @click="accepts(invite)">
                    Aceitar
                </n-button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.invite {
    background: #f9eaff;
    border-radius: 5px;
    padding: 10px;
    margin: 1rem 0;
}
</style>