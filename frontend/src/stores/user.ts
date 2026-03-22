import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { UserInfo } from '@/types';

export const useUserStore = defineStore('user', () => {
  const user = ref()
  
  function setUser(u: UserInfo) {
        user.value = u;
    }

  return { user, setUser }
})
