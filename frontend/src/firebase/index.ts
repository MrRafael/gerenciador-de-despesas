import { getAuth, onAuthStateChanged, type User} from 'firebase/auth';

export const getUser = () => {
    return new Promise<User | null>((resolve,reject) => {
        const removeListener = onAuthStateChanged(
            getAuth(),
            (user) => {
                removeListener();
                resolve(user);
            },
            reject
        )
    });
}
