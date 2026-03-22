<script setup lang="ts">
import type { ExpenseGroup } from '@/types';
import { ref } from 'vue';
import { NButton, NInputGroup, NInput, useMessage } from 'naive-ui'
import { createMember } from '@/api/groups';

const props = defineProps<{
    selectedGroup: ExpenseGroup
}>();

const message = useMessage();
const emit = defineEmits(['inveted']);

const emailToInvite = ref('');

async function inviteMember() {
    try {
        await createMember({ groupId: props.selectedGroup.id!, userEmail: emailToInvite.value })
        emit('inveted', emailToInvite.value);
        emailToInvite.value = '';
        message.success("Convite enviado");
    } catch {
        message.error("Verifique se esse email esta cadastrado");
    }
    
}

</script>

<template>
    <div>
        <n-input-group>
            <n-input placeholder="email do convidado" v-model:value="emailToInvite"></n-input>
            <n-button @click="inviteMember">Convidar</n-button>
        </n-input-group>

    </div>
</template>