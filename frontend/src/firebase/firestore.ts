import type { Data } from '@/types';
import { collection, query, where, getDocs, type WhereFilterOp, Firestore, addDoc, type WithFieldValue, type DocumentData, deleteDoc, doc, DocumentReference, getDoc, QueryConstraint } from 'firebase/firestore'

export async function getData<T>(dbInstance: Firestore, collectionName: string, fieldToQuery: string, whereClause: WhereFilterOp, valueToQuery:string | DocumentReference): Promise<Array<T|Data>>{
    const collectionRef = collection(dbInstance, collectionName);

    const q = await query(collectionRef, where(fieldToQuery, whereClause , valueToQuery));

    const querySnapshot = await getDocs(q);
    
    const data: Array<T | Data> = [];

    await querySnapshot.forEach(async (doc) => {
        data.push({
            id: doc.id,
            ref: doc.ref,
            ...doc.data(),
        })
    });

    return data;
}

export async function getDataAnd<T>(dbInstance: Firestore, collectionName: string, wheres: QueryConstraint[]): Promise<Array<T|Data>>{
    const collectionRef = collection(dbInstance, collectionName);

    const q = await query(collectionRef, ...wheres);

    const querySnapshot = await getDocs(q);
    
    const data: Array<T | Data> = [];

    await querySnapshot.forEach(async (doc) => {
        data.push({
            id: doc.id,
            ref: doc.ref,
            ...doc.data(),
        })
    });

    return data;
}

export async function getDataByDocId(dbInstance: Firestore, collectionName: string, docId: string): Promise<DocumentData | null> {
    const docRef = doc(dbInstance, collectionName, docId);

    const docSnap = await getDoc(docRef);

    if (docSnap.exists()) {
        const doc = docSnap.data()
        return {
            id: docSnap.id,
            ref: docSnap.ref,
            ...doc
        };
      } else {
        return null
      }
}

export async function getDataByRef(docRef: DocumentReference): Promise<DocumentData | null> {

    const docSnap = await getDoc(docRef);

    if (docSnap.exists()) {
        const doc = docSnap.data()
        return {
            id: docSnap.id,
            ref: docSnap.ref,
            ...doc
        };
      } else {
        return null
      }
}

export function addData(dbInstance: Firestore, collectionName: string, newData: WithFieldValue<DocumentData>) {
    const collectionRef = collection(dbInstance, collectionName);
    
    return addDoc(collectionRef, newData)
}

export async function deleteData(dbInstance: Firestore, collectionName: string, id: string){
    await deleteDoc(doc(dbInstance, collectionName, id));
}