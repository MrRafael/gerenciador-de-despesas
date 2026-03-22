import type { UserInfo } from "@/types";
import axios from "axios";

export const getUserById = async (userId: string): Promise<UserInfo> => {

    try {
        const { data, status } = await axios.get('api/User/' + userId);

        return data;
    } catch {
        console.log('Error');
    }
}