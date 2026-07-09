export default class HttpUtils {
  public static baseUrl = "";

  private static async request(method: string, url: string, params?: any) {
    if (this.baseUrl) {
      url = this.baseUrl + url;
    }

    const headers = {
      "Content-Type": "application/json",
    } as HeadersInit | any;

    const options: RequestInit = {
      method,
      headers: headers,
    };

    // 在请求发出前进行一些处理，比如添加认证token
    const token = localStorage.getItem("token");

    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }

    if (params) {
      if (method === "GET") {
        url += "?" + new URLSearchParams(params).toString();
      } else {
        options.body = JSON.stringify(params);
      }
    }

    let response: Response;
    try {
      response = await fetch(url, options);
    } catch {
      throw new Error(
        "无法连接 API。请确认两个终端都在运行：① dotnet run --project SengokuScroll.WebApi  ② npm run dev（本页 http://localhost:5173）"
      );
    }

    if (response.status !== 200 && response.status !== 201) {
      let detail = response.statusText;
      try {
        const errBody = await response.json();
        if (errBody?.code) detail = errBody.code;
      } catch {
        /* 非 JSON 错误体 */
      }
      throw new Error(detail || `HTTP ${response.status}`);
    }

    return await response.json();
  }

  public static async get(url: string, params?: any) {
    return await this.request("GET", url, params);
  }

  public static async post(url: string, params?: any) {
    return await this.request("POST", url, params);
  }

  public static async put(url: string, params?: any) {
    return await this.request("PUT", url, params);
  }

  public static async delete(url: string, params?: any) {
    return await this.request("DELETE", url, params);
  }
}
