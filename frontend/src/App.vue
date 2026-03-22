<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import TopApp from './components/TopApp.vue';
import SideMenu from './components/SideMenu.vue';
import BottomApp from './components/BottomApp.vue';
import { NConfigProvider, NMessageProvider, NDialogProvider, NLoadingBarProvider } from 'naive-ui'
import { ptBR, datePtBR } from 'naive-ui';
import InstallPage from './views/InstallPage.vue';
import { ref, onBeforeMount, computed, inject } from 'vue';
import { SignedIn, SignedOut, SignInButton, UserButton, SignIn, useAuth } from '@clerk/vue';
import axios from 'axios';
const { getToken } = useAuth();

const month = ref(0);

function updateMonth(m: number) {
    month.value = m;
}

const displayMode = ref(getPWADisplayMode());

const showApp = computed(() => displayMode.value !== 'browser');
const installEvent = ref<any>();

function getPWADisplayMode() {
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches;
    if (document.referrer.startsWith('android-app://')) {
        return 'twa';
    } else if (isStandalone) {
        return 'standalone';
    }
    return 'browser';
}

onBeforeMount(async () => {
    axios.defaults.baseURL = import.meta.env.VITE_BACKEND_URL;
    axios.interceptors.request.use(async config => {
        const token = await getToken.value();
        config.headers.Authorization = `Bearer ${token}`;

        return config;
    })

    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault()
        installEvent.value = e
    })

    window.matchMedia('(display-mode: standalone)').addEventListener('change', (evt) => {
        let dm = 'browser';
        if (evt.matches) {
            dm = 'standalone';
        }
        displayMode.value = dm;
    });
})

function installPWA() {
    installEvent.value.prompt();
}
</script>

<template>
    <SignedOut>
        <SignIn />
    </SignedOut>
    <n-config-provider :locale="ptBR" :date-locale="datePtBR">
        <n-loading-bar-provider>
            <n-message-provider>
                <n-dialog-provider>
                    <!-- <install-page v-if="!showApp" @install="installPWA"></install-page> -->
                    <div>
                        <SignedIn>
                            <top-app @month-change="updateMonth($event!)" />
                            <side-menu>
                                <RouterView />
                            </side-menu>
                            <!-- <bottom-app /> -->
                        </SignedIn>
                    </div>
                </n-dialog-provider>
            </n-message-provider>
        </n-loading-bar-provider>
    </n-config-provider>
</template>

<style scoped></style>
