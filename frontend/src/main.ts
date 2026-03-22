import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import firebase from 'firebase/compat/app';
import { initializeAppCheck, ReCaptchaEnterpriseProvider } from "firebase/app-check";
import { firebaseConfig } from './firebaseconfig';
import { clerkPlugin, useAuth } from '@clerk/vue'

const PUBLISHABLE_KEY = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY;
if (!PUBLISHABLE_KEY) {
  throw new Error('Add your Clerk Publishable Key to the .env file');
}

const fireBaseApp = firebase.initializeApp(firebaseConfig);

initializeAppCheck(fireBaseApp, {
    provider: new ReCaptchaEnterpriseProvider(import.meta.env.VITE_RECAPTCHA),
  });

const app = createApp(App)

app.use(createPinia());
app.use(clerkPlugin, { publishableKey: PUBLISHABLE_KEY });
app.use(router)

app.mount('#app')
