import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
    { path: '/login', name: 'login', component: () => import('@/views/auth/LoginView.vue') },
    { path: '/register', name: 'register', component: () => import('@/views/auth/RegisterView.vue') },
    {
      path: '/trips',
      name: 'trips',
      component: () => import('@/views/trips/TripsView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/trips/:id',
      name: 'trip-detail',
      component: () => import('@/views/trips/TripDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/trips/:id/edit',
      name: 'trip-edit',
      component: () => import('@/views/trips/TripEditView.vue'),
      meta: { requiresAuth: true }
    },
    { path: '/attractions', name: 'attractions', component: () => import('@/views/attractions/AttractionsView.vue') },
    {
      path: '/favorites',
      name: 'favorites',
      component: () => import('@/views/attractions/FavoritesView.vue'),
      meta: { requiresAuth: true }
    },
    { path: '/s/:token', name: 'shared-trip', component: () => import('@/views/share/SharedTripView.vue') }
  ]
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if ((to.name === 'login' || to.name === 'register') && auth.isAuthenticated) {
    return { name: 'trips' }
  }
})

export default router
