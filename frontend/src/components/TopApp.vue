<script setup lang="ts">
import { onBeforeMount, ref } from 'vue';
import ConstMonths from '../consts/months';
import { NPopselect, NButton, NGrid, NGridItem} from 'naive-ui';
import {useUserStore} from '../stores/user';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { UserButton, useUser } from '@clerk/vue';
import axios from 'axios';

const currentYear = new Date().getFullYear(); 
const decemberLastYear = {
    value: -1,
    year: currentYear - 1,
    label: ConstMonths[11].label + " " + (currentYear - 1),
};
const months = [decemberLastYear].concat(ConstMonths.map(month => ({...month, year: currentYear})));

const today = new Date();
const currentMonth = today.getMonth() + 1;
const month = ref(months.filter(month => month.value === currentMonth)[0].value);

const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();

const loadUser = async () => {
    const {user} = useUser();
    if(user.value?.id){
        try {
            const { data, status } = await axios.get('api/Users/' + user.value?.id, {params: [user.value?.id]});
            await sUser.setUser(data)
        } catch(e: any) {
            console.log(e);
            
            if(e?.response?.status === 404){
                const {data} = await axios.post('api/Users/', {
                    Id: user.value?.id,
                    Name: user.value.firstName,
                    Email: user.value.primaryEmailAddress?.emailAddress,
                    Photo: user.value.imageUrl,
                })

                await sUser.setUser(data)
            }
        }
    }
}

onBeforeMount(async () => {
    loadUser();
});

function currentMonthLabel(monthNumber: number) {
    return months.filter(month => month.value === monthNumber)[0].label;
}

async function changeCurrentMonth(month:number){
    await sYear.setYear(months.filter(m => m.value === month)[0].year);
    await sMonth.setMonth(month === -1 ? 12 : month);
}

</script>

<template>
    <header>
        <n-grid :cols="3" class="grid">
            <n-grid-item class="grid-item">
            </n-grid-item>
            <n-grid-item class="grid-item grid-item-select">
                <n-popselect :options="months" v-model:value="month" class="select" trigger="click" @update:value="changeCurrentMonth(month)">
                    <n-button class="select" round >{{ currentMonthLabel(month) }}</n-button>
                </n-popselect>
            </n-grid-item>
            <n-grid-item class="grid-item grid-item-logout">
                <UserButton/>
            </n-grid-item>
        </n-grid>
    </header>
</template>

<style scoped>
header {
    width: 100%;
    height: 4rem;
    position: fixed;
    left: 0;
    top: 0;
    padding: 0 2rem;
    z-index: 100;
    background: var(--color-background, #fff);
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
    border-bottom: 1px solid var(--color-border, rgba(60, 60, 60, 0.12));
}
.grid{
    height: 100%;
}
.grid-item {
    height: 100%;
    display: flex;
    align-items: center;
}

.grid-item-select {
    justify-content: center;
}

.grid-item-logout {
    justify-content: flex-end;
}
</style>