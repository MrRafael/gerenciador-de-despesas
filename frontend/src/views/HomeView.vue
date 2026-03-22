<script setup lang="ts">
import { ref, onBeforeMount, h } from 'vue';
import { NUpload, NUploadDragger, NText, NP, NIcon, NDataTable, NInput, NSelect, NButton, NFormItem, NTag, type DataTableColumns, useMessage, NTooltip, useDialog } from 'naive-ui';
import { deleteData, getDataAnd, getDataByDocId } from '@/firebase/firestore';
import type { CollaboratorResult, Expense, MonthGroup, PersonalInformation } from '@/types';
import { useMonthStore } from '@/stores/currentMonth';
import { useYearStore } from '@/stores/currentYear';
import { useUserStore } from '@/stores/user';
import { parseCurrency, formatCurrency } from '../util'
import { DeleteOutlineFilled } from '@vicons/material';
import { getCategoriesByUserId } from '@/api/category';
import { deleteExpense, getExpensesByUserIdAndRange } from '@/api/expense';

const dialog = useDialog();


const sUser = useUserStore();
const sMonth = useMonthStore();
const sYear = useYearStore();
const options = ref<{ label: string, value: number }[]>([]);

sMonth.$onAction(({ after }) => {
    after(() => { 
        syncExpenses();
        loadData();
    })
    // calcCollabResults();
});

const expenses = ref<Expense[]>();

const totalAmount = ref(0);

async function syncExpenses(month: number | null = null) {
    if (sUser.user?.id) {
        const result = await getExpensesByUserIdAndRange(sUser.user.id, `${sYear.year}-${sMonth.month}-01`, new Date(sYear.year, sMonth.month, 0).toISOString().split('T')[0]);
        if (result)
            expenses.value = result;
    }
}

const loadData = async () => {
    if (sUser.user?.id) {
        const cat = await getCategoriesByUserId(sUser.user.id);
        if (cat) {
            options.value = cat.map(x => ({ label: x.name!, value: x.id! }))
        }
    }
}

onBeforeMount(async () => {
    await syncExpenses();
    await loadData();
    // calcCollabResults();
});

// function calcCollabResults() {
//     const calcCollabResults: CollaboratorResult[] = [];
//     const collabs = currentMonthGroup.value?.collaborators;

//     collabs?.forEach(collab => {
//         const expensesCollab = expenses.value?.filter(expense => expense.ownerUid === collab.uid)!;
//         const expensesPartial = expenses.value?.filter(expense => !expense.halfHalfDivision)!;
//         const expensesHalfHalf = expenses.value?.filter(expense => expense.halfHalfDivision)!;
//         const amountToDivideEqualNominal = sum(expensesHalfHalf?.map(expense => expense.value as number));

//         const amountToDividePartial = sum(expensesPartial?.map(expense => expense.value as number));

//         const collabPercent = collab.remuneration / totalAmount.value * 100;

//         const expenseToPay = (amountToDivideEqualNominal! / collabs.length) + (amountToDividePartial! * (collabPercent / 100));

//         calcCollabResults.push({
//             collaborator: collab,
//             expenses: expensesCollab!,
//             result: expenseToPay - sum(expensesCollab?.map(expense => expense.value as number))
//         });
//     })

//     collabsResult.value = calcCollabResults;
// }

// function sum(arrayToSum: number[]): number {
//     if (arrayToSum.length === 0) return 0;
//     return arrayToSum.reduce((accumulator, a) => {
//         return accumulator + a;
//     })
// }

// function deleteExpense(expenseId: string) {
//     dialog.warning({
//         title: 'Deletar Despesa?',
//         positiveText: 'Confirmar',
//         negativeText: 'Não',
//         onPositiveClick: async () => {
//             await deleteData(db, 'Expense', expenseId)
//             await syncExpenses();
//             calcCollabResults();
//         },
//     });
// }

const handleDelete = (index: number) => {
    dialog.warning({
        title: 'Deletar despesa selecionada?',
        content: 'Tem Certeza?',
        positiveText: 'Confirmar',
        negativeText: 'Não',
        onPositiveClick: async () => {
            const element = expenses.value![index];
            await deleteExpense(element.id!)
            await syncExpenses();
        },
    });
}

function createColumns(): DataTableColumns<Expense> {
    return [
        {
            title: 'Data',
            key: 'date',
            render(row) {
                return new Date(row.date).toLocaleDateString();
            }
        },
        {
            title: 'Descrição',
            key: 'description',
        },
        {
            title: 'Valor',
            key: 'value',
            render(row) {
                return formatCurrency(row.value)
            }
        },
        {
            title: 'Categoria',
            key: 'categoryId',
            render(row, index) {
                return h(NTag, {}, () => options.value.find(x => x.value == row.categoryId)?.label)
            }
        },
        {
            title: 'Deletar',
            key: 'actions',
            render(row, index) {
                return h(
                    NIcon,
                    {
                        color: "red",
                        style: { cursor: "pointer" },
                        onClick: () => handleDelete(index)
                    },
                    () => h(DeleteOutlineFilled)
                )
            }
        }
    ]
}

const columns = createColumns();

</script>

<template>
    <n-data-table :columns="columns" :data="expenses" :bordered="false" />
</template>

<style scoped>
/* .f-size {
    font-size: 18px;
}

.person-data {
    padding: 1rem 0;
    border-bottom: 1px rgb(212, 212, 212) solid;
}

.person-result {
    padding: 1rem 0;
    margin-bottom: 1rem;
    align-items: center;
    justify-content: center;
    border-radius: 5px;
}

.need-to-pay {
    background-color: rgba(208, 48, 80, 0.16);
}

.need-to-receive {
    background-color: rgba(24, 160, 88, 0.16);
} */
</style>
