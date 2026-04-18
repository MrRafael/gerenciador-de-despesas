import type { DocumentReference } from "firebase/firestore";

export interface Data {
    id: string;
    ref: DocumentReference
}

export interface UserInfo {
    id: string;
    name: string;
    email: string;
    photoUrl: string;
}

export interface MemberGroup {
    id?: number,
    name: string,
    userId: string,
    ownerId: string,
    isActive: boolean,
    memberEmail?: string,
    memberName?: string,
    ownerEmail?: string,
    ownerName?: string,
}

export interface Expense {
    id?: number;
    description: string,
    value: number | null,
    date: string,
    categoryId?: number,
    userId?: string,
}

export enum SplitType {
    Equal = 0,
    Proportional = 1,
    Manual = 2,
}

export interface ExpenseSaveDto {
    description: string,
    value: number | null,
    date: number,
    categoryId?: number,
    userId?: string,
    groupId?: number,
    splitType?: SplitType,
}

export interface ExpenseCategory {
    id?: number,
    name: string;
    userId?: string;
}

export interface ExpenseGroup {
    id?: number,
    name: string;
    userId: string;
}

export interface GroupExpense {
    id: number;
    description: string;
    value: number;
    date: string;
    categoryId: number;
    userId: string;
    splitType?: SplitType;
}

export interface MonthCollabUser {
    uid: string;
    photoUrl: string;
    remuneration: number;
}

export interface MonthGroup extends Data {
    collaborators: MonthCollabUser[],
    expenseGroup: DocumentReference | null | undefined,
    month: number,
    year: number,
}

export interface PersonalInformation extends Data {
    uid: string;
    remuneration: number;
    currentGroup: DocumentReference;
}

export interface CollaboratorResult {
    collaborator: MonthCollabUser,
    expenses: Expense[],
    result: number,
}