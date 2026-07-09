import HttpUtils from "@/utils/http-utils";

HttpUtils.baseUrl = "https://localhost:7290";

export const login = (data: any) => {
  return HttpUtils.post(`/account/login`, data);
};

export const postCommand = (data: any) => {
  return HttpUtils.post(`/game`, data);
};
