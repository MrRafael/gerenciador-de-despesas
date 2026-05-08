<script setup lang="ts">
import { NIcon, NLayoutFooter } from 'naive-ui';
import {
  AddRound,
  HomeFilled,
  GroupsRound,
  UploadFilled,
} from '@vicons/material';
import { useRouter, useRoute } from 'vue-router';
import { computed } from 'vue';

const router = useRouter();
const route = useRoute();

const items = [
  { icon: HomeFilled, label: 'Home', route: '/' },
  { icon: AddRound, label: 'Adicionar', route: '/addExpense' },
  { icon: UploadFilled, label: 'Importar', route: '/import' },
  { icon: GroupsRound, label: 'Grupos', route: '/groups' },
];

const activeRoute = computed(() => route.path);

function navigate(path: string) {
  router.push(path);
}
</script>

<template>
  <n-layout-footer bordered class="bottom-nav">
    <button
      v-for="item in items"
      :key="item.route"
      :class="['nav-item', { active: activeRoute === item.route }]"
      @click="navigate(item.route)"
    >
      <n-icon :component="item.icon" :size="22" />
      <span class="nav-label">{{ item.label }}</span>
    </button>
  </n-layout-footer>
</template>

<style scoped>
.bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 3.5rem;
  display: none; /* oculto por padrão — só aparece no mobile */
  justify-content: space-around;
  align-items: center;
  z-index: 100;
  padding: 0;
}

@media (max-width: 768px) {
  .bottom-nav {
    display: flex;
  }
}

.nav-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  background: none;
  border: none;
  color: #888;
  cursor: pointer;
  padding: 4px 12px;
  border-radius: 8px;
  transition: color 0.2s, background 0.2s;
  font-size: 0;
  min-width: 56px;
}

.nav-item.active {
  color: #18a058;
}

.nav-item:active {
  background: rgba(24, 160, 88, 0.08);
}

.nav-label {
  font-size: 10px;
  line-height: 1.2;
  font-weight: 500;
}
</style>