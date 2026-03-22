<script setup lang="ts">
import { createGroup } from '@/api/groups';
import { useUserStore } from '@/stores/user';
import { NForm, NFormItem, NDynamicInput, useMessage, NButton } from 'naive-ui';
import { ref } from 'vue';

const sUser = useUserStore();
const message = useMessage();

const emit = defineEmits(['groupsAdded']);

const newGroupsForm = ref<{ groupNames: string[] }>({
    groupNames: []
});

function saveNewGroup() {
    if (newGroupsForm.value.groupNames.filter((groupName: string) => groupName.trim() === '').length > 0) {
        message.error('Não é possível criar um grupo com nome em branco.');
    } else {
        Promise.all(newGroupsForm.value.groupNames.map(newGroup => {
            return createGroup({userId: sUser.user?.id, name: newGroup});
        })).then(() => {
            message.success('Grupo(s) salvo(s) com sucesso!');
            newGroupsForm.value.groupNames = [];
            emit('groupsAdded');
        }).catch(() => {
            message.error('Erro ao salvar novo Grupo.')
        });
    }
}

</script>

<template>
    <n-form :model="newGroupsForm" class="">
        <div style="margin-bottom: 1rem;">
            <n-dynamic-input v-model:value="newGroupsForm.groupNames" placeholder="Nome do Grupo">
                <template #create-button-default>
                    Criar novo grupo de despesas
                </template>
            </n-dynamic-input>
        </div>
        <n-form-item v-if="newGroupsForm.groupNames.length > 0" class="display-flex justify-center">
            <n-button @click="saveNewGroup">Salvar novo(s) grupo(s)</n-button>
        </n-form-item>
    </n-form>
</template>