<script setup lang="ts">
import type { MenuOption } from 'naive-ui'
import type { Component } from 'vue'
import {
  AddCircleOutlineTwotone as AddIcon,
  HomeFilled as HomeIcon,
  UploadFilled as UploadIcon,
  GroupFilled as GroupIcon,
} from '@vicons/material'
import { NIcon, NSpace, NLayout, NLayoutSider, NMenu } from 'naive-ui'
import { h, ref } from 'vue'
import { RouterLink } from 'vue-router'

function renderIcon(icon: Component) {
  return () => h(NIcon, null, { default: () => h(icon) })
}

const menuOptions: MenuOption[] = [
  {
    label: () =>
      h(
        RouterLink,
        {
          to: {
            name: 'addExpense',
          }
        },
        { default: () => 'Adicionar Despesa' }
      ),
    key: 'add-expense',
    icon: renderIcon(AddIcon),
  },
  {
    label: () =>
      h(
        RouterLink,
        {
          to: {
            name: 'home',
          }
        },
        { default: () => 'Home' }
      ),
    key: 'home',
    icon: renderIcon(HomeIcon),
  },
  {
    label: () =>
      h(
        RouterLink,
        {
          to: {
            name: 'import',
          }
        },
        { default: () => 'Importar fatura' }
      ),
    key: 'import',
    icon: renderIcon(UploadIcon),
  },
  {
    label: () =>
      h(
        RouterLink,
        {
          to: {
            name: 'groups',
          }
        },
        { default: () => 'Gerenciar grupos' }
      ),
    key: 'groups',
    icon: renderIcon(GroupIcon),
  }
]

const activeKey = ref<string | null>(null)
const collapsed = ref(true)
</script>

<template>
  <n-space vertical class="side-component">
    <n-layout has-sider class="container">
      <n-layout-sider
        class="sider"
        bordered
        collapse-mode="width"
        :collapsed-width="64"
        :width="240"
        :collapsed="collapsed"
        show-trigger
        @collapse="collapsed = true"
        @expand="collapsed = false"
      >
        <n-menu
          v-model:value="activeKey"
          :collapsed="collapsed"
          :collapsed-width="64"
          :options="menuOptions"
        />
      </n-layout-sider>
      <n-layout class="content-side">
        <slot></slot>
      </n-layout>
    </n-layout>
  </n-space>
</template>

<style scoped>
.side-component {
    width: 100%;
    height: 100vh;
}

.side-component .container{
    height: 100vh;
}

.side-component .sider{
    padding-top: 4rem;
}

.content-side {
    padding: 5.5rem 2rem 2rem 2rem;
}

@media (max-width: 768px) {
    .sider {
        display: none !important;
    }

    .content-side {
        padding: 5rem 1rem 5rem 1rem !important;
    }
}
</style>