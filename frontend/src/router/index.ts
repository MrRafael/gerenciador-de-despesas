import { createRouter, createWebHistory } from 'vue-router'

import HomeView from '../views/HomeView.vue'
import AddExpenseView from '../views/AddExpenseView.vue'
import NotFound from '../views/NotFound.vue'
import ImportFileView from '@/views/ImportFileView.vue'
import GroupsView from '@/views/GroupsView.vue'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'home',
            component: HomeView,
        },
        {
            path: '/addExpense',
            name: 'addExpense',
            component: AddExpenseView,
        },
        {
            path: '/import',
            name: 'import',
            component: ImportFileView,
        },
        {
            path: '/groups',
            name: 'groups',
            component: GroupsView,
        },
        {
            path: '/:pathMatch(.*)*',
            name: 'notfound',
            component: NotFound,
        },
    ]
})

export default router
