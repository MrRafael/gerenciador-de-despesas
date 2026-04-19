import type { MonthCloseStatus, PendingMonth, SplitMemberResult } from '@/types';
import axios from 'axios';

export const getSplitSummary = async (groupId: number, month: number, year: number): Promise<SplitMemberResult[]> => {
    const { data } = await axios.get(`api/groups/${groupId}/split-summary`, { params: { month, year } });
    return data;
}

export const getPendingMonths = async (groupId: number): Promise<PendingMonth[]> => {
    const { data } = await axios.get(`api/groups/${groupId}/month-close/pending`);
    return data;
}

export const getMonthCloseStatus = async (groupId: number, month: number, year: number): Promise<MonthCloseStatus> => {
    const { data } = await axios.get(`api/groups/${groupId}/month-close/${month}/${year}`);
    return data;
}

export const confirmMonthClose = async (groupId: number, month: number, year: number): Promise<boolean> => {
    const { data } = await axios.post(`api/groups/${groupId}/month-close/${month}/${year}/confirm`);
    return data;
}

export const unconfirmMonthClose = async (groupId: number, month: number, year: number): Promise<void> => {
    await axios.delete(`api/groups/${groupId}/month-close/${month}/${year}/confirm`);
}
