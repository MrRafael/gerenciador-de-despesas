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
    salary?: number | null,
}

export interface Expense {
    id?: number;
    description: string,
    value: number | null,
    date: string,
    categoryId?: number,
    userId?: string,
    groupId?: number,
    groupName?: string,
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
    groupSplitConfigId?: number,
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
    groupSplitConfigId?: number;
}

export interface GroupSplitConfigShare {
    userId: string;
    percentage: number;
}

export interface GroupSplitConfig {
    id: number;
    groupId: number;
    splitType: SplitType;
    isDefault: boolean;
    shares: GroupSplitConfigShare[];
}

export type SplitDirection = 'receiver' | 'payer';

export interface SplitMemberResult {
    userId: string;
    name: string;
    amountPaid: number;
    amountOwed: number;
    balance: number;
    percentage: number;
    direction: SplitDirection;
}

export interface MonthCloseConfirmation {
    userId: string;
    name: string;
    confirmed: boolean;
}

export interface MonthCloseStatus {
    month: number;
    year: number;
    isClosed: boolean;
    confirmations: MonthCloseConfirmation[];
}

export interface PendingMonth {
    month: number;
    year: number;
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