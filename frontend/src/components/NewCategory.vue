<script setup lang="ts">
import type { ExpenseCategory } from '@/types';
import type { FormInst } from 'naive-ui';
import { ref } from 'vue';
import { NButton, NForm, NFormItem, NCard, NInput, useMessage } from 'naive-ui';
import { createCategory } from '@/api/category';
import { useUserStore } from '@/stores/user';

const message = useMessage();
const sUser = useUserStore();
const emit = defineEmits(['closed']);
const formRef = ref<FormInst | null>(null);
const category = ref<ExpenseCategory>({
    name: ''
});
const isSaving = ref(false);

const save = async () => {
    isSaving.value = true;
    try {
        category.value.userId = sUser.user.id;
        await createCategory(category.value)
        message.success('Categoria salva com sucesso');
        emit('closed');
    } catch {
        message.error('Erro ao salvar categoria, verifique se a categoria ja existe.');
    }

    isSaving.value = false;
}

</script>

<template>
    <n-card style="width: min(600px, 90vw)" title="Nova Categoria" :bordered="false" size="huge" role="dialog" aria-modal="true">
        <n-form ref="formRef" :model="category">
            <n-form-item label="Nome" path="description">
                <n-input v-model:value="category.name" placeholder="Nome" />
            </n-form-item>

            <n-form-item class="display-flex justify-end">
                <n-button class="form-input" @click="save" :disabled="isSaving">Salvar</n-button>
                <n-button class="form-input cancel-btt" @click="emit('closed')">Cancelar</n-button>
            </n-form-item>
        </n-form>
    </n-card>

</template>

<style scoped>
.cancel-btt{
    margin-left: 1rem;
}
</style>