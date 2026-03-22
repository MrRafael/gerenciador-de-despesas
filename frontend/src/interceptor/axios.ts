import { useAuth } from '@clerk/vue';
import axios from 'axios';

const { getToken } = useAuth();

axios.defaults.baseURL = import.meta.env.VITE_BACKEND_URL;

axios.interceptors.response.use(resp => resp, async error => {
    if (error.response.status === 401) {
        const token = await getToken.value();
        axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    }

    return error;
})