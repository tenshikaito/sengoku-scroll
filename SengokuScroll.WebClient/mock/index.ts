import type { MockMethod } from "vite-plugin-mock";

export default [
  {
    url: "/api/users/12345",
    method: "get",
    response: () => {
      return {
        code: 200,
        message: "success",
        data: {
          id: 12345,
          name: "张三",
          email: "zhangsan@example.com",
          age: 30,
          address: "北京市海淀区",
        },
      };
    },
  },
  {
    url: "/api/login",
    method: "get",
    response: () => {
      return {
        code: 200,
        message: "success",
        data: {
          id: 1,
          name: "张三",
          portrait: "https://i.pravatar.cc/150?img=3",
          token: "123456789",
        },
      };
    },
  },
] as MockMethod[];

console.log("mock start");
