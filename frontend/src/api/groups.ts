import type { GroupExpense, MemberGroup } from '@/types';
import axios from 'axios';

export const getMyGroups = async (userId: string): Promise<MemberGroup[]> => {
    const { data } = await axios.get(`api/users/${userId}/groups`);
    return data;
}

export const getInvites = async (userId: string): Promise<MemberGroup[]> => {
    const { data } = await axios.get(`api/users/${userId}/group-invitations`);
    return data;
}

export const getMembers = async (groupId: number): Promise<MemberGroup[]> => {
    const { data } = await axios.get(`api/groups/${groupId}/members`);
    return data;
}

export const getGroupExpenses = async (groupId: number, startDate: string, endDate: string): Promise<GroupExpense[]> => {
    const { data } = await axios.get(`api/groups/${groupId}/expenses`, { params: { startDate, endDate } });
    return data;
}

export const createGroup = async (groupToSave: { name: string, userId: string }): Promise<MemberGroup> => {
    const { data } = await axios.post('api/groups', groupToSave);
    return data;
}

export const inviteMember = async (groupId: number, userEmail: string): Promise<MemberGroup> => {
    const { data } = await axios.post(`api/groups/${groupId}/members`, { userEmail });
    return data;
}

export const acceptInvite = async (groupId: number, userId: string): Promise<MemberGroup> => {
    const { data } = await axios.put(`api/groups/${groupId}/invitations/${userId}/accept`);
    return data;
}

export const refuseInvite = async (groupId: number, userId: string): Promise<void> => {
    await axios.delete(`api/groups/${groupId}/invitations/${userId}`);
}

export const deleteGroup = async (groupId: number): Promise<void> => {
    await axios.delete(`api/groups/${groupId}`);
}

export const deleteMember = async (groupId: number, userId: string): Promise<void> => {
    await axios.delete(`api/groups/${groupId}/members/${userId}`);
}

export const setMemberSalary = async (groupId: number, userId: string, salary: number | null): Promise<void> => {
    await axios.put(`api/groups/${groupId}/members/${userId}/salary`, { salary });
}
