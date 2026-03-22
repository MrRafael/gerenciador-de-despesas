import type { ExpenseCategory } from "@/types";
import axios from "axios";

export const getCategoriesByUserId = async (userId: string): Promise<ExpenseCategory[] | null> => {
    try {
        const { data, status } = await axios.get(`api/Users/${userId}/ExpenseCategory/`);

        return data;
    } catch {
        console.log('Error');
    }
    return null;
}

export const createCategory = async (category: ExpenseCategory) => {
    const { data, status } = await axios.post('api/ExpenseCategory/', category);
    return data;
}