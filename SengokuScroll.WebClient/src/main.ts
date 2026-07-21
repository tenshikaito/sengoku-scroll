import { createApp } from "vue";
import ElementPlus from "element-plus";
import zhCn from "element-plus/es/locale/lang/zh-cn";
import en from "element-plus/es/locale/lang/en";
import "element-plus/dist/index.css";
import "./style.css";
import "./styles/strategyDialogShared.css";
import router from "./router";
import App from "./App.vue";
import { installI18n, readStoredLocale } from "@/i18n";

const elementLocales: Record<string, typeof zhCn> = {
  "zh-CN": zhCn,
  "en-US": en,
};

const app = createApp(App);

app.use(router);
app.use(ElementPlus, {
  locale: elementLocales[readStoredLocale()] ?? zhCn,
});
installI18n(app);

app.mount("#app");
