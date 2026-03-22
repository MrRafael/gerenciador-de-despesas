<script setup lang="ts">
import NewGroups from '@/components/NewGroups.vue';
import GroupMenage from '@/components/GroupMenage.vue';
import type { MemberGroup } from '@/types';
import { onMounted, ref } from 'vue';
import { getMyGroups } from '@/api/groups';
import { useUserStore } from '@/stores/user';

const sUser = useUserStore()
const groups = ref<MemberGroup[]>([])

const loadGroups = async () => {
    if (sUser.user?.id)
        groups.value = await getMyGroups(sUser.user.id);
}

onMounted(() => {
    loadGroups();
})

sUser.$onAction(({
    name, // name of the action
    store, // store instance, same as `someStore`
    args, // array of parameters passed to the action
    after, // hook after the action returns or resolves
    onError, // hook if the action throws or rejects
}) => {
    after(() => {
        console.log('aquiii', sUser.user?.id);
        
        loadGroups()
    })
});

</script>

<template>
    <group-menage :my-groups="groups" @group-deleted="() => { loadGroups() }" @invites-change="() => { loadGroups() }"></group-menage>
    <new-groups @groups-added="(e) => {
        loadGroups()
    }" />
</template>

<style scoped></style>