<script setup lang="ts">
import { NUpload, NUploadDragger, NText, NP, NIcon, NDataTable, NInput, NSelect, NButton, NFormItem, NTag, type DataTableColumns, useMessage, NTooltip } from 'naive-ui';
import Papa from 'papaparse';
import {
    ArchiveRound as ArchiveIcon,
    WarningAmberFilled as WarningIcon,
    DoneAllFilled as DoneIcon,
} from '@vicons/material'
import { ref, h, onMounted } from 'vue';
import type { Expense, MemberGroup } from '@/types';
import { useUserStore } from '@/stores/user';
import { getCategoriesByUserId } from '@/api/category';
import { createExpenses, getExpensesByUserIdAndRange } from '@/api/expense';
import { getMyGroups } from '@/api/groups';

const data = ref<Expense[]>([]);
const SavedData = ref<Expense[]>([]);
const options = ref<{ label: string, value: number }[]>([]);
const groups = ref<MemberGroup[]>([]);
const message = useMessage();

const sUser = useUserStore();

const groupOptions = () => [
    { label: 'Pessoal (sem grupo)', value: null as number | null },
    ...groups.value.map(g => ({ label: g.name, value: g.id as number | null }))
];

const loadData = async () => {
    if (sUser.user?.id) {
        const [cat, grp] = await Promise.all([
            getCategoriesByUserId(sUser.user.id),
            getMyGroups(sUser.user.id),
        ]);
        if (cat) options.value = cat.map(x => ({ label: x.name!, value: x.id! }));
        if (grp) groups.value = grp;
    }
}

const handleDelete = (index: number) => {
    data.value.splice(index, 1);
}

const onRemoveCategory = (indexToRemove: number) => {
    const element = data.value[indexToRemove];
    data.value[indexToRemove] = { date: element.date, description: element.description, value: element.value, groupId: element.groupId };
}

function createColumns(): DataTableColumns<Expense> {
    return [
        {
            title: 'Data',
            key: 'date',
            render(row, index) {
                return h(NInput, {
                    value: row.date,
                    onUpdateValue(v: string) {
                        data.value[index].date = v
                    }
                })
            }
        },
        {
            title: 'Descrição',
            key: 'description',
            render(row, index) {
                return h(NInput, {
                    value: row.description,
                    onUpdateValue(v: string) {
                        data.value[index].description = v
                    }
                })
            }
        },
        {
            title: 'Valor',
            key: 'value',
            render(row, index) {
                return h(NInput, {
                    value: row.value,
                    onUpdateValue(v: number) {
                        data.value[index].value = v
                    }
                })
            }
        },
        {
            title: 'Categoria',
            key: 'categoryId',
            render(row, index) {
                if (row.categoryId) {
                    return h(NTag, { closable: true, onClose: () => { onRemoveCategory(index) } }, () => options.value.find(x => x.value == row.categoryId)?.label)
                }
                return h(NSelect, {
                    value: row.categoryId,
                    options: options.value,
                    onUpdateValue(v: number) {
                        data.value[index].categoryId = v
                    }
                })
            }
        },
        {
            title: 'Grupo',
            key: 'groupId',
            render(row, index) {
                if (groups.value.length === 0) return '—';
                return h(NSelect, {
                    value: row.groupId ?? null,
                    options: groupOptions(),
                    style: { minWidth: '150px' },
                    onUpdateValue(v: number | null) {
                        data.value[index].groupId = v ?? undefined;
                    }
                });
            }
        },
        {
            title: 'Deletar',
            key: 'actions',
            render(row, index) {
                return h(
                    NButton,
                    {
                        size: 'small',
                        onClick: () => handleDelete(index)
                    },
                    () => 'Deletar'
                )
            }
        },
        {
            title: 'Conflitos',
            key: 'conflicts',
            render(row) {
                if (SavedData.value.some(x => x.date == row.date && x.value == row.value)) {
                    return h(NTooltip, {}, { default: () => 'Essa despesa pode ja estar cadastrada', trigger: () => h(NIcon, { color: "red" }, () => h(WarningIcon)) })
                } else {
                    return h(NTooltip, {}, { default: () => 'Despesa unica', trigger: () => h(NIcon, { color: "#0e7a0d" }, () => h(DoneIcon)) })
                }
            }
        }
    ]
}


const columns = createColumns();

const onCompleteCsvParse = async (result: any) => {
    await loadData();
    const resultFiltered = [];
    let startDate = null;
    let endDate = null;
    for (let index = 1; index < result.data.length; index++) {
        const element = result.data[index];
        if (element[2] > 0) {
            startDate = !startDate ? element[0] : new Date(startDate) > new Date(element[0]) ? element[0] : startDate;
            endDate = !endDate ? element[0] : new Date(endDate) < new Date(endDate[0]) ? endDate[0] : endDate;
            resultFiltered.push({ date: element[0], description: element[1], value: element[2] })
        }
    }

    loadSavedData(startDate, endDate);

    data.value = resultFiltered;
}

const loadSavedData = async (startDate: string, endDate: string) => {
    const result = await getExpensesByUserIdAndRange(sUser.user.id, startDate, endDate);
    if (result)
        SavedData.value = result;
}

const onErrorCsvParse = () => {
    message.error('Erro ao ler csv.')
}

const handleUpload = (file: any) => {
    Papa.parse(file.file.file, { complete: onCompleteCsvParse, error: onErrorCsvParse });
}

const isDataValid = () => {
    return !data.value.some(x => !x.date || !x.description || !x.value || !x.categoryId);
}

const onSave = async () => {
    if (isDataValid()) {
        try {
            await data.value.forEach(x => x.userId = sUser.user.id)
            createExpenses(data.value)
            message.success("Salvos");
            data.value = [];
        } catch {
            message.error("Erro ao salvar verifique todos os campos.");
        }
    } else {
        message.error("Todos campos devem estar preenchidos");
    }
}

</script>

<template>
    <h1>Importar fatura</h1>
    <n-upload multiple directory-dnd :max="5" accept=".csv" @change="handleUpload">
        <n-upload-dragger>
            <div style="margin-bottom: 12px">
                <n-icon size="48" :depth="3">
                    <ArchiveIcon />
                </n-icon>
            </div>
            <n-text style="font-size: 16px">
                Clique ou arraste um arquivo
            </n-text>
            <n-p depth="3" style="margin: 8px 0 0 0">
                Escolha a fatura em formato csv
                <br />
                Atualmente suportado fatura da nubank com 3 colunas, date, title e amount
                <br />
                Primeira linha será desconsiderada por ser títulos, valores negativos tambem serão desconsiderados
            </n-p>
        </n-upload-dragger>
    </n-upload>
    <div v-if="data.length > 0">
        <n-data-table :columns="columns" :data="data" :bordered="false" />
        <n-form-item class="display-flex justify-center padding-top">
            <n-button type="primary" @click="onSave">Salvar</n-button>
        </n-form-item>
    </div>
</template>
<style scoped>
.padding-top {
    padding-top: 1rem;
}

.padding-top button {
    min-width: 200px;
}
</style>
