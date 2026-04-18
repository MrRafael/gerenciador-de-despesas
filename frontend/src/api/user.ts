import type { UserInfo } from "@/types";
import axios from "axios";

export const getUserById = async (userId: string): Promise<UserInfo | null> => {
    try {
        const { data } = await axios.get(`api/users/${userId}`);
        return data;
    } catch {
        return null;
    }
}
