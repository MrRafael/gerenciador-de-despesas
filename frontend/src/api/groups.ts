import type { ExpenseGroup, MemberGroup } from '@/types';
import axios from 'axios';

export const getMyGroups = async (userId: string) => {
    const { data, status } = await axios.get(`api/Users/${userId}/GroupMember`);

    return data
}

export const createGroup = async (groupToSave: { name: string, userId: string }) => {
    const { data, status } = await axios.post('api/GroupMember/', groupToSave);

    return data;
}

export const getMembers = async (groupId: number) => {
    const { data, status } = await axios.get(`api/Groups/${groupId}/GroupMember`);

    return data
}


export const createMember = async (memberToAdd: { groupId: number, userEmail: string }) => {
    const { data, status } = await axios.post('api/GroupMember/NewMember', memberToAdd);

    return data;
}

export const getInvites = async (userId: string) => {
    const { data, status } = await axios.get(`api/Users/${userId}/GroupMember/Invites`);

    return data
}

export const acceptInvite = async (userId: string, groupId: number) => {
    const { data, status } = await axios.put(`api/GroupMember/Accept`, {userId, groupId});

    return data
}

export const refuseInvite = async (userId: string, groupId: number) => {
    const { data, status } = await axios.delete(`api/GroupMember/Refuse`, {params: {
        userId,
        groupId
    }});

    return data
}

export const deleteGroup = async (groupId: number) => {
    const { data, status } = await axios.delete(`api/GroupMember/Group`, {params: {
        groupId
    }});

    return data
}

export const deleteMember = async (userId: string, groupId: number) => {
    const { data, status } = await axios.delete(`api/GroupMember/Member`, {params: {
        userId,
        groupId
    }});

    return data
}