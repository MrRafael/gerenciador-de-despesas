import type { Expense, ExpenseSaveDto } from "@/types";
import axios from "axios";

export const getExpensesByUserIdAndRange = async (userId: string, startDate: string, endDate: string): Promise<Expense[] | null> => {
    try {
        const { data } = await axios.get(`api/users/${userId}/expenses/by-range`, { params: { startDate, endDate } });
        return data;
    } catch {
        return null;
    }
}

export const createExpense = async (expense: Omit<ExpenseSaveDto, 'date'> & { date: string }): Promise<Expense> => {
    const { data } = await axios.post('api/expenses', expense);
    return data;
}

export const createExpenses = async (expenses: Expense[]): Promise<Expense[]> => {
    const { data } = await axios.post('api/expenses/bulk', { expenses });
    return data;
}

export const deleteExpense = async (expenseId: number): Promise<void> => {
    await axios.delete(`api/expenses/${expenseId}`);
}
