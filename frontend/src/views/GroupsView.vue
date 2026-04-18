<script setup lang="ts">
import NewGroups from '@/components/NewGroups.vue';
import GroupMenage from '@/components/GroupMenage.vue';
import type { MemberGroup } from '@/types';
import { onMounted, ref } from 'vue';
import { getMyGroups } from '@/api/groups';
import { useUserStore } from '@/stores/user';

const sUser = useUserStore();
const groups = ref<MemberGroup[]>([]);

const loadGroups = async () => {
    if (sUser.user?.id)
        groups.value = await getMyGroups(sUser.user.id);
}

onMounted(() => {
    loadGroups();
});

sUser.$onAction(({ after }) => {
    after(() => loadGroups());
});
</script>

<template>
    <group-menage :my-groups="groups" @group-deleted="loadGroups" @invites-change="loadGroups" />
    <new-groups @groups-added="loadGroups" />
</template>

<style scoped></style>
