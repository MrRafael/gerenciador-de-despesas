<script setup lang="ts">
import {NResult, NButton} from 'naive-ui'
const emit = defineEmits(['install'])
</script>
<template>
    <n-result status="404" title="Esse Site é um App!" description="Para poder ter acesso a todas as funcionalidades a esse app de finanças domestica é necessario realizar a instalação." size="huge">
        <template #footer>
            <n-button @click="emit('install')">Instalar</n-button>
        </template>
    </n-result>
</template>