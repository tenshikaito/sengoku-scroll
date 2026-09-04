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
    build: {
      // Element Plus / Pixi / ECharts 各自独立成可缓存 vendor chunk；阈值按最大第三方包设置。
      chunkSizeWarningLimit: 900,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (!id.includes("node_modules")) return undefined;
            if (id.includes("pixi.js") || id.includes("@pixi")) return "pixi";
            if (id.includes("echarts") || id.includes("zrender")) return "charts";
            if (id.includes("element-plus") || id.includes("@element-plus")) return "element-plus";
            if (id.includes("vue-router") || id.includes("/vue/")) return "vue";
            if (id.includes("axios")) return "http";
            return "vendor";
          },
        },
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
          configure: (proxy) => {
            proxy.on("error", (_err, _req, res) => {
              if (!("writeHead" in res) || typeof res.writeHead !== "function") return;
              res.writeHead(502, { "Content-Type": "application/json; charset=utf-8" });
              res.end(
                JSON.stringify({
                  errorCode: "BackendUnavailable",
                  message:
                    "策略 API（5100）未启动。请在项目根目录运行：dotnet run --project SengokuScroll.WebApi --launch-profile http",
                })
              );
            });
          },
        },
      },
    },
  };
});
