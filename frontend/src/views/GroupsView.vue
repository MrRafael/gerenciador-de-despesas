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
    <div class="groups-container">
        <h2 class="page-title">Gerenciar Grupos</h2>
        <group-menage :my-groups="groups" @group-deleted="loadGroups" @invites-change="loadGroups" />
        <div class="new-groups-wrapper">
            <new-groups @groups-added="loadGroups" />
        </div>
    </div>
</template>

<style scoped>
.groups-container {
    max-width: 700px;
}

.page-title {
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: 1.5rem;
}

.new-groups-wrapper {
    margin-top: 1.5rem;
    padding-top: 1.5rem;
    border-top: 1px solid var(--color-border, rgba(60, 60, 60, 0.12));
}
</style>
