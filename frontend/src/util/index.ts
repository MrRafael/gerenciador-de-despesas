export function parseCurrency(input: string) {
    const nums = input.replace(/(\.|\$|R|\s)/g, '').replace(',', '.').trim();

    if (/^\d+(\.(\d+)?)?$/.test(nums)) {
        return Number(nums);
    }
    return nums === '' ? null : Number.NaN
}

export function formatCurrency(value: number | null) {
    if (value === null) return ''
    return `R$ ${value.toLocaleString('pt-BR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    })}`
}