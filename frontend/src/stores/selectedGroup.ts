import { ref } from 'vue'
import { defineStore } from 'pinia'

const GROUP_ID_KEY = 'selectedGroupId'
const GROUP_NAME_KEY = 'selectedGroupName'

export const useSelectedGroupStore = defineStore('selectedGroup', () => {
    const groupId = ref<number | null>(
        JSON.parse(localStorage.getItem(GROUP_ID_KEY) ?? 'null')
    )
    const groupName = ref<string | null>(
        localStorage.getItem(GROUP_NAME_KEY)
    )

    function setGroup(id: number | null, name: string | null) {
        groupId.value = id
        groupName.value = name
        if (id !== null) {
            localStorage.setItem(GROUP_ID_KEY, JSON.stringify(id))
            localStorage.setItem(GROUP_NAME_KEY, name ?? '')
        } else {
            localStorage.removeItem(GROUP_ID_KEY)
            localStorage.removeItem(GROUP_NAME_KEY)
        }
    }

    return { groupId, groupName, setGroup }
})
