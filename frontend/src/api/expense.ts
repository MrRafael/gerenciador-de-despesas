import type { Expense } from "@/types";
import axios from "axios";

export const createExpense = async (expense: Expense) => {
    const { data, status } = await axios.post('api/Expenses/', expense);
    return data;
}

export const getExpensesByUserIdAndRange = async (userId: string, startDate: string, endDate: string): Promise<Expense[] | null> => {
    try {
        const { data, status } = await axios.get(`api/Users/${userId}/Expenses/by-range`, {params: {
            startDate,
            endDate
        }});

        return data;
    } catch {
        console.log('Error');
    }
    return null;
}

export const createExpenses = async (expenses: Expense[]) => {
    const { data, status } = await axios.post('api/Expenses/PostBulkExpense', {expenses});
    return data;
}

export const deleteExpense = async (expenseId: number) => {
    const { data, status } = await axios.delete('api/Expenses/' + expenseId);
    return data;
}