import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useYearStore = defineStore('year', () => {
  const year = ref(new Date().getFullYear());
  function setYear(m:number) {
    year.value = m;
  }

  return { year, setYear }
})
