<script setup lang="ts">
import { NIcon, NSpace } from 'naive-ui'
import { DeleteOutlineFilled } from '@vicons/material';

defineProps<{
    userId?: string,
    userName?: string,
    userEmail?: string,
    showDelete?: boolean
}>();

</script>

<template>
    <n-space v-if="userId" class="align-center background" justify="space-between">
        <n-space class="align-center">
            <div>
                <p>{{ userName }}</p>
                <p>{{ userEmail }}</p>
            </div>
        </n-space>
        <n-icon v-if="showDelete" @click="$emit('deleteMember', userId )" size="large" :component="DeleteOutlineFilled" class="cursor-pointer" />
    </n-space>
</template>

<style scoped>
.background{
    background: var(--n-color, #f4f0f7);
    border: 1px solid var(--n-border-color, #e8e0f0);
    border-radius: 8px;
    padding: 12px 16px;
}
</style>