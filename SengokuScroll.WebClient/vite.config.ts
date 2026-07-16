import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import path from "path";

// https://vite.dev/config/
export default defineConfig(() => {
  return {
    plugins: [vue()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "src"),
      },
    },
    server: {
      // 同时响应 localhost 与 127.0.0.1（Windows 上 localhost 常解析到 IPv6 ::1）
      host: true,
      port: 5173,
      proxy: {
        "/api": {
          target: "http://127.0.0.1:5100",
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ""),
        },
      },
    },
  };
});
