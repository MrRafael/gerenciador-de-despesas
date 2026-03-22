import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useMonthStore = defineStore('month', () => {
  const month = ref(1)
  function setMonth(m:number) {
    month.value = m;
  }

  return { month, setMonth }
})
