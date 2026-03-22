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
    background: #f9eaff;
    border-radius: 5px;
    padding: 10px;
}
</style>