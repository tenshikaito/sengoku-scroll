import { createRouter, createWebHistory } from "vue-router";
import Layout from "@/Layout.vue";
import Home from "@/views/Home.vue";
import Game from "@/views/Game.vue";
import Unauthorized from "@/views/401.vue";
import NotFound from "@/views/404.vue";

const routes = [
  {
    path: "/",
    component: Layout,
    children: [
      {
        path: "",
        name: "Home",
        component: Home,
      },
      {
        path: "/game",
        name: "game",
        component: Game,
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "/strategy",
        redirect: { name: "Home" },
      },
    ],
  },
  {
    path: "/401",
    name: "401",
    component: Unauthorized,
  },
  {
    path: "/:pathMatch(.*)*",
    name: "404",
    component: NotFound,
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, _from, next) => {
  const isAuthenticated = localStorage.getItem("authToken") || true;

  if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: "login" });
  } else if (isAuthenticated && to.name === "login") {
    next({ name: "home" });
  } else {
    next();
  }
});

export default router;
