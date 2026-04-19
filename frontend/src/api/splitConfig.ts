import type { GroupSplitConfig, GroupSplitConfigShare, SplitType } from '@/types';
import axios from 'axios';

export interface CreateGroupSplitConfigPayload {
    splitType: SplitType;
    isDefault: boolean;
    shares: GroupSplitConfigShare[];
}

export interface UpdateGroupSplitConfigPayload {
    isDefault: boolean;
    shares: GroupSplitConfigShare[];
}

export const getSplitConfigs = async (groupId: number): Promise<GroupSplitConfig[]> => {
    const { data } = await axios.get(`api/groups/${groupId}/split-configs`);
    return data;
}

export const createSplitConfig = async (groupId: number, payload: CreateGroupSplitConfigPayload): Promise<GroupSplitConfig> => {
    const { data } = await axios.post(`api/groups/${groupId}/split-configs`, payload);
    return data;
}

export const updateSplitConfig = async (groupId: number, configId: number, payload: UpdateGroupSplitConfigPayload): Promise<GroupSplitConfig> => {
    const { data } = await axios.put(`api/groups/${groupId}/split-configs/${configId}`, payload);
    return data;
}

export const deleteSplitConfig = async (groupId: number, configId: number): Promise<void> => {
    await axios.delete(`api/groups/${groupId}/split-configs/${configId}`);
}
