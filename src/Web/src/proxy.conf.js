const target = process.env["services__api__http__0"] || "http://127.0.0.1:5002";
const wsTarget = target.replace("http", "ws");

module.exports = {
  "/api": {
    target: target,
    secure: false,
    pathRewrite: {
      "^/api": ""
    }
  },
  "/ws/api": {
    target: wsTarget,
    secure: false,
    ws: true,
    pathRewrite: {
      "^/ws/api": ""
    }
  }
}
